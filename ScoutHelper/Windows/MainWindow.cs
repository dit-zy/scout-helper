using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CSharpFunctionalExtensions;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using ScoutHelper.Config;
using ScoutHelper.Localization;
using ScoutHelper.Managers;
using ScoutHelper.Models;
using ScoutHelper.Utils;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Windows;

public class MainWindow : Window, IDisposable {
	private readonly IClientState _clientState;
	private readonly Configuration _conf;
	private readonly IChatGui _chat;
	private readonly HuntHelperManager _huntHelperManager;
	private readonly BearManager _bearManager;
	private readonly SirenManager _sirenManager;
	private readonly ConfigWindow _configWindow;

	private readonly Lazy<Vector2> _buttonSize;

	private bool _isCopyModeFullText;
	private uint _selectedMode;

	public MainWindow(
		IClientState clientState,
		Configuration conf,
		IChatGui chat,
		HuntHelperManager huntHelperManager,
		BearManager bearManager,
		SirenManager sirenManager,
		ConfigWindow configWindow
	) : base(
		Strings.MainWindowTitle,
		ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
	) {
		_clientState = clientState;
		_conf = conf;
		_chat = chat;
		_huntHelperManager = huntHelperManager;
		_bearManager = bearManager;
		_sirenManager = sirenManager;
		_configWindow = configWindow;

		_isCopyModeFullText = _conf.IsCopyModeFullText;
		_selectedMode = _isCopyModeFullText ? 1U : 0U;

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
	}

	public void Dispose() {
		_conf.IsCopyModeFullText = _isCopyModeFullText;
		_conf.Save();

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
		if (ImGuiPlus.ClickableHelpMarker(DrawModeTooltipContents)) _configWindow.IsOpen = true;

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
		if (ImGui.IsItemHovered()) CreateTooltip(Strings.BearButtonTooltip);

		if (ImGui.Button(Strings.SirenButton, _buttonSize.Value)) GenerateSirenLink();
		ImGui.BeginDisabled(true);
		ImGui.Button(Strings.SirenButton, _buttonSize.Value);
		ImGui.EndDisabled();
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) CreateTooltip(Strings.SirenButtonTooltip);
	}

	private void GenerateSirenLink() {
		_chat.TaggedPrint("Generating Siren link...");
		IList<TrainMob> trainList = null!;

		_huntHelperManager
			.GetTrainList()
			.Ensure(
				train => 0 < train.Count,
				"No mobs in the train :T"
			)
			.Map(
				train => {
					trainList = train;
					return _sirenManager.GenerateSirenLink(train);
				}
			)
			.Match(
				sirenLink => {
					if (_isCopyModeFullText) {
						var fullText = FormatTemplate(
							_conf.CopyTemplate,
							trainList,
							"siren",
							_clientState.WorldName(),
							sirenLink.HighestPatch,
							sirenLink.Url
						);
						ImGui.SetClipboardText(fullText);
						_chat.TaggedPrint($"Copied full text to clipboard: {fullText}");
					} else {
						ImGui.SetClipboardText(sirenLink.Url);
						_chat.TaggedPrint($"Copied link to clipboard: {sirenLink.Url}");
					}
				},
				errorMessage => { _chat.TaggedPrintError(errorMessage); }
			);
	}

	private void GenerateBearLink() {
		_chat.TaggedPrint("Generating Bear link...");
		IList<TrainMob> trainList = null!;

		_huntHelperManager
			.GetTrainList()
			.Ensure(
				train => 0 < train.Count,
				"No mobs in the train :T"
			)
			.Bind(
				train => {
					trainList = train;
					return _bearManager.GenerateBearLink(_clientState.WorldName(), train);
				}
			)
			.ContinueWith(
				apiResponseTask => {
					apiResponseTask
						.Result.Match(
							bearTrainLink => {
								_chat.TaggedPrint($"Bear train link: {bearTrainLink.Url}");
								_chat.TaggedPrint($"Train admin password: {bearTrainLink.Password}");
								if (_isCopyModeFullText) {
									var fullText = FormatTemplate(
										_conf.CopyTemplate,
										trainList,
										"bear",
										_clientState.WorldName(),
										bearTrainLink.HighestPatch,
										bearTrainLink.Url
									);
									ImGui.SetClipboardText(fullText);
									_chat.TaggedPrint($"Copied full text to clipboard: {fullText}");
								} else {
									ImGui.SetClipboardText(bearTrainLink.Url);
									_chat.TaggedPrint("Copied link to clipboard");
								}
							},
							errorMessage => { _chat.TaggedPrintError(errorMessage); }
						);
				}
			);
	}
}
