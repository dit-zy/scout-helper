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

	public string BearApiBaseUrl = "https://tracker.beartoolkit.com/api/";
	public string BearApiTrainPath = "hunttrain";
	public TimeSpan BearApiTimeout = TimeSpan.FromSeconds(5);
	public string BearSiteTrainUrl = "https://tracker.beartoolkit.com/train";
	public string BearTrainName = "Scout Helper Train";

	public string SirenBaseUrl = "https://sirenhunts.com/scouting/";

	public string TurtleBaseUrl = "https://scout.wobbuffet.net";
	public string TurtleTrainPath = "/scout";
	public string TurtleApiBaseUrl = "https://scout.wobbuffet.net";
	public string TurtleApiTrainPath = "/api/v1/scout";
	public TimeSpan TurtleApiTimeout = TimeSpan.FromSeconds(5);
	public bool IncludeNameInTurtleSession = true;

	public string CopyTemplate = Constants.DefaultCopyTemplate;
	public bool IsCopyModeFullText = false;

	[NotManaged] public DateTime LastPluginUpdate = DateTime.UnixEpoch;
	[NotManaged] public DateTime LastNoticeAcknowledged = DateTime.UnixEpoch;

	[NotManaged]
	[Obsolete("field no longer needed, but must remain for backwards compatibility")]
	public DateTime LastInstancePatchUpdate = DateTime.UnixEpoch;

	[NotManaged] public Dictionary<uint, uint> Instances = new();
	[NotManaged] public (Territory, uint)[] LatestPatchInstances = Constants.LatestPatchInstances;

	[NotManaged] public Dictionary<string, string?> ConfigDefaults = new();

	public void Initialize(IPluginLog log, IDalamudPluginInterface pluginInterface) {
		_pluginInterface = pluginInterface;
		ManageDefaults(log);
	}

	private void ManageDefaults(IPluginLog log) {
		log.Debug("checking configs for new defaults...");

		var defaultConf = new Configuration();

		typeof(Configuration)
			.GetFields()
			.Where(info => info.CustomAttributes.All(attrData => attrData.AttributeType != typeof(NotManaged)))
			.ForEach(
				info => {
					var currentDefault = info.GetValue(defaultConf);
					var currentDefaultStr = currentDefault?.ToString();
					var propName = info.Name;

					if (ConfigDefaults.TryGetValue(propName, out var prevDefault)
						&& ActualValuesEqualBecauseMicrosoftHasBrainDamage(prevDefault, currentDefaultStr)
					) {
						log.Debug("config [{0:l}] does not need to be updated.", propName);
						return;
					}

					log.Debug("updating config [{0:l}] to the new default.", propName);
					ConfigDefaults[propName] = currentDefaultStr;
					info.SetValue(this, currentDefault);
				}
			);

		Save();
	}

	public void Save() {
		_pluginInterface.SavePluginConfig(this);
	}
}
