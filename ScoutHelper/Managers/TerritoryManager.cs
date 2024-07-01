using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpFunctionalExtensions;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets2;
using ScoutHelper.Models;
using ScoutHelper.Utils;
using ScoutHelper.Utils.Functional;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Managers;

public class TerritoryManager {
	private readonly IDalamudPluginInterface _pluginInterface;
	private readonly IPluginLog _log;

	private readonly IDictionary<string, uint> _nameToId;
	private readonly IDictionary<string, IDictionary<uint, string>> _idToName;

	public TerritoryManager(IDalamudPluginInterface pluginInterface, IPluginLog log, IDataManager dataManager) {
		_pluginInterface = pluginInterface;
		_log = log;

		(_nameToId, _idToName) = LoadData(dataManager);
	}

	public Maybe<uint> FindTerritoryId(string territoryName) => _nameToId.MaybeGet(territoryName.Lower());

	public Result<uint, string> GetTerritoryId(string territoryName) =>
		FindTerritoryId(territoryName)
			.ToResult<uint, string>($"Failed to find a territoryId for map name: {territoryName}");

	public Maybe<string> GetTerritoryName(uint territoryId) =>
		_idToName
			.MaybeGet(_pluginInterface.UiLanguage)
			.Bind(nameMap => nameMap.MaybeGet(territoryId));

	private (IDictionary<string, uint> nameToId, IDictionary<string, IDictionary<uint, string>> idToName) LoadData(
		IDataManager dataManager
	) {
		_log.Debug("Building map data from game files...");

		var supportedMapNames = GetEnumValues<Patch>()
			.SelectMany(patch => patch.HuntMaps())
			.Select(map => map.Name())
			.ToImmutableHashSet();

		var supportedPlaceIds = dataManager.GetExcelSheet<PlaceName>(ClientLanguage.English)!
			.Where(place => supportedMapNames.Contains(place.Name.RawString.Lower()))
			.ForEach(place => _log.Verbose("Found PlaceName: {0} | {1:l}", place.RowId, place.Name))
			.Select(place => place.RowId)
			.ToImmutableHashSet();

		var dataDicts = GetEnumValues<ClientLanguage>()
			.Select(
				language => {
					var placeNames = dataManager
						.GetExcelSheet<PlaceName>(language)!
						.Where(name => supportedPlaceIds.Contains(name.RowId))
						.Select(name => (name.RowId, name.Name.RawString))
						.ToDict();

					var idToName = dataManager
						.GetExcelSheet<TerritoryType>(language)!
						.Where(territory => territory.TerritoryIntendedUse.Row == 1)
						.Where(territory => placeNames.ContainsKey(territory.PlaceName.Row))
						.Select(territory => (mapId: territory.RowId, name: placeNames[territory.PlaceName.Row]))
						.GroupBy(map => map.name)
						.Select(
							grouping => {
								if (1 < grouping.Count()) {
									_log.Debug(
										"[{2:l}] Duplicate maps found for name [{0:l}]: {1:l}",
										grouping.Key,
										grouping.Select(place => place.mapId.ToString()).Join(", "),
										language.GetLanguageCode()
									);
								}
								return grouping.First();
							}
						)
						.ForEach(
							territory => _log.Verbose(
								"[{2:l}] Found territoryId [{0}] for place: {1:l}",
								territory.mapId,
								territory.name,
								language.GetLanguageCode()
							)
						)
						.ToDict();

					var nameToId = idToName
						.Flip()
						.Select(entry => (entry.Key.Lower(), entry.Value))
						.ToDict();

					return (nameToId, (language.GetLanguageCode(), idToName));
				}
			)
			.Unzip(
				(ts, us) => (
					ts
						.SelectMany(nameToId => nameToId.AsPairs())
						.ToDict(),
					us
						.ToDict()
				)
			);

		_log.Debug("Map data built.");

		return dataDicts;
	}
}

internal static class TerritoryExtensions {
	private static IDictionary<ClientLanguage, string> _langCodes = new Dictionary<ClientLanguage, string>() {
		{ ClientLanguage.Japanese, "jp" },
		{ ClientLanguage.English, "en" },
		{ ClientLanguage.German, "de" },
		{ ClientLanguage.French, "fr" },
	}.VerifyEnumDictionary();

	public static string GetLanguageCode(this ClientLanguage language) => _langCodes[language];
}
