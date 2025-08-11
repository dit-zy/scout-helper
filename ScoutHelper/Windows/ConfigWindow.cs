using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CSharpFunctionalExtensions;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using DitzyExtensions.Functional;
using Dalamud.Bindings.ImGui;
using ScoutHelper.Config;
using ScoutHelper.Localization;
using ScoutHelper.Utils;
using XIVHuntUtils.Managers;
using XIVHuntUtils.Models;
using static DitzyExtensions.MathUtils;
using static ScoutHelper.Utils.Utils;
using TrainMob = ScoutHelper.Models.TrainMob;

namespace ScoutHelper.Windows;

public class ConfigWindow : Window, IDisposable {
	private static readonly IList<TrainMob> PreviewTrainList = new[] {
			"Gourmand", "Chef's Kiss", "Little Mischief", "Poub"
		}
		.Select(
			name => new TrainMob(name, 3654, 1634, 3214, 2, V2(35, 64), false, DateTime.Now)
		)
		.ToImmutableList();

	private static readonly uint InputScalarStep = 1U;
	private static readonly Configuration DefaultConf = new();

	private readonly IClientState _clientState;
	private readonly IPluginLog _log;
	private readonly Configuration _conf;
	private readonly ITerritoryManager _territoryManager;

	private string _fullTextTemplate;
	private string _previewFullText = string.Empty;
	private bool _wasFocused = true;

	public ConfigWindow(
		IClientState clientState,
		IPluginLog log,
		Configuration conf,
		ITerritoryManager territoryManager
	) : base(Strings.ConfigWindowTitle) {
		_clientState = clientState;
		_log = log;
		_conf = conf;
		_territoryManager = territoryManager;

		_fullTextTemplate = _conf.CopyTemplate;

		SizeConstraints = new WindowSizeConstraints() {
			MinimumSize = V2(384, 256),
			MaximumSize = V2(float.MaxValue, float.MaxValue)
		};
	}

	public void Dispose() {
		UpdateConfig();

		GC.SuppressFinalize(this);
	}

	public override void OnOpen() {
		_previewFullText = ComputePreviewFullText();
	}

	public override void OnClose() {
		UpdateConfig();
	}

	private void UpdateConfig() {
		_conf.CopyTemplate = _fullTextTemplate;
		_conf.Save();

		_log.Debug("config saved");
	}

	public override void Draw() {
		if (!IsFocused && _wasFocused) UpdateConfig();

		_wasFocused = IsFocused;

		if (ImGui.BeginTabBar("conf_tabs")) {
			DrawTab("TEMPLATE", DrawTemplateTab);
			DrawTab("TWEAKS", DrawTweaksTab);
			ImGui.EndTabBar();
		}
	}

	private static void DrawTab(string label, Action contentAction) {
		if (ImGui.BeginTabItem(label)) {
			if (ImGui.BeginChild("tab_content")) {
				contentAction();
				ImGui.EndChild();
			}
			ImGui.EndTabItem();
		}
	}

	private void DrawTemplateTab() {
		ImGuiPlus.Heading(Strings.ConfigWindowSectionLabelFullText);
		DrawParagraphSpacing();
		DrawTextInput();
		DrawTemplatePreview();
		ImGui.NewLine();
		DrawTemplateDescription();
	}

	private void DrawTweaksTab() {
		DrawTweaksTrackerConfigs();
		ImGuiPlus.Separator();
		DrawTweaksInstances();
	}

