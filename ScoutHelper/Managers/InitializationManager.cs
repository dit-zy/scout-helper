using System;
using System.Linq;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;
using ScoutHelper.Config;
using ScoutHelper.Models;
using ScoutHelper.Utils;
using ScoutHelper.Utils.Functional;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Managers;

/**
 * A manager for initializing system components that require it *after*
 * dependency injection is stood up. It is safe to assume that this manager will
 * was run before any other components are used.
 */
public class InitializationManager {
	private readonly IPluginLog _log;
	private readonly Configuration _conf;
	private readonly TerritoryManager _territoryManager;

	// mobManager is unused, but including it in the constructor forces it to be
	// initialized right away, rather than waiting for a dependant to be used.
	public InitializationManager(
		IPluginLog log,
		Configuration conf,
		TerritoryManager territoryManager,
		MobManager mobManager
	) {
		_log = log;
		_conf = conf;
		_territoryManager = territoryManager;
	}

	public void InitializeNecessaryComponents() {
		InitializeInstanceMap();
	}

	private void InitializeInstanceMap() {
		GetEnumValues<Patch>()
			.ForEach(
				patch => patch
					.HuntMaps()
					.SelectResults(
						map => _territoryManager
							.GetTerritoryId(map)
							.ToResult($"Failed to find a territoryId for map name: {map}")
					)
					.ForEachError(error => _log.Debug(error))
					.Value
					.Where(mapId => !_conf.Instances.ContainsKey(mapId))
					.ForEach(mapId => _conf.Instances[mapId] = 1U)
			);
	}
}
