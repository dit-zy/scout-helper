using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using DitzyExtensions.Functional;
using Newtonsoft.Json;
using ScoutHelper.Config;
using ScoutHelper.Models;
using ScoutHelper.Models.Http;
using ScoutHelper.Models.Json;
using ScoutHelper.Utils;
using XIVHuntUtils.Managers;
using XIVHuntUtils.Models;
using static DitzyExtensions.MathUtils;
using static ScoutHelper.Managers.TurtleHttpStatus;
using TrainMob = ScoutHelper.Models.TrainMob;

namespace ScoutHelper.Managers;

using MobDict = IDictionary<uint, (Patch patch, uint turtleMobId)>;
using TerritoryDict = IDictionary<uint, TurtleMapData>;

public partial class TurtleManager : IDisposable {
	[GeneratedRegex(@"(?:/scout)?/?(?<session>\w+)/(?<password>\w+)/?\s*$")]
	private static partial Regex CollabLinkRegex();

	private readonly IPluginLog _log;
	private readonly IChatGui _chat;
	private readonly Configuration _conf;
	private readonly IClientState _clientState;
	private readonly HuntMarkManager _huntMarkManager;
	private readonly IMobManager _mobManager;
	private readonly HttpClientGenerator _httpClientGenerator;

	private MobDict MobIdToTurtleId { get; }
	private TerritoryDict TerritoryIdToTurtleData { get; }

	private string _currentCollabSession = "";
	private string _currentCollabPassword = "";

	public bool IsTurtleCollabbing { get; private set; } = false;

	public TurtleManager(
		IPluginLog log,
		IChatGui chat,
		Configuration conf,
		IClientState clientState,
		ScoutHelperOptions options,
		ITerritoryManager territoryManager,
		HuntMarkManager huntMarkManager,
		IMobManager mobManager
	) {
		_log = log;
		_chat = chat;
		_conf = conf;
		_clientState = clientState;
		_huntMarkManager = huntMarkManager;
		_mobManager = mobManager;

		_httpClientGenerator = new HttpClientGenerator(
			_log,
			() => _conf.TurtleApiBaseUrl,
			client => client.Timeout = _conf.TurtleApiTimeout
		);

		(MobIdToTurtleId, TerritoryIdToTurtleData)
			= LoadData(options.TurtleDataFile, territoryManager, mobManager);

		_huntMarkManager.OnMarkFound += OnMarkSeen;
	}

	public void Dispose() {
		_httpClientGenerator.Dispose();
		_huntMarkManager.StopLooking();
		_huntMarkManager.OnMarkFound -= OnMarkSeen;

		GC.SuppressFinalize(this);
	}

	public Maybe<(string slug, string password)> JoinCollabSession(string sessionLink) {
		var match = CollabLinkRegex().Match(sessionLink);
		if (!match.Success) return Maybe.None;

		_currentCollabSession = match.Groups["session"].Value;
		_currentCollabPassword = match.Groups["password"].Value;
		IsTurtleCollabbing = true;
		_huntMarkManager.StartLooking();
		return (_currentCollabSession, _currentCollabPassword);
	}

	public void OnMarkSeen(TrainMob mark) {
		if (!IsTurtleCollabbing) return;

		if (!MobIdToTurtleId.ContainsKey(mark.MobId)) {
			if (!_conf.IncludeOccupiedSpawnsInTurtleSession) return;

			var mobIsBRank = _mobManager
				.FindMobRank(mark.MobId)
				.Map(rank => rank == Rank.B)
				.GetValueOrDefault(false);
			if (!mobIsBRank) return;

			_chat.TaggedPrint($"B-rank mark spotted at {mark.Position.AsEchoString()}. marking spawn as occupied.");
			MarkSpawnAsOccupied(mark)
				.ContinueWith(
					task => {
						if (task.Result == TurtleHttpStatus.HttpError) {
							_chat.TaggedPrintError($"something went wrong while flagging spawn as occupied ;-;.");
						}
					},
					TaskContinuationOptions.OnlyOnRanToCompletion
				)
				.ContinueWith(
					task => {
						_log.Error(task.Exception, "failed to update turtle session");
						_chat.TaggedPrintError($"something went wrong when flagging spawn as occupied ;-;.");
					},
					TaskContinuationOptions.OnlyOnFaulted
				);
			return;
		}

		UpdateCurrentSession(mark.AsSingletonList())
			.ContinueWith(
				task => {
					switch (task.Result) {
						case Success:
							_chat.TaggedPrint($"added {mark.Name} to the turtle session.");
							break;
						case NoSupportedMobs:
							_chat.TaggedPrint(
								$"{mark.Name} was seen, but is not supported by turtle and will not be added to the session."
							);
							break;
						case TurtleHttpStatus.HttpError:
							_chat.TaggedPrintError($"something went wrong when adding {mark.Name} to the turtle session ;-;.");
							break;
					}
				},
				TaskContinuationOptions.OnlyOnRanToCompletion
			)
			.ContinueWith(
				task => _log.Error(task.Exception, "failed to update turtle session"),
				TaskContinuationOptions.OnlyOnFaulted
			);
	}