	private void DrawTweaksTrackerConfigs() {
		ImGuiPlus.Heading("TRACKERS");
		DrawParagraphSpacing();

		ImGui.TextWrapped("reconfigure various internal values associated with the different trackers.");
		DrawParagraphSpacing();
		ImGui.TextWrapped(
			"NOTE: when scout helper updates one of these values in a new version, those new values will override any customizations you have here."
		);
		DrawParagraphSpacing();

		if (ImGui.Button("RESET ALL")) {
			_conf.BearApiBaseUrl = DefaultConf.BearApiBaseUrl;
			_conf.BearApiTrainPath = DefaultConf.BearApiTrainPath;
			_conf.BearSiteTrainUrl = DefaultConf.BearSiteTrainUrl;
			_conf.BearTrainName = DefaultConf.BearTrainName;
			_conf.SirenBaseUrl = DefaultConf.SirenBaseUrl;
			_conf.TurtleApiBaseUrl = DefaultConf.TurtleApiBaseUrl;
			_conf.TurtleApiTrainPath = DefaultConf.TurtleApiTrainPath;
			_conf.TurtleApiSpawnOccupiedPath = DefaultConf.TurtleApiSpawnOccupiedPath;
			_conf.TurtleBaseUrl = DefaultConf.TurtleBaseUrl;
			_conf.TurtleTrainPath = DefaultConf.TurtleTrainPath;
		}
		if (ImGui.IsItemHovered()) ImGuiPlus.CreateTooltip("reset all tracker configs to their defaults.");

		if (ImGui.TreeNode("BEAR")) {
			DrawConfigTextInput(nameof(Configuration.BearApiBaseUrl), ref _conf.BearApiBaseUrl);
			DrawConfigTextInput(nameof(Configuration.BearApiTrainPath), ref _conf.BearApiTrainPath);
			DrawConfigTextInput(nameof(Configuration.BearSiteTrainUrl), ref _conf.BearSiteTrainUrl);
			DrawConfigTextInput(nameof(Configuration.BearTrainName), ref _conf.BearTrainName);
			ImGui.TreePop();
		}

		if (ImGui.TreeNode("SIREN")) {
			DrawConfigTextInput(nameof(Configuration.SirenBaseUrl), ref _conf.SirenBaseUrl);
			ImGui.TreePop();
		}

		if (ImGui.TreeNode("TURTLE")) {
			DrawConfigTextInput(nameof(Configuration.TurtleApiBaseUrl), ref _conf.TurtleApiBaseUrl);
			DrawConfigTextInput(nameof(Configuration.TurtleApiTrainPath), ref _conf.TurtleApiTrainPath);
			DrawConfigTextInput(nameof(Configuration.TurtleApiSpawnOccupiedPath), ref _conf.TurtleApiSpawnOccupiedPath);
			DrawConfigTextInput(nameof(Configuration.TurtleBaseUrl), ref _conf.TurtleBaseUrl);
			DrawConfigTextInput(nameof(Configuration.TurtleTrainPath), ref _conf.TurtleTrainPath);
			ImGui.TreePop();
		}
	}

	private static void DrawConfigTextInput(string configName, ref string configRef) {
		var defaultValue = (string)typeof(Configuration).GetField(configName)!.GetValue(DefaultConf)!;
		ImGui.BeginDisabled(defaultValue == configRef);
		if (ImGuiComponents.IconButton(FontAwesomeIcon.Undo)) {
			configRef = defaultValue;
		}
		ImGui.EndDisabled();
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
			ImGuiPlus.CreateTooltip($"reset to default:\n{defaultValue}", 20);
		ImGui.SameLine();
		ImGui.InputText(configName, ref configRef, 256);
	}

