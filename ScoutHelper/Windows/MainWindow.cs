using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;
using DitzyExtensions.Functional;
using ImGuiNET;
using OtterGui.Widgets;
using ScoutHelper.Config;
using ScoutHelper.Localization;
using ScoutHelper.Managers;
using ScoutHelper.Utils;
using XIVHuntUtils.Models;
using XIVHuntUtils.Utils;
using static DitzyExtensions.MathUtils;
using static ScoutHelper.Utils.Utils;
using TrainMob = ScoutHelper.Models.TrainMob;

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

	private readonly IFramework _framework;
	private readonly IPluginLog _log;
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
	private readonly Lazy<float> _noticeFrameHeight;
	private readonly Lazy<float> _noticeFrameWrap;
	private readonly Lazy<float> _noticeFrameBorderSize;
	private readonly Lazy<Vector2> _noticeAckButtonPos;
	private readonly ISet<int> _alreadyContributedMobs = new HashSet<int>();

	private bool _isCopyModeFullText;
	private uint _selectedMode;
	private bool _latestNoticesAreAcknowledged;
	private Vector4 _noticeAckButtonColor = DangerFgColor;

	// turtle stuff
	private string _collabInput = "";
	private string _collabLink = "";
	private bool _closeTurtleCollabPopup = false;

	public MainWindow(
		IFramework framework,
		IPluginLog log,
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
		_framework = framework;
		_log = log;
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
		TitleBarButtons.Add(
			new TitleBarButton() {
				Click = _ => _configWindow.IsOpen = true,
				Icon = FontAwesomeIcon.Cog,
				IconOffset = V2(2, 1),
				ShowTooltip = () => ImGuiPlus.CreateTooltip("open the scout helper settings window"),
			}
		);

		_buttonSize = new Lazy<Vector2>(
			() => {
				try {
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
				} catch (Exception e) {
					_log.Error(e, "error while determining button size.");
					throw;
				}
			}
		);

		_notices = Constants.Notices;

		_noticeFrameBorderSize = new Lazy<float>(() => ImGuiHelpers.GlobalScaleSafe * (NoticeHasBorder ? 1 : 0));
		_noticeFrameWrap = new Lazy<float>(
			() => new[] {
				_buttonSize.Value.X,
				-2 * ImGui.GetStyle().FramePadding.X,
				-ImGui.CalcTextSize(NoticeBulletChar).X,
			}.Sum()
		);
		var noticeAckButtonSize = new Lazy<Vector2>(() => ImGuiHelpers.GetButtonSize(Strings.MainWindowNoticesAck));
		_noticeFrameHeight = new Lazy<float>(
			() => new[] {
				2 * ImGui.GetStyle().FramePadding.Y,
				ImGui.CalcTextSize(Strings.MainWindowSectionLabelNotices).Y,
				4 * ImGui.GetStyle().ItemSpacing.Y,
				noticeAckButtonSize.Value.Y,
				2 * _noticeFrameBorderSize.Value,
				_notices
					.Select(
						notice =>
							ImGui.CalcTextSize(notice, _noticeFrameWrap.Value).Y
							+ ImGui.GetStyle().ItemSpacing.Y
					)
					.Sum(),
			}.Sum()
		);
		_noticeAckButtonPos = new Lazy<Vector2>(
			() => V2(
				(_buttonSize.Value.X - noticeAckButtonSize.Value.X) / 2,
				_noticeFrameHeight.Value - noticeAckButtonSize.Value.Y - ImGui.GetStyle().FramePadding.Y -
				2 * _noticeFrameBorderSize.Value
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
						_buttonSize.Value.WithY(_noticeFrameHeight.Value),
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
										ImGui.PushTextWrapPos(_noticeFrameWrap.Value + ImGui.GetCursorPosX());
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
						.WithStyle(ImGuiStyleVar.FrameBorderSize, _noticeFrameBorderSize.Value)
						.Do(
							() => {
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
								ImGuiPlus.CreateTooltip(Strings.MainWindowNoticesAckTooltip);
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
		if (ImGuiPlus.ClickableHelpMarker(DrawModeTooltipContents)) {
			_configWindow.IsOpen = true;
		}

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

		if (ImGui.Button(Strings.BearButton, _buttonSize.Value)) {
			_chat.TaggedPrint("Generating Bear link...");
			GenerateLinkAsync(
				train => _bearManager.GenerateBearLink(_clientState.WorldName(), train),
				(trainList, bearTrainLink) => {
					_chat.TaggedPrint($"Bear train link: {bearTrainLink.Url}");
					_chat.TaggedPrint($"Train admin password: {bearTrainLink.Password}");
					CopyLink(trainList, "bear", bearTrainLink.HighestPatch, bearTrainLink.Url);
				}
			);
		}
		if (ImGui.IsItemHovered()) ImGuiPlus.CreateTooltip(Strings.BearButtonTooltip);

		if (ImGui.Button(Strings.SirenButton, _buttonSize.Value)) {
			_chat.TaggedPrint("Generating Siren link...");
			GenerateLink(
				train => _sirenManager.GenerateSirenLink(train),
				(trainList, sirenLink) => {
					sirenLink
						.ForEachError(errorMessage => _chat.TaggedPrintError(errorMessage))
						.Value
						.Execute(linkData => CopyLink(trainList, "siren", linkData.HighestPatch, linkData.Url));
				}
			);
		}
		if (ImGui.IsItemHovered()) ImGuiPlus.CreateTooltip(Strings.SirenButtonTooltip);

		DrawTurtleButtons();
		if (ImGui.BeginPopup("turtle collab popup")) {
			if (_closeTurtleCollabPopup) {
				_closeTurtleCollabPopup = false;
				ImGui.CloseCurrentPopup();
			} else {
				DrawTurtleCollabPopup();
			}
			ImGui.EndPopup();
		}
	}

	private unsafe void DrawTurtleButtons() {
		var itemSpacing = ImGui.GetStyle().ItemSpacing;
		var turtCollabButtonSize = _buttonSize.Value with {
			X = ImGuiHelpers.GetButtonSize(Strings.TurtleCollabButton).X - itemSpacing.Y
		};
		var turtButtonSize = (_buttonSize.Value - turtCollabButtonSize - itemSpacing.Transpose()) with {
			Y = _buttonSize.Value.Y
		};

		var turtlePressed = ToggleButton.ButtonEx(
			Strings.TurtleButton,
			turtButtonSize,
			ImGuiButtonFlags.MouseButtonDefault,
			ImDrawFlags.RoundCornersLeft
		);
		if (ImGui.IsItemHovered())
			ImGuiPlus.CreateTooltip(
				_turtleManager.IsTurtleCollabbing ? Strings.TurtleButtonActiveCollabTooltip : Strings.TurtleButtonTooltip
			);
		if (turtlePressed) {
			if (_turtleManager.IsTurtleCollabbing) {
				PushLatestMobsToTurtle();
			} else {
				_chat.TaggedPrint("Generating Turtle link...");
				GenerateLinkAsync(
					train => _turtleManager.GenerateTurtleLink(train),
					(trainList, turtleTrainLink) => {
						_chat.TaggedPrint($"Turtle train link: {turtleTrainLink.ReadonlyUrl}");
						_chat.TaggedPrint($"Turtle collaborate link: {turtleTrainLink.CollabUrl}");
						CopyLink(trainList, "turtle", turtleTrainLink.HighestPatch, turtleTrainLink.ReadonlyUrl);
					}
				);
			}
		}

		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() - itemSpacing.X + itemSpacing.Y);
		var collabColor = _turtleManager.IsTurtleCollabbing
			? *ImGui.GetStyleColorVec4(ImGuiCol.ButtonActive)
			: *ImGui.GetStyleColorVec4(ImGuiCol.Button);
		var turtleCollabPressed = ImGuiPlus
			.WithStyle(ImGuiCol.Button, collabColor)
			.Do(
				() => ToggleButton.ButtonEx(
					Strings.TurtleCollabButton,
					turtCollabButtonSize,
					ImGuiButtonFlags.MouseButtonDefault,
					ImDrawFlags.RoundCornersRight
				)
			);
		if (ImGui.IsItemHovered()) ImGuiPlus.CreateTooltip(Strings.TurtleCollabButtonTooltip);
		if (turtleCollabPressed) ImGui.OpenPopup("turtle collab popup");
	}

	private void DrawTurtleCollabPopup() {
		var contentWidth = 1.5f * _buttonSize.Value.X;
		ImGui.PushTextWrapPos(contentWidth);

		ImGuiPlus.Heading("SESSION", centered: true);

		ImGui.Checkbox("include name", ref _conf.IncludeNameInTurtleSession);
		ImGui.SameLine();
		ImGuiComponents.HelpMarker(
			"share your character name in the turtle session, so others can see that you contributed."
		);

		ImGui.Checkbox("include occupied spawns", ref _conf.IncludeOccupiedSpawnsInTurtleSession);
		ImGui.SameLine();
		ImGuiComponents.HelpMarker(
			"if a B-rank mark is spotted, mark that spawn location as 'occupied' in the scout tracker."
		);

		ImGui.BeginDisabled(!_turtleManager.IsTurtleCollabbing);
		if (ImGui.Button("LEAVE SESSION")) _turtleManager.LeaveCollabSession();
		ImGui.EndDisabled();

		ImGuiPlus.Separator();
		ImGuiPlus.Heading("NEW", centered: true);
		ImGui.TextWrapped("start a new scout session on turtle for other scouters to join and contribute to.");
		if (ImGui.Button("START NEW SESSION", _buttonSize.Value with { X = contentWidth })) {
			_turtleManager
				.GenerateTurtleLink(new List<TrainMob>(), allowEmpty: true)
				.Then(
					result => result.Match(
						linkData => {
							_collabInput = $"{linkData.Slug}/{linkData.CollabPassword}";
							if (JoinTurtleCollabSession(_collabInput)) _closeTurtleCollabPopup = true;
						},
						errorMessage => _chat.TaggedPrintError(errorMessage)
					)
				);
		}
		if (ImGui.IsItemHovered())
			ImGuiPlus.CreateTooltip(
				"generate a link to a new session, and immediately join it so you can start contributing. share the link with other scouters so they can also contribute :3"
			);

		ImGuiPlus.Separator();
		ImGuiPlus.Heading("CONTRIBUTE", centered: true);
		ImGui.TextWrapped("contribute scouted marks to an existing turtle session.");
		ImGui.SetNextItemWidth(contentWidth - ImGuiHelpers.GetButtonSize("JOIN").X - ImGui.GetStyle().ItemSpacing.X);
		var linkInputted = ImGui.InputTextWithHint(
			"",
			"https://scout.wobbuffet.net/scout/2WAZMI3DeZ/e5b2ede5",
			ref _collabInput,
			256,
			ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue
		);
		if (ImGui.IsItemHovered())
			ImGuiPlus.CreateTooltip("paste a collaborator link here and join the session to start contributing marks.");
		ImGui.SameLine();
		linkInputted = linkInputted || ImGui.Button("JOIN");
		if (linkInputted) {
			if (JoinTurtleCollabSession(_collabInput)) _closeTurtleCollabPopup = true;
		}

		ImGui.PopTextWrapPos();
	}

	private bool JoinTurtleCollabSession(string collabLink) {
		var collabInfo = _turtleManager.JoinCollabSession(collabLink);

		_collabLink = "";
		collabInfo
			.Match(
				sessionInfo => {
					_collabLink = $"{_conf.TurtleBaseUrl}{_conf.TurtleTrainPath}/{sessionInfo.slug}/{sessionInfo.password}";
					_chat.TaggedPrint($"joined turtle session: {_collabLink}");
					_alreadyContributedMobs.Clear();
				},
				() => _chat.TaggedPrintError($"failed to parse collab link. please ensure it is a valid link.\n{collabLink}")
			);

		return collabInfo.HasValue;
	}

	private void PushLatestMobsToTurtle() {
		// _chat.TaggedPrint($"Contributing marks to turtle session: {_collabLink}");
		GetTrainMobs(out _)
			.Map(
				train => {
					var trainSet = train.ToHashSet();
					trainSet.RemoveWhere(mob => _alreadyContributedMobs.Contains(mob.GetHashCode()));
					return trainSet.AsList();
				}
			)
			.Tap(train => _chat.TaggedPrint($"pushing {train.Count} new marks to: {_collabInput}"))
			.Bind(
				train => _turtleManager.UpdateCurrentSession(train)
					.Then(
						updateStatus => {
							if (updateStatus == TurtleHttpStatus.NoSupportedMobs)
								_chat.TaggedPrint("no mobs supported by turtle were in the newest batch of mobs.");

							return updateStatus is TurtleHttpStatus.Success or TurtleHttpStatus.NoSupportedMobs
								? Result.Success<IList<TrainMob>, string>(train)
								: "an error occurred while trying to update the marks in the current turtle session. please try again later.";
						}
					)
			)
			.Match(
				train => {
					train.ForEach(mob => _alreadyContributedMobs.Add(mob.GetHashCode()));
					_chat.TaggedPrint("turtle session updated!");
				},
				errorMessage => _chat.TaggedPrintError(errorMessage)
			)
			.ContinueWith(
				task => {
					if (task.IsCompletedSuccessfully) return;
					_log.Error(task.Exception, "uncaught exception when updating turtle session.");
				}
			);
	}

	private void GenerateLink<T>(
		Func<List<TrainMob>, AccumulatedResults<T, string>> linkGenerator,
		Action<IList<TrainMob>, AccumulatedResults<T, string>> onSuccess
	) {
		GetTrainMobs(out var trainList)
			.Map(linkGenerator)
			.Match(
				link => onSuccess(trainList, link),
				errorMessage => _chat.TaggedPrintError(errorMessage)
			);
	}

	private void GenerateLinkAsync<T>(
		Func<List<TrainMob>, Task<Result<T, string>>> linkGenerator,
		Action<IList<TrainMob>, T> onSuccess
	) {
		GetTrainMobs(out var trainList)
			.Bind(linkGenerator)
			.Match(
				link => onSuccess(trainList, link),
				errorMessage => { _chat.TaggedPrintError(errorMessage); }
			);
	}

	private Result<List<TrainMob>, string> GetTrainMobs(out IList<TrainMob> trainList) {
		var obtainedTrain = new List<TrainMob>();
		var result = _huntHelperManager
			.GetTrainList()
			.Ensure(
				train => train.IsNotEmpty(),
				"No mobs in the train :T"
			)
			.Tap(mobs => obtainedTrain = mobs);
		trainList = obtainedTrain.AsList();
		return result;
	}

	private async void CopyLink(IList<TrainMob> trainList, string tracker, Patch highestPatch, string link) {
		if (_isCopyModeFullText) {
			var fullText = await _framework.RunOnFrameworkThread(
				() => FormatTemplate(
					_conf.CopyTemplate,
					trainList,
					tracker,
					_clientState.WorldName(),
					highestPatch,
					link
				)
			);
			ImGui.SetClipboardText(fullText);
			_chat.TaggedPrint($"Copied full text to clipboard: {fullText}");
		} else {
			ImGui.SetClipboardText(link);
			_chat.TaggedPrint($"Copied link to clipboard: {link}");
		}
	}
}
