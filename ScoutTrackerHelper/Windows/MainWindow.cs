using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using ScoutTrackerHelper.Managers;
using System;
using System.Numerics;

namespace ScoutTrackerHelper.Windows;

using LocStrings = Localization.Strings;

public class MainWindow : Window, IDisposable {

	private readonly HuntHelperManager _huntHelperManager;

	public MainWindow(HuntHelperManager huntHelperManager) : base(
		"My Amazing Window",
		ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
	) {
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(64, 32),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
		};

		_huntHelperManager = huntHelperManager;
	}

	public void Dispose() { }

	public override void Draw() {
		if (ImGui.Button(LocStrings.TestButton)) {
			var trainList = _huntHelperManager.GetTrainList();
			if (trainList == null) {
				Plugin.ChatGui.Print("Could not get train list ;-;");
			}
			else if (trainList.Count == 0) {
				Plugin.ChatGui.Print("No mobs in the train :T");
			}
			else {
				trainList.ForEach(mob => Plugin.ChatGui.Print(mob.ToString()!));
			}
		}
	}
}
