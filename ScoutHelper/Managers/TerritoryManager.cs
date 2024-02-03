using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpFunctionalExtensions;
using Dalamud;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets2;
using ScoutHelper.Config;
using ScoutHelper.Utils;
using ScoutHelper.Utils.Functional;

namespace ScoutHelper.Managers;

public class TerritoryManager {
	private static readonly ISet<string> SupportedMaps = new HashSet<string>() {
		// HW
		"coerthas western highlands", "the sea of clouds", "azys lla",
		"the dravanian forelands", "the dravanian hinterlands", "the churning mists",

		// SB
		"the fringes", "the peaks", "the lochs",
		"the ruby sea", "yanxia", "the azim steppe",

		// SHB
		"lakeland", "kholusia", "amh araeng",
		"il mheg", "the rak'tika greatwood", "the tempest",

		// EW
		"labyrinthos", "thavnair", "garlemald",
		"mare lamentorum", "elpis", "ultima thule",
	}.ToImmutableHashSet();

	private readonly IPluginLog _log;

	private readonly IDictionary<string, IList<uint>> _nameToId;
	private readonly IDictionary<uint, string> _idToName;

	public TerritoryManager(IPluginLog log, IDataManager dataManager) {
		_log = log;

		(_nameToId, _idToName) = LoadData(dataManager);
	}

	public Maybe<IList<uint>> GetTerritoryId(string territoryName) => _nameToId.MaybeGet(territoryName.Lower());

	public Maybe<string> GetTerritoryName(uint territoryId) => _idToName.MaybeGet(territoryId);

	private (IDictionary<string, IList<uint>> nameToId, IDictionary<uint, string> idToName) LoadData(
		IDataManager dataManager
	) {
		_log.Debug("Building map data from game files...");

		var placeNames = dataManager.GetExcelSheet<PlaceName>(ClientLanguage.English)!
			.Where(place => SupportedMaps.Contains(place.Name.ToString().Lower()))
			.Select(place => (place.RowId, place.Name.ToString().Lower()))
			.ToDict();

		var idToName = dataManager.GetExcelSheet<TerritoryType>(ClientLanguage.English)!
			.SelectMaybe(
				territory => placeNames
					.MaybeGet(territory.PlaceName.Row)
					.Select(placeName => (territoryId: territory.RowId, placeName))
			)
			.ToDict();

		var nameToId = idToName
			.Select(entry => (placeName: entry.Value, territoryId: entry.Key))
			.GroupBy(entry => entry.placeName)
			.Select(
				places => (
					places.Key,
					(IList<uint>)places.Select(territory => territory.territoryId).ToImmutableList()
				)
			)
			.ToDict();

		_log.Debug("Map data built.");

		return (nameToId, idToName);
	}
}
