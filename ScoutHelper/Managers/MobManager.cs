using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpFunctionalExtensions;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets2;
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

		var notoriousMonsters = dataManager.GetExcelSheet<NotoriousMonster>(ClientLanguage.English)!
			.Select(monster => monster.BNpcName.Row)
			.ToImmutableHashSet();
		
		var nameToId = dataManager.GetExcelSheet<BNpcName>(ClientLanguage.English)!
			.Select(name => (name: name.Singular.RawString.Lower(), mobId: name.RowId))
			.Where(name => notoriousMonsters.Contains(name.mobId))
			.GroupBy(entry => entry.name)
			.Select(
				grouping => {
					if (1 < grouping.Count()) {
						_log.Debug(
							"Duplicate mobs found for name [{0:l}]: {1:l}",
							grouping.Key,
							grouping.Select(entry => entry.mobId.ToString()).Join(", ")
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
