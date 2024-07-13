using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using ScoutHelper.Config;
using ScoutHelper.Models;
using ScoutHelper.Models.Http;
using ScoutHelper.Models.Json;
using ScoutHelper.Utils.Functional;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Managers;

using PatchDict = IDictionary<Patch, uint>;
using MobDict = IDictionary<uint, (Patch patch, uint turtleMobId)>;
using TerritoryDict = IDictionary<uint, TurtleMapData>;

public class TurtleManager {
	private readonly IPluginLog _log;
	private readonly Configuration _conf;
	private static HttpClient HttpClient { get; } = new();

	private MobDict MobIdToTurtleId { get; }
	private TerritoryDict TerritoryIdToTurtleData { get; }

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

	public async Task<Result<TurtleLinkData, string>> GenerateTurtleLink(
		IList<TrainMob> trainMobs
	) {
		var turtleSupportedMobs = trainMobs.Where(mob => MobIdToTurtleId.ContainsKey(mob.MobId)).AsList();
		if (turtleSupportedMobs.IsEmpty())
			return "No mobs supported by Turtle Scouter were found in the Hunt Helper train recorder ;-;";

		var spawnPoints = turtleSupportedMobs.SelectMaybe(GetRequestInfoForMob).ToList();
		var highestPatch = turtleSupportedMobs
			.Select(mob => MobIdToTurtleId[mob.MobId].patch)
			.Max();

		var requestPayload = JsonConvert.SerializeObject(TurtleTrainRequest.CreateRequest(spawnPoints));
		_log.Debug("Request payload: {0}", requestPayload);
		var requestContent = new StringContent(requestPayload, Encoding.UTF8, Constants.MediaTypeJson);

		try {
			var response = await HttpClient.PostAsync(_conf.TurtleApiTrainPath, requestContent);
			_log.Debug(
				"Request: {0}\n\nResponse: {1}",
				response.RequestMessage!.ToString(),
				response.ToString()
			);

			response.EnsureSuccessStatusCode();

			var responseJson = await response.Content.ReadAsStringAsync();
			var trainInfo = JsonConvert.DeserializeObject<TurtleTrainResponse>(responseJson)!;

			return new TurtleLinkData(
				trainInfo.ReadonlyUrl,
				trainInfo.CollaborateUrl,
				highestPatch
			);
		} catch (TimeoutException) {
			const string message = "Timed out posting the train to Turtle ;-;";
			_log.Error(message);
			return message;
		} catch (OperationCanceledException e) {
			const string message = "Generating the Turtle link was canceled >_>";
			_log.Warning(e, message);
			return message;
		} catch (HttpRequestException e) {
			_log.Error(e, "Posting the train to Turtle failed.");
			return "Something failed when communicating with Turtle :T";
		} catch (Exception e) {
			const string message = "An unknown error happened while generating the Turtle link D:";
			_log.Error(e, message);
			return message;
		}
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

public record struct TurtleLinkData(
	string ReadonlyUrl,
	string CollabUrl,
	Patch HighestPatch
);

public static class TurtleExtensions {
	public static uint AsTurtleInstance(this uint? instance) {
		return instance is null or 0 ? 1 : (uint)instance!;
	}
}
