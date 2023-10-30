using CSharpFunctionalExtensions;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using ScoutTrackerHelper.Localization;
using ScoutTrackerHelper.Managers;
using System;
using System.Numerics;

namespace ScoutTrackerHelper.Windows;

public class MainWindow : Window, IDisposable {

	private HuntHelperManager HuntHelperManager { get; init; }
	private BearManager BearManager { get; init; }

	public MainWindow(HuntHelperManager huntHelperManager, BearManager bearManager) : base(
		Strings.MainWindowTitle,
		ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
	) {
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(64, 32),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
		};

		HuntHelperManager = huntHelperManager;
		BearManager = bearManager;
	}

	public void Dispose() {
		GC.SuppressFinalize(this);
	}

	public override void Draw() {
		if (ImGui.Button(Strings.TestButton)) {
			GenerateBearLink();
		}
	}
	private void GenerateBearLink() {
		Plugin.ChatGui.TaggedPrint("Generating Bear link...");

		HuntHelperManager
			.GetTrainList()
			.Ensure(
				train => 0 < train.Count,
				"No mobs in the train :T"
			)
			.Bind(
				train => BearManager.GenerateBearLink(Utils.WorldName, train)
			)
			.ContinueWith(
				apiResponseTask => {
					apiResponseTask
						.Result.Match(
							bearTrainLink => {
								Plugin.ChatGui.TaggedPrint($"Copied link to clipboard: {bearTrainLink.Url}");
								Plugin.ChatGui.TaggedPrint($"Train admin password: {bearTrainLink.Pass}");
								ImGui.SetClipboardText(bearTrainLink.Url);
							},
							errorMessage => {
								Plugin.ChatGui.TaggedPrintError(errorMessage);
							}
						);
				}
			);
	}
}
