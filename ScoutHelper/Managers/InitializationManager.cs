using System.Linq;
using Dalamud.Plugin.Services;
using ScoutHelper.Config;
using XIVHuntUtils;
using XIVHuntUtils.Managers;
using XIVHuntUtils.Models;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Managers;

/**
 * A manager for initializing system components that require it *after*
 * dependency injection is stood up. It is safe to assume that this manager
 * was run before any other components are used.
 */
public class InitializationManager {
	private readonly IPluginLog _log;
	private readonly Configuration _conf;
	private readonly ITerritoryManager _territoryManager;

	public InitializationManager(
		IPluginLog log,
		Configuration conf,
		ITerritoryManager territoryManager,
		// mobManager is unused, but including it in the constructor forces it to be
		// initialized right away, rather than waiting for a dependant to be used.
		IMobManager mobManager
	) {
		_log = log;
		_conf = conf;
		_territoryManager = territoryManager;
	}

	public void InitializeNecessaryComponents() {
		InitializeInstanceMap();
		InitializeTerritoryInstances();
	}

	private void InitializeInstanceMap() {
		var patchUpdateNotYetApplied = !ActualValuesEqualBecauseMicrosoftHasBrainDamage(
			_conf.LatestPatchInstances,
			HuntConstants.LatestPatchIncreasedInstances.Instances
		);
		_log.Debug("initializing instance map. patch instances updated since last update: {0}", patchUpdateNotYetApplied);

		_territoryManager.GetDefaultInstancesForIds()
			.Where(map => patchUpdateNotYetApplied || !_conf.Instances.ContainsKey(map.territoryId))
			.UseToUpdate(_conf.Instances);

		if (patchUpdateNotYetApplied) _conf.LatestPatchInstances = HuntConstants.LatestPatchIncreasedInstances.Instances.ToArray();
		_conf.Save();
	}

	private void InitializeTerritoryInstances() =>
		TerritoryExtensions.SetTerritoryInstances(_conf.Instances, _territoryManager.GetTerritoryIds());
}