	public void RejoinLastCollabSession() {
		if (_currentCollabSession.IsNullOrEmpty() || _currentCollabPassword.IsNullOrEmpty())
			throw new Exception("cannot rejoin the last turtle collab session as there is no last session.");
		IsTurtleCollabbing = true;
		_huntMarkManager.StartLooking();
	}

	public void LeaveCollabSession() {
		IsTurtleCollabbing = false;
		_huntMarkManager.StopLooking();
	}

	public async Task<TurtleHttpStatus> MarkSpawnAsOccupied(TrainMob mob) {
		var httpResult = await
			HttpUtils.DoRequest(
				_log,
				new TurtleTrainSpawnOccupiedRequest(
					_currentCollabPassword,
					_clientState.PlayerTag().Where(_ => _conf.IncludeNameInTurtleSession),
					mob.TerritoryId,
					mob.Instance.AsTurtleInstance(),
					mob.Position,
					true
				),
				content => _httpClientGenerator.Client.PatchAsync(
					string.Format(_conf.TurtleApiSpawnOccupiedPath, _currentCollabSession),
					content
				)
			).HandleHttpError(
				_log,
				"timed out while trying to mark a spawn as occupied.",
				"operation canceled while trying to mark a spawn as occupied.",
				"http exception while trying to mark a spawn as occupied.",
				"unknown exception while trying to mark a spawn as occupied."
			);

		return httpResult.IsSuccess ? Success : TurtleHttpStatus.HttpError;
	}

	public async Task<TurtleHttpStatus> UpdateCurrentSession(IList<TrainMob> train) {
		var turtleSupportedMobs = train.Where(mob => MobIdToTurtleId.ContainsKey(mob.MobId)).AsList();
		if (turtleSupportedMobs.IsEmpty())
			return NoSupportedMobs;

		var httpResult = await
			HttpUtils.DoRequest(
				_log,
				new TurtleTrainUpdateRequest(
					_currentCollabPassword,
					_clientState.PlayerTag().Where(_ => _conf.IncludeNameInTurtleSession),
					turtleSupportedMobs.Select(mob =>
						(TerritoryIdToTurtleData[mob.TerritoryId].TurtleId,
							mob.Instance.AsTurtleInstance(),
							MobIdToTurtleId[mob.MobId].turtleMobId,
							mob.Position)
					)
				),
				content => _httpClientGenerator.Client.PatchAsync(
					$"{_conf.TurtleApiTrainPath}/{_currentCollabSession}",
					content
				)
			).HandleHttpError(
				_log,
				"timed out while trying to post updates to turtle session.",
				"operation canceled while trying to post updates to turtle session.",
				"http exception while trying to post updates to turtle session.",
				"unknown exception while trying to post updates to turtle session."
			);

		return httpResult.IsSuccess ? Success : TurtleHttpStatus.HttpError;
	}

	public async Task<Result<TurtleLinkData, string>> GenerateTurtleLink(
		IList<TrainMob> trainMobs,
		bool allowEmpty = false
	) {
		var turtleSupportedMobs = trainMobs.Where(mob => MobIdToTurtleId.ContainsKey(mob.MobId)).AsList();
		if (!allowEmpty && turtleSupportedMobs.IsEmpty())
			return "No mobs supported by Turtle Scouter were found in the Hunt Helper train recorder ;-;";

		var spawnPoints = turtleSupportedMobs.SelectMaybe(GetRequestInfoForMob).ToList();
		var highestPatch = allowEmpty
			? Patch.DT
			: turtleSupportedMobs
				.Select(mob => MobIdToTurtleId[mob.MobId].patch)
				.Max();

		return await HttpUtils.DoRequest<TurtleTrainRequest, TurtleTrainResponse, TurtleLinkData>(
				_log,
				TurtleTrainRequest.CreateRequest(spawnPoints),
				(content) => _httpClientGenerator.Client.PostAsync(_conf.TurtleApiTrainPath, content),
				trainResponse => TurtleLinkData.From(trainResponse, highestPatch)
			)
			.HandleHttpError(
				_log,
				"timed out posting the train to turtle ;-;",
				"generating the turtle link was canceled >_>",
				"something failed when communicating with turtle :T",
				"an unknown error happened while generating the turtle link D:"
			);
	}

