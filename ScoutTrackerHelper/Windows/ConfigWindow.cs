using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace ScoutTrackerHelper.Windows;

public class ConfigWindow : Window, IDisposable {
	private const ImGuiWindowFlags WindowFlags =
		ImGuiWindowFlags.NoResize |
		ImGuiWindowFlags.NoCollapse |
		ImGuiWindowFlags.NoScrollbar |
		ImGuiWindowFlags.NoScrollWithMouse;

	public ConfigWindow() : base("A Wonderful Configuration Window", WindowFlags) {
		Size = new Vector2(232, 75);
		SizeCondition = ImGuiCond.FirstUseEver;
	}

	public void Dispose() { }

	public override void Draw() { }
}
