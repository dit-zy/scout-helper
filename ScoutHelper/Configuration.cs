using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace ScoutHelper;

[Serializable]
public class Configuration : IPluginConfiguration {

	// the below exist just to make saving less cumbersome
	[NonSerialized]
	private DalamudPluginInterface? _pluginInterface;

	public int Version { get; set; } = 0;

	public string BearApiBaseUrl { get; set; } = "https://tracker-api.beartoolkit.com/public/";
	public string BearApiTrainPath { get; set; } = "hunttrain";
	public TimeSpan BearApiTimeout { get; set; } = TimeSpan.FromSeconds(10);
	public string BearSiteTrainUrl { get; set; } = "https://tracker.beartoolkit.com/train";
	public string BearTrainName { get; set; } = "Scout Helper Train";

	public void Initialize(DalamudPluginInterface pluginInterface) {
		this._pluginInterface = pluginInterface;
	}

	public void Save() {
		_pluginInterface!.SavePluginConfig(this);
	}
}
