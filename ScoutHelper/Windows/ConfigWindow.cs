using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ScoutHelper.Localization;
using System;
using System.Numerics;

namespace ScoutHelper.Windows;

public class ConfigWindow : Window, IDisposable {

	public ConfigWindow() : base(
		Strings.ConfigWindowTitle,
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar
	) {
		SizeConstraints = new WindowSizeConstraints() {
			MinimumSize = new Vector2(128 * ImGuiHelpers.GlobalScale, 0),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};
	}

	public void Dispose() { }

	public override void Draw() {
		ImGui.TextWrapped(Strings.ConfigWindowContent);
	}
}
