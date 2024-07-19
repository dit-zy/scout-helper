using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CSharpFunctionalExtensions;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;
using ImGuiNET;
using OtterGui.Widgets;
using ScoutHelper.Config;
using ScoutHelper.Localization;
using ScoutHelper.Managers;
using ScoutHelper.Models;
using ScoutHelper.Utils;
using ScoutHelper.Utils.Functional;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Windows;

public class MainWindow : Window, IDisposable {
	private readonly IClientState _clientState;
	private readonly Configuration _conf;
	private readonly IChatGui _chat;
	private readonly HuntHelperManager _huntHelperManager;
	private readonly BearManager _bearManager;
	private readonly SirenManager _sirenManager;
	private readonly TurtleManager _turtleManager;
	private readonly ConfigWindow _configWindow;

	private readonly Lazy<Vector2> _buttonSize;
	private readonly IList<string> _notices;
	private readonly float _noticeFrameHeight;

	private bool _isCopyModeFullText;
	private uint _selectedMode;

	public MainWindow(
		IClientState clientState,
		Configuration conf,
		IChatGui chat,
		HuntHelperManager huntHelperManager,
		BearManager bearManager,
		SirenManager sirenManager,
		TurtleManager turtleManager,
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
		_turtleManager = turtleManager;
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
						[Strings.BearButton],
						[Strings.SirenButton],
						[Strings.TurtleButton, Strings.TurtleCollabButton],
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

		_notices = Constants.Notices
			.Select(notice => "· " + notice)
			.AsList();

		var noticeWrapWidth = _buttonSize.Value.X - ImGui.GetStyle().WindowPadding.X * 2;
		_noticeFrameHeight = ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.CalcTextSize(
			_notices
				.Append("NOTICES")
				.Join("\n"),
			noticeWrapWidth
		).Y;
	}

	public void Dispose() {
		_conf.IsCopyModeFullText = _isCopyModeFullText;
		_conf.Save();

		GC.SuppressFinalize(this);
	}

	public override void Draw() {
		DrawNotices();

		DrawModeButtons();

		ImGuiPlus.Separator();

		DrawGeneratorButtons();
	}

	private void DrawNotices() {
		if (Constants.Notices.IsEmpty())
			return;

		try {
			ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(.7f, .0f, .2f, 1f));
			ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 1f));

			var startedChildFrame = ImGui.BeginChildFrame(
				ImGui.GetID("notice panel"),
				_buttonSize.Value.WithY(_noticeFrameHeight),
				ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize
			);
			if (!startedChildFrame) return;

			ImGuiHelpers.CenteredText("NOTICES");
			_notices.ForEach(ImGui.TextWrapped);
		} finally {
			ImGui.PopStyleColor(2);
		}
		ImGui.EndChildFrame();

		ImGuiPlus.Separator();
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
		if (ImGui.IsItemHovered()) CreateTooltip(Strings.SirenButtonTooltip);

		var turtCollabButtonSize = _buttonSize.Value with { X = ImGuiHelpers.GetButtonSize(Strings.TurtleCollabButton).X };
		var turtButtonSize = (_buttonSize.Value - turtCollabButtonSize) with { Y = _buttonSize.Value.Y };
		var turtlePressed = ToggleButton.ButtonEx(
			Strings.TurtleButton,
			turtButtonSize,
			ImGuiButtonFlags.MouseButtonDefault,
			ImDrawFlags.RoundCornersLeft
		);
		if (turtlePressed) GenerateTurtleLink();
		if (ImGui.IsItemHovered()) CreateTooltip(Strings.TurtleButtonTooltip);

		ImGui.SameLine(); ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.GetStyle().ItemSpacing.X);
		ImGui.BeginDisabled(true);
		ToggleButton.ButtonEx(
			Strings.TurtleCollabButton,
			turtCollabButtonSize,
			ImGuiButtonFlags.MouseButtonDefault,
			ImDrawFlags.RoundCornersRight
		);
		ImGui.EndDisabled();
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) CreateTooltip(Strings.TurtleCollabButtonTooltip);
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
					sirenLink
						.ForEachError(errorMessage => _chat.TaggedPrintError(errorMessage))
						.Value
						.Execute(linkData => CopyLink(trainList, "siren", linkData.HighestPatch, linkData.Url));
				},
				errorMessage => _chat.TaggedPrintError(errorMessage)
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
								CopyLink(trainList, "bear", bearTrainLink.HighestPatch, bearTrainLink.Url);
							},
							errorMessage => { _chat.TaggedPrintError(errorMessage); }
						);
				}
			);
	}

	private void GenerateTurtleLink() {
		_chat.TaggedPrint("Generating Turtle link...");
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
					return _turtleManager.GenerateTurtleLink(train);
				}
			)
			.ContinueWith(
				apiResponseTask => {
					apiResponseTask
						.Result.Match(
							turtleTrainLink => {
								_chat.TaggedPrint($"Turtle train link: {turtleTrainLink.ReadonlyUrl}");
								_chat.TaggedPrint($"Turtle collaborate link: {turtleTrainLink.CollabUrl}");
								CopyLink(trainList, "turtle", turtleTrainLink.HighestPatch, turtleTrainLink.ReadonlyUrl);
							},
							errorMessage => { _chat.TaggedPrintError(errorMessage); }
						);
				}
			);
	}

	private void CopyLink(IList<TrainMob> trainList, string tracker, Patch highestPatch, string link) {
		if (_isCopyModeFullText) {
			var fullText = FormatTemplate(
				_conf.CopyTemplate,
				trainList,
				tracker,
				_clientState.WorldName(),
				highestPatch,
				link
			);
			ImGui.SetClipboardText(fullText);
			_chat.TaggedPrint($"Copied full text to clipboard: {fullText}");
		} else {
			ImGui.SetClipboardText(link);
			_chat.TaggedPrint($"Copied link to clipboard: {link}");
		}
	}
}
