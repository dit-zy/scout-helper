using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScoutHelper.Config;
using ScoutHelper.Models;
using ScoutHelper.Utils;
using ScoutHelper.Utils.Functional;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Managers;

public class SirenManager {
	private readonly IPluginLog _log;
	private readonly Configuration _conf;

	private readonly IDictionary<Patch, SirenPatchData> _patchData;
	private readonly IDictionary<uint, Patch> _mobToPatch;

	public SirenManager(
		IPluginLog log,
		Configuration conf,
		ScoutHelperOptions options,
		TerritoryManager territoryManager,
		MobManager mobManager
	) {
		_log = log;
		_conf = conf;

		(_patchData, _mobToPatch) = LoadData(options.SirenDataFile, territoryManager, mobManager);
	}

	public Result<(string Url, Patch HighestPatch), string> GenerateSirenLink(IList<TrainMob> mobList) {
		_log.Debug("Generating a siren link for mob list: {0}", mobList);

		var patches = mobList
			.Select(mob => mob.MobId)
			.SelectMaybe(mobId => _mobToPatch.MaybeGet(mobId))
			.Distinct()
			.Order()
			.ToImmutableList();

		_log.Debug("Patches represented in mob list: {0}", patches);

		if (patches.IsEmpty()) return "No mobs in the train are supported by Siren Hunts ;-;";

		var fullPath = patches
			.Select(
				patch => {
					var patchData = _patchData[patch];
					var urlPathForPatch = new StringBuilder(patchData.MobOrder.Count + 4);

					urlPathForPatch.Append(patch.SirenName());
					urlPathForPatch.Append('>');

					patchData.MobOrder
						.Select(
							mobOrderInfo => mobList.FindMob(mobOrderInfo.mobId, mobOrderInfo.instance)
								.SelectMany(
									mob => patchData
										.Maps
										.MaybeGet(mob.TerritoryId)
										.SelectMany(
											spawnPoints => Maybe.From(
												spawnPoints.MinBy(spawnPoint => (spawnPoint.Pos - mob.Position).LengthSquared())
											)
										)
								)
								.Select(spawnPoint => spawnPoint.Glyph.Upper())
								.GetValueOrDefault("-")
						)
						.ForEach(glyph => urlPathForPatch.Append(glyph));

					return urlPathForPatch.ToString();
				}
			)
			.Join("&");

		return ($"{_conf.SirenBaseUrl}{fullPath.ToString()}", patches.Last());
	}

	private (IDictionary<Patch, SirenPatchData> patchData, IDictionary<uint, Patch> mobToPatch) LoadData(
		string dataFilePath,
		TerritoryManager territoryManager,
		MobManager mobManager
	) {
		_log.Debug("Loading Siren data...");

		if (!File.Exists(dataFilePath)) {
			throw new Exception($"Can't find {dataFilePath}");
		}

		var data = JsonConvert.DeserializeObject<IDictionary<string, JObject>>(File.ReadAllText(dataFilePath));
		if (data == null) {
			throw new Exception("Failed to read Siren data ;-;");
		}

		var patchesData = data
			.SelectMany(patchData => ParsePatchData(territoryManager, mobManager, patchData))
			.WithValue(patches => patches.ToDict())
			.ForEachError(error => { _log.Error(error); });

		var mobToPatch = patchesData
			.Value
			.SelectMany(entry => entry.Value.MobOrder.Select(mob => (mob.mobId, entry.Key)))
			.ToDict();

		_log.Debug("Siren data loaded.");

		return (patchesData.Value, mobToPatch);
	}

	private static AccResults<(Patch patch, SirenPatchData), string> ParsePatchData(
		TerritoryManager territoryManager,
		MobManager mobManager,
		KeyValuePair<string, JObject> patchData
	) {
		if (!Enum.TryParse(patchData.Key.Upper(), out Patch patch)) {
			throw new Exception($"Unknown patch: {patchData.Key}");
		}

		var patchDataElements = patchData.Value as IDictionary<string, JToken>;

		var mobOrderResults = patchDataElements["mob order"].ToObject<List<string>>()!
			.SelectResults(
				mobName => mobManager
					.GetMobId(mobName.UnInstanced())
					.ToResult($"No mobId found for mobName: {mobName.UnInstanced()}")
					.Map(mobId => (mobId, mobName.Instance()))
			);

		var mapResults = patchDataElements["maps"]
			.ToObject<IDictionary<string, JToken>>()!
			.Select(
				mapData => {
					var spawnPoints = mapData.Value.ToObject<IDictionary<string, JToken>>()!
						.Select(
							spawnPoint => {
								var pos = spawnPoint.Value.ToObject<IList<float>>()!;
								return new SirenSpawnPoint(spawnPoint.Key, V2(pos[0], pos[1]));
							}
						)
						.ToImmutable();

					return (mapName: mapData.Key!, spawnPoints);
				}
			)
			.SelectResults(
				mapData => territoryManager
					.GetTerritoryId(mapData.mapName)
					.Select(ids => ids.Select(id => (id, mapData.spawnPoints)))
					.ToResult($"No territoryId found for territoryName: {mapData.mapName}")
			)
			.WithValue(value => value.SelectMany(x => x).ToDict());

		return mobOrderResults.Join(
			mapResults,
			(mobOrder, maps) => (patch, SirenPatchData.From(mobOrder, maps))
		);
	}
}

public static class SirenExtensions {
	private static readonly IDictionary<Patch, string> SirenPatchNames = new Dictionary<Patch, string>() {
		{ Patch.ARR, "ARR" },
		{ Patch.HW, "HW" },
		{ Patch.SB, "STB" },
		{ Patch.SHB, "SHB" },
		{ Patch.EW, "EW" },
	}.VerifyEnumDictionary();

	public static string SirenName(this Patch patch) {
		return SirenPatchNames[patch];
	}
}