	private void DrawTweaksInstances() {
		ImGuiPlus.Heading(Strings.ConfigWindowTweaksSectionLabelInstances);
		DrawParagraphSpacing();

		ImGui.TextWrapped(Strings.ConfigWindowTweaksInstanceDescription);
		DrawParagraphSpacing();
		ImGui.TextWrapped(Strings.ConfigWindowTweaksInstanceDescriptionNote);
		DrawParagraphSpacing();

		if (ImGui.Button(Strings.ConfigWindowTweaksInstanceResetButton)) {
			GetEnumValues<Territory>()
				.SelectResults(
					territory => _territoryManager
						.GetTerritoryId(territory.Name())
						.Map(territoryId => (territoryId, territory.DefaultInstances()))
				)
				.ForEachError(error => _log.Debug(error))
				.Value
				.UseToUpdate(_conf.Instances);
		}
		if (ImGui.IsItemHovered()) {
			ImGuiPlus.CreateTooltip(Strings.ConfigWindowTweaksInstanceResetTooltip);
		}

		var textSize = ImGui.CalcTextSize("8 ");
		var buttonsSize = ImGuiHelpers.GetButtonSize(" +  ");
		var spacing = ImGui.GetStyle().ItemSpacing;
		var inputSize = (textSize + 2 * (buttonsSize + spacing)).X;
		(Enum.GetValuesAsUnderlyingType<Patch>() as Patch[])!
			.OrderDescending()
			.ForEach(
				patch => {
					if (ImGui.TreeNode(patch.ToString())) {
						ImGui.PushItemWidth(inputSize);
						DrawPatchInstanceInputs(patch);
						ImGui.PopItemWidth();
						ImGui.TreePop();
					}
				}
			);
	}

	private unsafe void DrawPatchInstanceInputs(Patch patch) {
		var stepSize = InputScalarStep;
		var stepSizePointer = (IntPtr)Unsafe.AsPointer(ref stepSize);
		patch
			.HuntMaps()
			.ForEach(
				map => _territoryManager
					.FindTerritoryId(map.Name())
					.Select(
						mapId => ImGui.InputScalar<uint>(
							" " + _territoryManager.GetTerritoryName(mapId).Value,
							ImGuiDataType.U8,
							_conf.Instances.GetValuePointer(mapId),
							in stepSize,
							0U,
							"%d"
						)
					)
			);
	}

	private void DrawTextInput() {
		var textWasEdited = ImGui.InputTextMultiline(
			string.Empty,
			ref _fullTextTemplate,
			255,
			ImGui.GetContentRegionAvail() with { Y = ImGui.GetFontSize() * 4.75f }
		);
		if (ImGui.IsItemDeactivatedAfterEdit()) UpdateConfig();
		if (textWasEdited) _previewFullText = ComputePreviewFullText();

		if (ImGui.Button(Strings.ConfigWindowTemplateResetButton)) {
			_fullTextTemplate = Constants.DefaultCopyTemplate;
			UpdateConfig();
		}
		if (ImGui.IsItemHovered()) {
			ImGuiPlus.CreateTooltip($"{Strings.ConfigWindowTemplateResetTooltip}:\n    {Constants.DefaultCopyTemplate}");
		}
	}

	private void DrawTemplatePreview() {
		ImGui.Text(Strings.ConfigWindowPreviewLabel);
		ImGui.Indent();
		ImGui.TextDisabled(_previewFullText);
		ImGui.Unindent();
	}

	private static void DrawTemplateDescription() {
		ImGui.Text(Strings.ConfigWindowDescriptionLabel);
		DrawParagraphSpacing();
		ImGui.TextWrapped(Strings.ConfigWindowTemplateDesc);
		DrawParagraphSpacing();
		ImGui.Indent();
		ImGui.TextWrapped(Strings.ConfigWindowTemplateVariables);
		ImGui.Unindent();
	}

	private static void DrawParagraphSpacing() {
		ImGui.Dummy(V2(0, 0.50f * ImGui.GetFontSize()));
	}

	private string ComputePreviewFullText() => FormatTemplate(
		_fullTextTemplate,
		PreviewTrainList,
		"bear",
		_clientState.WorldName(),
		Patch.SHB,
		"https://example.com"
	);
}

internal static class ConfigWindowExtensions {
	public static Span<uint> GetValuePointer<K>(this Dictionary<K, uint> source, K key) where K : notnull {
		return new Span<uint>(ref CollectionsMarshal.GetValueRefOrNullRef(source, key));
	}
}
