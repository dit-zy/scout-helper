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
	private static readonly string NoticeBulletChar = "▪";
	private static readonly bool NoticeHasBorder = true;

	private static readonly Vector4 DangerFgColor = Color(255, 235, 242);
	private static readonly Vector4 DangerBgColor = Color(181, 0, 69);

	private static readonly IList<(ImGuiCol, Vector4)> NoticeStyleColors = new[] {
		(ImGuiCol.FrameBg, DangerBgColor),
		(ImGuiCol.Button, DangerBgColor),
		(ImGuiCol.ButtonHovered, DangerFgColor),
		(ImGuiCol.Border, DangerFgColor),
		(ImGuiCol.Text, DangerFgColor),
	}.AsList();

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
	private readonly float _noticeFrameWrap;
	private readonly float _noticeFrameBorderSize;
	private readonly Lazy<Vector2> _noticeAckButtonPos;

	private bool _isCopyModeFullText;
	private uint _selectedMode;
	private bool _latestNoticesAreAcknowledged;
	private Vector4 _noticeAckButtonColor = DangerFgColor;

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

		_notices = Constants.Notices;

		var style = ImGui.GetStyle();
		_noticeFrameBorderSize = ImGuiHelpers.GlobalScale * (NoticeHasBorder ? 1 : 0);
		_noticeFrameWrap = new[] {
			_buttonSize.Value.X,
			-2 * style.FramePadding.X,
			-ImGui.CalcTextSize(NoticeBulletChar).X,
		}.Sum();
		var noticeAckButtonSize = ImGuiHelpers.GetButtonSize(Strings.MainWindowNoticesAck);
		_noticeFrameHeight = new[] {
			2 * style.FramePadding.Y,
			ImGui.CalcTextSize(Strings.MainWindowSectionLabelNotices).Y,
			4 * style.ItemSpacing.Y,
			noticeAckButtonSize.Y,
			2 * _noticeFrameBorderSize,
			_notices
				.Select(
					notice =>
						ImGui.CalcTextSize(notice, _noticeFrameWrap).Y
						+ style.ItemSpacing.Y
				)
				.Sum(),
		}.Sum();
		_noticeAckButtonPos = new Lazy<Vector2>(
			() => V2(
				(_buttonSize.Value.X - noticeAckButtonSize.X) / 2,
				_noticeFrameHeight - noticeAckButtonSize.Y - style.FramePadding.Y - 2 * _noticeFrameBorderSize
			)
		);

		_latestNoticesAreAcknowledged = Constants.LatestNoticeUpdate < _conf.LastNoticeAcknowledged;
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

	private unsafe void DrawNotices() {
		if (Constants.Notices.IsEmpty()) return;
		if (_latestNoticesAreAcknowledged) return;

		var textColor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);

		ImGuiPlus
			.WithStyle(NoticeStyleColors)
			.Do(
				() => {
					var startedChildFrame = ImGui.BeginChildFrame(
						ImGui.GetID("notice panel"),
						_buttonSize.Value.WithY(_noticeFrameHeight),
						ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysAutoResize
					);
					if (!startedChildFrame) return;

					ImGuiHelpers.CenteredText(Strings.MainWindowSectionLabelNotices);
					ImGuiPlus
						.WithStyle(ImGuiStyleVar.CellPadding, V2(0, 0))
						.Do(
							() => {
								if (!ImGui.BeginTable("notices", 2, ImGuiTableFlags.SizingFixedFit))
									return;

								_notices.ForEach(
									notice => {
										ImGui.TableNextRow();
										ImGui.TableNextColumn();
										ImGui.Text(NoticeBulletChar);

										ImGui.TableNextColumn();
										ImGui.PushTextWrapPos(_noticeFrameWrap + ImGui.GetCursorPosX());
										ImGui.TextWrapped(notice);
										ImGui.PopTextWrapPos();
									}
								);

								ImGui.EndTable();
							}
						);

					ImGui.Dummy(2 * V2(0, ImGui.GetStyle().ItemSpacing.Y));
					ImGuiPlus
						.WithStyle(ImGuiCol.Text, _noticeAckButtonColor)
						.WithStyle(ImGuiStyleVar.FrameBorderSize, _noticeFrameBorderSize)
						.Do(
							() => {
								var buttonSize = ImGuiHelpers.GetButtonSize(Strings.MainWindowNoticesAck);
								ImGui.SetCursorPos(_noticeAckButtonPos.Value);
								if (!ImGui.Button(Strings.MainWindowNoticesAck)) return;

								_conf.LastNoticeAcknowledged = DateTime.UtcNow;
								_conf.Save();
								_latestNoticesAreAcknowledged = true;
							}
						);
					ImGuiPlus.WithStyle(ImGuiCol.Text, textColor).Do(
						() => {
							if (ImGui.IsItemHovered()) {
								CreateTooltip(Strings.MainWindowNoticesAckTooltip);
								_noticeAckButtonColor = DangerBgColor;
							} else {
								_noticeAckButtonColor = DangerFgColor;
							}
						}
					);

					ImGui.EndChildFrame();

					ImGuiPlus.Separator();
				}
			);
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
