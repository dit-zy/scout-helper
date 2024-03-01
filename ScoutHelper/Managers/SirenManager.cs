using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScoutHelper.Config;
using ScoutHelper.Models;
using ScoutHelper.Models.Json;
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

	public AccResults<Maybe<(string Url, Patch HighestPatch)>, string> GenerateSirenLink(IList<TrainMob> mobList) {
		_log.Debug("Generating a siren link for mob list: {0}", mobList);

		var patches = mobList
			.Select(mob => mob.MobId)
			.SelectMaybe(mobId => _mobToPatch.MaybeGet(mobId))
			.Distinct()
			.Order()
			.ToImmutableList();

		_log.Debug("Patches represented in mob list: {0}", patches);

		if (patches.IsEmpty())
			return AccResults.From(
				Maybe<(string, Patch)>.None,
				"No mobs in the train are supported by Siren Hunts ;-;".AsSingletonList()
			);

		return patches
			.SelectResults(
				patch => {
					var patchData = _patchData[patch];
					var urlPathForPatch = new StringBuilder(patchData.MobOrder.Count + 4);

					urlPathForPatch.Append(patch.SirenName());
					urlPathForPatch.Append('>');

					var mobPathSpec = patchData.MobOrder
						.SelectMany(
							mapData => {
								var numInstances = (int)_conf.Instances[mapData.MapId];
								return (0..numInstances)
									.Sequence()
									.SelectMany(
										i => mapData.Mobs.Select(
											mobId => (
												mobId,
												instance: (uint)(numInstances == 1 ? 0 : i + 1))
										)
									);
							}
						)
						.Select(mobOrderInfo => mobList.FindMob(mobOrderInfo.mobId, mobOrderInfo.instance))
						.SelectManyOverMaybe(mob => patchData.GetNearestSpawnPoint(mob.TerritoryId, mob.Position))
						.SelectOverMaybe(spawnPoint => spawnPoint.Glyph.Upper())
						.Select(glyph => glyph.GetValueOrDefault("-"))
						.Join(null);

					if (mobPathSpec.Trim('-').IsNullOrEmpty())
						return Result.Failure<string, string>(
							$"No {patch} mobs in the train are from locations with instances v_v."
						);

					urlPathForPatch.Append(mobPathSpec);
					return Result.Success<string, string>(urlPathForPatch.ToString());
				}
			)
			.WithValue(patchPaths => patchPaths.Join("&"))
			.WithValue(
				patchPaths => patchPaths.IsNullOrEmpty()
					? Maybe.None
					: Maybe.From(($"{_conf.SirenBaseUrl}{patchPaths}", patches.Last()))
			);
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

		var data = JsonConvert.DeserializeObject<Dictionary<string, SirenJsonPatchData>>(File.ReadAllText(dataFilePath));
		if (data == null) {
			throw new Exception("Failed to read Siren data ;-;");
		}

		var patchesData = data
			.SelectMany(patchData => ParsePatchData(territoryManager, mobManager, patchData))
			.WithValue(patches => patches.ToDict())
			.ForEachError(error => { _log.Error(error); });

		var mobToPatch = patchesData
			.Value
			.SelectMany(
				entry => entry.Value.MobOrder.SelectMany(
					mapData => mapData.Mobs.Select(mob => (mob, entry.Key))
				)
			)
			.ToDict();

		_log.Debug("Siren data loaded.");

		return (patchesData.Value, mobToPatch);
	}

	private static AccResults<(Patch patch, SirenPatchData), string> ParsePatchData(
		TerritoryManager territoryManager,
		MobManager mobManager,
		KeyValuePair<string, SirenJsonPatchData> patchData
	) {
		if (!Enum.TryParse(patchData.Key.Upper(), out Patch patch)) {
			throw new Exception($"Unknown patch: {patchData.Key}");
		}

		var parsedMobOrder = patchData
			.Value
			.MobOrder
			.BindResults(
				mapMobs => territoryManager
					.FindTerritoryId(mapMobs.Map)
					.ToResult<uint, string>($"No mapId found for mapName: {mapMobs.Map}")
					.Map(
						mapId => mapMobs
							.Mobs
							.SelectResults(
								mobName => mobManager
									.GetMobId(mobName)
									.ToResult($"No mobId found for mobName: {mobName}")
							)
							.WithValue(mobIds => SirenMapData.From(mapId, mobIds))
					)
			);

		var mapResults = patchData
			.Value
			.Maps
			.Select(
				mapData => {
					var spawnPoints = mapData
						.Value
						.Select(
							spawnPoint => {
								var pos = spawnPoint.Value;
								return new SirenSpawnPoint(
									spawnPoint.Key,
									V2(
										float.Parse(pos.X.Trim(), CultureInfo.InvariantCulture),
										float.Parse(pos.Y.Trim(), CultureInfo.InvariantCulture)
									)
								);
							}
						)
						.ToImmutable();

					return (mapName: mapData.Key, spawnPoints);
				}
			)
			.SelectResults(
				mapData => territoryManager
					.FindTerritoryId(mapData.mapName)
					.Select(id => (id, mapData.spawnPoints))
					.ToResult($"No territoryId found for territoryName: {mapData.mapName}")
			)
			.WithValue(value => value.ToDict());

		return parsedMobOrder.Join(
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

	public static Maybe<SirenSpawnPoint> GetNearestSpawnPoint(
		this SirenPatchData patchData,
		uint territoryId,
		Vector2 pos
	) =>
		patchData
			.Maps
			.MaybeGet(territoryId)
			.SelectMany(
				spawnPoints => Maybe.From(
					spawnPoints.MinBy(spawnPoint => (spawnPoint.Pos - pos).LengthSquared())!
				)
			);
}
