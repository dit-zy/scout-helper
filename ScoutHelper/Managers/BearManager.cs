using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScoutHelper.Config;
using ScoutHelper.Models;
using ScoutHelper.Models.Http;
using ScoutHelper.Utils;

namespace ScoutHelper.Managers;

public class BearManager : IDisposable {
	private readonly IPluginLog _log;
	private readonly Configuration _conf;
	private readonly HttpClient _httpClient = new();

	private IDictionary<uint, (Patch patch, string name)> MobIdToBearName { get; init; }

	public BearManager(IPluginLog log, Configuration conf, ScoutHelperOptions options) {
		_log = log;
		_conf = conf;

		MobIdToBearName = LoadData(options.BearDataFile);
	}

	public void Dispose() {
		_httpClient.Dispose();

		GC.SuppressFinalize(this);
	}

	public async Task<Result<BearLinkData, string>> GenerateBearLink(
		string worldName,
		IEnumerable<TrainMob> trainMobs
	) {
		var bearSupportedMobs = trainMobs.Where(mob => MobIdToBearName.ContainsKey(mob.MobId)).ToList();
		if (bearSupportedMobs.Count == 0)
			return "no mobs supported by bear toolkit were found in the hunt helper train recorder ;-;";

		var spawnPoints = bearSupportedMobs.Select(CreateRequestSpawnPoint).ToList();
		var highestPatch = bearSupportedMobs
			.Select(mob => MobIdToBearName[mob.MobId].patch)
			.Distinct()
			.Max();

		return await HttpUtils.DoRequest<BearApiTrainRequest, BearApiTrainResponse, BearLinkData>(
				_log,
				_httpClient,
				_conf.BearApiBaseUrl,
				new BearApiTrainRequest(worldName, _conf.BearTrainName, highestPatch.BearName(), spawnPoints),
				(client, content) => {
					client.Timeout = _conf.BearApiTimeout;
					return client.PostAsync(_conf.BearApiTrainPath, content);
				},
				bearResponse => new BearLinkData(
					$"{_conf.BearSiteTrainUrl}/{bearResponse.Trains.First().TrainId}",
					bearResponse.Trains.First().Password,
					highestPatch
				)
			)
			.HandleHttpError(
				_log,
				"timed out posting the train to bear ;-;",
				"generating the bear link was canceled >_>",
				"something failed when communicating with bear :T",
				"an unknown error happened while generating the bear link D:"
			);
	}

	private BearApiSpawnPoint CreateRequestSpawnPoint(TrainMob mob) {
		var huntName = MobIdToBearName[mob.MobId].name;
		if (mob.Instance is >= 1 and <= 9) {
			huntName += $" {mob.Instance}";
		}

		return new BearApiSpawnPoint(
			huntName,
			mob.Position.X,
			mob.Position.Y,
			mob.LastSeenUtc
		);
	}

	private IDictionary<uint, (Patch patch, string name)> LoadData(string dataFilePath) {
		_log.Debug("Loading Bear data...");

		if (!File.Exists(dataFilePath)) {
			throw new Exception($"Can't find {dataFilePath}");
		}

		var data = JsonConvert.DeserializeObject<IDictionary<string, JObject>>(File.ReadAllText(dataFilePath));
		if (data == null) {
			throw new Exception("Failed to read in Bear data ;-;");
		}

		var bearData = data
			.SelectMany(
				patchData => {
					if (!Enum.TryParse(patchData.Key, out Patch patch)) {
						throw new Exception($"Unknown patch: {patchData.Key}");
					}

					return (patchData.Value as IDictionary<string, JToken>).Select(
						mob => {
							var mobName = mob.Key;
							var mobId = (uint)mob.Value;
							return (mobName, patch, mobId);
						}
					);
				}
			)
			.ToImmutableDictionary(
				mob => mob.mobId,
				mob => (mob.patch, mob.mobName)
			);

		_log.Debug("Bear data loaded.");

		return bearData;
	}
}

public record struct BearLinkData(
	string Url,
	string Password,
	Patch HighestPatch
) { }

public static class BearExtensions {
	private static readonly IDictionary<Patch, string> BearPatchNames = new Dictionary<Patch, string> {
		{ Patch.ARR, "ARR" },
		{ Patch.HW, "HW" },
		{ Patch.SB, "SB" },
		{ Patch.SHB, "ShB" },
		{ Patch.EW, "EW" },
		{ Patch.DT, "DT" }
	}.VerifyEnumDictionary();

	public static string BearName(this Patch patch) {
		return BearPatchNames[patch];
	}
}