	private Maybe<(uint mapId, uint instance, uint pointId, uint mobId)> GetRequestInfoForMob(TrainMob mob) =>
		TerritoryIdToTurtleData
			.MaybeGet(mob.TerritoryId)
			.SelectMany(mapData => GetNearestSpawnPoint(mob)
				.Select(nearestSpawnPoint => (
						mapData.TurtleId,
						mob.Instance.AsTurtleInstance(),
						nearestSpawnPoint,
						MobIdToTurtleId[mob.MobId].turtleMobId
					)
				)
			);

	private Maybe<uint> GetNearestSpawnPoint(TrainMob mob) =>
		TerritoryIdToTurtleData
			.MaybeGet(mob.TerritoryId)
			.Select(territoryData => territoryData
				.SpawnPoints
				.AsPairs()
				.MinBy(spawnPoint => (spawnPoint.val - mob.Position).LengthSquared())
				.key
			);

	private (MobDict, TerritoryDict) LoadData(
		string dataFilePath,
		ITerritoryManager territoryManager,
		IMobManager mobManager
	) {
		_log.Debug("Loading Turtle data...");

		if (!File.Exists(dataFilePath)) {
			throw new Exception($"Can't find {dataFilePath}");
		}

		var data = JsonConvert.DeserializeObject<Dictionary<string, TurtleJsonPatchData>>(File.ReadAllText(dataFilePath));
		if (data is null) {
			throw new Exception("Failed to read Turtle data ;-;");
		}

		var patchesData = data
			.SelectMany(patchData => ParsePatchData(territoryManager, mobManager, patchData))
			.WithValue(patchData => patchData.Unzip())
			.ForEachError(error => { _log.Error(error); });

		var (mobIds, territories) = patchesData.Value;

		return (
			mobIds
				.SelectMany(mobDict => mobDict.AsPairs())
				.ToDict(),
			territories
				.SelectMany(territoryDict => territoryDict.AsPairs())
				.ToDict()
		);
	}

	private static
		AccumulatedResults<(MobDict, TerritoryDict), string> ParsePatchData(
			ITerritoryManager territoryManager,
			IMobManager mobManager,
			KeyValuePair<string, TurtleJsonPatchData> patchData
		) {
		if (!Enum.TryParse(patchData.Key.Upper(), out Patch patch)) {
			throw new Exception($"Unknown patch: {patchData.Key}");
		}

		var parsedMobs = patchData
			.Value
			.Mobs
			.SelectResults(patchMob => mobManager
				.GetMobId(patchMob.Key)
				.Map(mobId => (mobId, (patch, patchMob.Value)))
			)
			.WithValue(mobs => mobs.ToDict());

		var parsedTerritories = patchData
			.Value
			.Maps
			.SelectResults(mapData => territoryManager
				.FindTerritoryId(mapData.Key)
				.ToResult<uint, string>($"No mapId found for mapName: {mapData.Key}")
				.Map(territoryId => {
						var points = mapData
							.Value
							.Points
							.Select(pointData =>
								(pointData.Key, V2(pointData.Value.X.AsFloat(), pointData.Value.Y.AsFloat()))
							)
							.ToDict();

						return (territoryId, new TurtleMapData(mapData.Value.Id, points));
					}
				)
			)
			.WithValue(territoriesAsPairs => territoriesAsPairs.ToDict());

		return parsedMobs.JoinWith(parsedTerritories, (mobs, territories) => (mobs, territories));
	}
}

public enum TurtleHttpStatus {
	Success,
	NoSupportedMobs,
	HttpError,
}

public record struct TurtleLinkData(
	string Slug,
	string CollabPassword,
	string ReadonlyUrl,
	string CollabUrl,
	Patch HighestPatch
) {
	public static TurtleLinkData From(TurtleTrainResponse response, Patch highestPatch) =>
		new(
			response.Slug,
			response.CollaboratorPassword,
			response.ReadonlyUrl,
			response.CollaborateUrl,
			highestPatch
		);
}
