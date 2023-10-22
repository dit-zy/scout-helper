using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace ScoutTrackerHelper.Windows;

public class MainWindow : Window, IDisposable {
	private readonly Plugin _plugin;

	public MainWindow(Plugin plugin, IDalamudTextureWrap goatImage) : base(
		"My Amazing Window",
		ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
	) {
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(375, 330),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
		};

		_plugin = plugin;
	}

	public void Dispose() { }

	public override void Draw() {

		if (ImGui.Button("Show Settings")) {
			_plugin.DrawConfigUi();
		}
	}
}
