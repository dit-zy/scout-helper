using System;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace ScoutHelper.Config;

[Serializable]
public class Configuration : IPluginConfiguration {

	// the below exists just to make saving less cumbersome
	[NonSerialized]
	private DalamudPluginInterface _pluginInterface = null!;

	public int Version { get; set; } = 0;

	public string BearApiBaseUrl { get; set; } = "https://tracker-api.beartoolkit.com/public/";
	public string BearApiTrainPath { get; set; } = "hunttrain";
	public TimeSpan BearApiTimeout { get; set; } = TimeSpan.FromSeconds(10);
	public string BearSiteTrainUrl { get; set; } = "https://tracker.beartoolkit.com/train";
	public string BearTrainName { get; set; } = "Scout Helper Train";

	public string SirenBaseUrl { get; set; } = "https://sirenhunts.com/scouting/";

	public string CopyTemplate { get; set; } = Constants.DefaultCopyTemplate;
	public bool IsCopyModeFullText { get; set; } = false;

	public Dictionary<uint, uint> Instances { get; set; } = new();

	public void Initialize(DalamudPluginInterface pluginInterface) {
		_pluginInterface = pluginInterface;
	}

	public void Save() {
		_pluginInterface.SavePluginConfig(this);
	}
}
