using CSharpFunctionalExtensions;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using ScoutHelper.Localization;
using ScoutHelper.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ScoutHelper.Windows;

public class MainWindow : Window, IDisposable {

	private HuntHelperManager HuntHelperManager { get; init; }
	private BearManager BearManager { get; init; }

	public MainWindow(HuntHelperManager huntHelperManager, BearManager bearManager) : base(
		Strings.MainWindowTitle,
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
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
		var buttonSize = new List<string> {
				Strings.BearButton,
				Strings.SirenButton
			}
			.Select(ImGuiHelpers.GetButtonSize)
			.MaxBy(size => size.X);
		buttonSize.X += ImGui.GetFontSize();
			
		if (ImGui.Button(Strings.BearButton, buttonSize)) {
			GenerateBearLink();
		}
		if (ImGui.IsItemHovered()) {
			Utils.CreateTooltip(Strings.BearButtonTooltip);
		}

		ImGui.BeginDisabled(true);
		if (ImGui.Button(Strings.SirenButton, buttonSize)) { }
		ImGui.EndDisabled();
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
			Utils.CreateTooltip(Strings.SirenButtonTooltip);
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
