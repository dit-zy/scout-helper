using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using ScoutHelper.Config;
using ScoutHelper.Models;
using ScoutHelper.Models.Http;
using ScoutHelper.Models.Json;
using ScoutHelper.Utils;
using ScoutHelper.Utils.Functional;
using static ScoutHelper.Managers.TurtleHttpStatus;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Managers;

using MobDict = IDictionary<uint, (Patch patch, uint turtleMobId)>;
using TerritoryDict = IDictionary<uint, TurtleMapData>;

public partial class TurtleManager {
	[GeneratedRegex(@"(?:/scout)?/?(?<session>\w+)/(?<password>\w+)/?\s*$")]
	private static partial Regex CollabLinkRegex();

	private readonly IPluginLog _log;
	private readonly Configuration _conf;
	private static HttpClient HttpClient { get; } = new();

	private MobDict MobIdToTurtleId { get; }
	private TerritoryDict TerritoryIdToTurtleData { get; }

	private string _currentCollabSession = "";
	private string _currentCollabPassword = "";

	public TurtleManager(
		IPluginLog log,
		Configuration conf,
		ScoutHelperOptions options,
		TerritoryManager territoryManager,
		MobManager mobManager
	) {
		_log = log;
		_conf = conf;

		HttpClient.BaseAddress = new Uri(_conf.TurtleApiBaseUrl);
		HttpClient.DefaultRequestHeaders.UserAgent.Add(Constants.UserAgent);
		HttpClient.Timeout = _conf.TurtleApiTimeout;

		(MobIdToTurtleId, TerritoryIdToTurtleData)
			= LoadData(options.TurtleDataFile, territoryManager, mobManager);
	}

	public Maybe<(string slug, string password)> JoinCollabSession(string sessionLink) {
		var match = CollabLinkRegex().Match(sessionLink);
		if (!match.Success) return Maybe.None;

		_currentCollabSession = match.Groups["session"].Value;
		_currentCollabPassword = match.Groups["password"].Value;
		return (_currentCollabSession, _currentCollabPassword);
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
					turtleSupportedMobs.Select(
						mob =>
							(TerritoryIdToTurtleData[mob.TerritoryId].TurtleId,
								mob.Instance.AsTurtleInstance(),
								MobIdToTurtleId[mob.MobId].turtleMobId,
								mob.Position)
					)
				),
				requestContent => HttpClient.PutAsync($"{_conf.TurtleApiTrainPath}/{_currentCollabSession}", requestContent)
			).TapError(
				error => {
					if (error.ErrorType == HttpErrorType.Timeout) {
						_log.Warning("timed out while trying to post updates to turtle session.");
					} else if (error.ErrorType == HttpErrorType.Canceled) {
						_log.Warning("operation canceled while trying to post updates to turtle session.");
					} else if (error.ErrorType == HttpErrorType.HttpException) {
						_log.Error(error.Exception, "http exception while trying to post updates to turtle session.");
					} else {
						_log.Error(error.Exception, "unknown exception while trying to post updates to turtle session.");
					}
				}
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

		var trainResult = await HttpUtils.DoRequest<TurtleTrainRequest, TurtleTrainResponse>(
			_log,
			TurtleTrainRequest.CreateRequest(spawnPoints),
			requestContent => HttpClient.PostAsync(_conf.TurtleApiTrainPath, requestContent)
		);

		return trainResult
			.Map(trainInfo => TurtleLinkData.From(trainInfo, highestPatch))
			.MapError<TurtleLinkData, HttpError, string>(
				error => {
					string message;
					switch (error.ErrorType) {
						case HttpErrorType.Timeout: {
							message = "Timed out posting the train to Turtle ;-;";
							_log.Error(message);
							return message;
						}
						case HttpErrorType.Canceled: {
							message = "Generating the Turtle link was canceled >_>";
							_log.Warning(message);
							return message;
						}
						case HttpErrorType.HttpException:
							_log.Error(error.Exception, "Posting the train to Turtle failed.");
							return "Something failed when communicating with Turtle :T";
						default:
							message = "An unknown error happened while generating the Turtle link D:";
							_log.Error(error.Exception, message);
							return message;
					}
				}
			);
	}

	private Maybe<(uint mapId, uint instance, uint pointId, uint mobId)> GetRequestInfoForMob(TrainMob mob) =>
		TerritoryIdToTurtleData
			.MaybeGet(mob.TerritoryId)
			.Select(mapData => mapData.TurtleId)
			.Join(mob.Instance.AsTurtleInstance())
			.Join(GetNearestSpawnPoint(mob))
			.Select(tuple => tuple.Flatten())
			.Join(MobIdToTurtleId[mob.MobId].turtleMobId)
			.Select(tuple => tuple.Flatten());

	private Maybe<uint> GetNearestSpawnPoint(TrainMob mob) =>
		TerritoryIdToTurtleData
			.MaybeGet(mob.TerritoryId)
			.Select(
				territoryData => territoryData
					.SpawnPoints
					.AsPairs()
					.MinBy(spawnPoint => (spawnPoint.val - mob.Position).LengthSquared())
					.key
			);

	private (MobDict, TerritoryDict) LoadData(
		string dataFilePath,
		TerritoryManager territoryManager,
		MobManager mobManager
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
		AccResults<(MobDict, TerritoryDict), string> ParsePatchData(
			TerritoryManager territoryManager,
			MobManager mobManager,
			KeyValuePair<string, TurtleJsonPatchData> patchData
		) {
		if (!Enum.TryParse(patchData.Key.Upper(), out Patch patch)) {
			throw new Exception($"Unknown patch: {patchData.Key}");
		}

		var parsedMobs = patchData
			.Value
			.Mobs
			.SelectResults(
				patchMob => mobManager
					.GetMobId(patchMob.Key)
					.ToResult<uint, string>($"No mobId found for mobName: {patchMob.Key}")
					.Map(mobId => (mobId, (patch, patchMob.Value)))
			)
			.WithValue(mobs => mobs.ToDict());

		var parsedTerritories = patchData
			.Value
			.Maps
			.SelectResults(
				mapData => territoryManager
					.FindTerritoryId(mapData.Key)
					.ToResult<uint, string>($"No mapId found for mapName: {mapData.Key}")
					.Map(
						territoryId => {
							var points = mapData
								.Value
								.Points
								.Select(
									pointData =>
										(pointData.Key, V2(pointData.Value.X.AsFloat(), pointData.Value.Y.AsFloat()))
								)
								.ToDict();

							return (territoryId, new TurtleMapData(mapData.Value.Id, points));
						}
					)
			)
			.WithValue(territoriesAsPairs => territoriesAsPairs.ToDict());

		return parsedMobs.Join(parsedTerritories, (mobs, territories) => (mobs, territories));
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

public static class TurtleExtensions {
	public static uint AsTurtleInstance(this uint? instance) {
		return instance is null or 0 ? 1 : (uint)instance!;
	}
}
