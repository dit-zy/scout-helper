using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CSharpFunctionalExtensions;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ScoutHelper.Localization;
using ScoutHelper.Managers;
using ScoutHelper.Models;

namespace ScoutHelper.Windows;

public class MainWindow : Window, IDisposable {
	private HuntHelperManager HuntHelperManager { get; }
	private BearManager BearManager { get; }
	private ConfigWindow ConfigWindow { get; }

	private readonly Lazy<Vector2> _buttonSize;
	
	private bool _isCopyModeFullText = Plugin.Conf.IsCopyModeFullText;
	private uint _selectedMode = Plugin.Conf.IsCopyModeFullText ? 1U : 0U;

	public MainWindow(HuntHelperManager huntHelperManager, BearManager bearManager, ConfigWindow configWindow) : base(
		Strings.MainWindowTitle,
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
	) {
		SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(64, 32),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
		};

		_buttonSize = new Lazy<Vector2>(
			() => {
				var buttonSize = new[] {
						new[] { Strings.BearButton },
						new[] { Strings.SirenButton },
						new[] { Strings.CopyModeLinkButton, Strings.CopyModeFullTextButton, },
					}
					.Select(
						labels => labels
							.Select(ImGuiHelpers.GetButtonSize)
							.Aggregate((a, b) => new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)))
					)
					.MaxBy(size => size.X);
				buttonSize.X += 4 * ImGui.GetFontSize(); // add some horizontal padding
				return buttonSize;
			}
		);

		HuntHelperManager = huntHelperManager;
		BearManager = bearManager;
		ConfigWindow = configWindow;
	}

	public void Dispose() {
		Plugin.Conf.IsCopyModeFullText = _isCopyModeFullText;
		Plugin.Conf.Save();

		GC.SuppressFinalize(this);
	}

	public override void Draw() {
		DrawModeButtons();

		ImGui.Dummy(new Vector2(0, ImGui.GetStyle().FramePadding.Y));
		ImGui.Separator();
		ImGui.Dummy(new Vector2(0, ImGui.GetStyle().FramePadding.Y));

		DrawGeneratorButtons();
	}

	private void DrawModeButtons() {
		ImGuiHelpers.CenteredText(Strings.MainWindowSectionLabelMode);

		ImGui.SameLine();
		if (ImGuiPlus.ClickableHelpMarker(DrawModeTooltipContents)) ConfigWindow.IsOpen = true;

		var modes = new[] { Strings.CopyModeLinkButton, Strings.CopyModeFullTextButton };
		if (ImGuiPlus.ToggleBar("mode", ref _selectedMode, _buttonSize.Value, modes))
			_isCopyModeFullText = _selectedMode == 1;
	}

	private static void DrawModeTooltipContents() {
		ImGui.TextUnformatted(Strings.CopyModeTooltipSummary);

		ImGui.NewLine();

		ImGui.TextUnformatted(Strings.CopyModeLinkButton);
		ImGui.Indent();
		ImGui.TextUnformatted(Strings.CopyModeTooltipLinkDesc);
		ImGui.Unindent();

		ImGui.NewLine();

		ImGui.TextUnformatted(Strings.CopyModeFullTextButton);
		ImGui.Indent();
		ImGui.TextUnformatted(Strings.CopyModeTooltipFullTextDesc);
		ImGui.Unindent();
	}

	private void DrawGeneratorButtons() {
		ImGuiHelpers.CenteredText(Strings.MainWindowSectionLabelGenerators);

		if (ImGui.Button(Strings.BearButton, _buttonSize.Value)) GenerateBearLink();
		if (ImGui.IsItemHovered()) Utils.CreateTooltip(Strings.BearButtonTooltip);

		ImGui.BeginDisabled(true);
		ImGui.Button(Strings.SirenButton, _buttonSize.Value);
		ImGui.EndDisabled();
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) Utils.CreateTooltip(Strings.SirenButtonTooltip);
	}

	private void GenerateBearLink() {
		Plugin.ChatGui.TaggedPrint("Generating Bear link...");
		IList<TrainMob> trainList = null!;

		HuntHelperManager
			.GetTrainList()
			.Ensure(
				train => 0 < train.Count,
				"No mobs in the train :T"
			)
			.Bind(
				train => {
					trainList = train;
					return BearManager.GenerateBearLink(Utils.WorldName, train);
				}
			)
			.ContinueWith(
				apiResponseTask => {
					apiResponseTask
						.Result.Match(
							bearTrainLink => {
								Plugin.ChatGui.TaggedPrint($"Bear train link: {bearTrainLink.Url}");
								Plugin.ChatGui.TaggedPrint($"Train admin password: {bearTrainLink.Password}");
								if (_isCopyModeFullText) {
									var fullText = Utils.FormatTemplate(
										Plugin.Conf.CopyTemplate,
										trainList,
										"bear",
										bearTrainLink.HighestPatch,
										bearTrainLink.Url
									);
									ImGui.SetClipboardText(fullText);
									Plugin.ChatGui.TaggedPrint($"Copied full text to clipboard: {fullText}");
								} else {
									ImGui.SetClipboardText(bearTrainLink.Url);
									Plugin.ChatGui.TaggedPrint("Copied link to clipboard");
								}
							},
							errorMessage => { Plugin.ChatGui.TaggedPrintError(errorMessage); }
						);
				}
			);
	}
}
