using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ScoutHelper.Models;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Config;

[Serializable]
public class Configuration : IPluginConfiguration {
	// the below exists just to make saving less cumbersome
	[NonSerialized, NotManaged] private IDalamudPluginInterface _pluginInterface = null!;

	public int Version { get; set; } = 0;

	public string BearApiBaseUrl { get; set; } = "https://tracker.beartoolkit.com/api/";
	public string BearApiTrainPath { get; set; } = "hunttrain";
	public TimeSpan BearApiTimeout { get; set; } = TimeSpan.FromSeconds(5);
	public string BearSiteTrainUrl { get; set; } = "https://tracker.beartoolkit.com/train";
	public string BearTrainName { get; set; } = "Scout Helper Train";

	public string SirenBaseUrl { get; set; } = "https://sirenhunts.com/scouting/";

	public string TurtleBaseUrl { get; set; } = "https://scout.wobbuffet.net";
	public string TurtleTrainPath { get; set; } = "/scout";
	public string TurtleApiBaseUrl { get; set; } = "https://scout.wobbuffet.net";
	public string TurtleApiTrainPath { get; set; } = "/api/v1/scout";
	public TimeSpan TurtleApiTimeout { get; set; } = TimeSpan.FromSeconds(5);

	public string CopyTemplate { get; set; } = Constants.DefaultCopyTemplate;
	public bool IsCopyModeFullText { get; set; } = false;

	[NotManaged] public DateTime LastPluginUpdate { get; set; } = DateTime.UnixEpoch;
	[NotManaged] public DateTime LastNoticeAcknowledged { get; set; } = DateTime.UnixEpoch;

	[NotManaged]
	[Obsolete("field no longer needed, but must remain for backwards compatibility")]
	public DateTime LastInstancePatchUpdate { get; set; } = DateTime.UnixEpoch;

	[NotManaged] public Dictionary<uint, uint> Instances { get; set; } = new();
	[NotManaged] public (Territory, uint)[] LatestPatchInstances { get; set; } = Constants.LatestPatchInstances;

	[NotManaged] public Dictionary<string, string?> ConfigDefaults { get; set; } = new();

	public void Initialize(IPluginLog log, IDalamudPluginInterface pluginInterface) {
		_pluginInterface = pluginInterface;
		ManageDefaults(log);
	}

	private void ManageDefaults(IPluginLog log) {
		log.Debug("checking configs for new defaults...");

		var defaultConf = new Configuration();

		typeof(Configuration)
			.GetProperties()
			.Where(propInfo => propInfo.CustomAttributes.All(attrData => attrData.AttributeType != typeof(NotManaged)))
			.ForEach(
				propInfo => {
					var currentDefault = propInfo.GetValue(defaultConf);
					var currentDefaultStr = currentDefault?.ToString();
					var propName = propInfo.Name;

					if (ConfigDefaults.TryGetValue(propName, out var prevDefault)
						&& ActualValuesEqualBecauseMicrosoftHasBrainDamage(prevDefault, currentDefaultStr)
					) {
						log.Debug("config [{0:l}] does not need to be updated.", propName);
						return;
					}

					log.Debug("updating config [{0:l}] to the new default.", propName);
					ConfigDefaults[propName] = currentDefaultStr;
					propInfo.SetValue(this, currentDefault);
				}
			);

		Save();
	}

	public void Save() {
		_pluginInterface.SavePluginConfig(this);
	}
}
