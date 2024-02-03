using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Dalamud;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets2;
using ScoutHelper.Config;
using ScoutHelper.Utils;
using ScoutHelper.Utils.Functional;

namespace ScoutHelper.Managers;

public class MobManager {
	private readonly IPluginLog _log;

	private readonly IDictionary<string, uint> _mobNameToId;
	private readonly IDictionary<uint, string> _mobIdToName;

	public MobManager(IPluginLog log, IDataManager dataManager) {
		_log = log;

		(_mobNameToId, _mobIdToName) = LoadData(dataManager);
	}

	public Maybe<uint> GetMobId(string mobName) => _mobNameToId.MaybeGet(mobName.Lower());

	public Maybe<string> GetMobName(uint mobId) => _mobIdToName.MaybeGet(mobId);

	private (IDictionary<string, uint> nameToId, IDictionary<uint, string> idToName) LoadData(
		IDataManager dataManager
	) {
		_log.Debug("Building mob data from game files...");
		
		var nameToId = dataManager.GetExcelSheet<BNpcName>(ClientLanguage.English)!
			.Select(name => (name.Singular.ToString().Lower(), name.RowId))
			.GroupBy(entry => entry.Item1)
			.Select(
				grouping => {
					if (1 < grouping.Count()) {
						_log.Debug(
							"Duplicate mobs found for name [{0:l}]: {1:l}",
							grouping.Key,
							grouping.Select(entry => entry.RowId.ToString()).Join(", ")
						);
					}
					return grouping.First();
				}
			)
			.ToDict();
		var idToName = nameToId.Flip();
		
		_log.Debug("Mob data built.");

		return (nameToId, idToName);
	}
}
