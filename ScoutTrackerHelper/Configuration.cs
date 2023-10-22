using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace ScoutTrackerHelper;

[Serializable]
public class Configuration : IPluginConfiguration {

	// the below exist just to make saving less cumbersome
	[NonSerialized]
	private DalamudPluginInterface? _pluginInterface;

	public int Version { get; set; } = 0;

	public void Initialize(DalamudPluginInterface pluginInterface) {
		this._pluginInterface = pluginInterface;
	}

	public void Save() {
		_pluginInterface!.SavePluginConfig(this);
	}
}
