using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpFunctionalExtensions;
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

public class ConfigWindow : Window, IDisposable {
	private static readonly IList<TrainMob> PreviewTrainList = new[] {
			"Gourmand", "Chef's Kiss", "Little Mischief", "Poub"
		}
		.Select(
			name => new TrainMob(name, 3654, 1634, 3214, 2, V2(35, 64), false, DateTime.Now)
		)
		.ToImmutableList();

	private static readonly PointerRef<uint> InputScalarStep = new(1U);

	private readonly IClientState _clientState;
	private readonly IPluginLog _log;
	private readonly Configuration _conf;
	private readonly TerritoryManager _territoryManager;

	private readonly IDictionary<uint, PointerRef<uint>> _instances = new Dictionary<uint, PointerRef<uint>>();

	private string _fullTextTemplate;
	private string _previewFullText;
	private bool _wasFocused = true;

	public ConfigWindow(
		IClientState clientState,
		IPluginLog log,
		Configuration conf,
		TerritoryManager territoryManager
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

		_conf
			.Instances
			.ForEach(entry => _instances[entry.Key] = new PointerRef<uint>(entry.Value));

		_previewFullText = ComputePreviewFullText();
	}

	public void Dispose() {
		UpdateConfig();

		GC.SuppressFinalize(this);
	}

	public override void OnClose() {
		UpdateConfig();
	}

	private void UpdateConfig() {
		_conf.CopyTemplate = _fullTextTemplate;
		_conf.Instances.Update(
			_instances
				.AsPairs()
				.Select(entry => (entry.key, entry.val.GetValue()))
		);
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
		ImGuiPlus.Heading("INSTANCES");
		DrawParagraphSpacing();

		ImGui.Text("Configure how many instances there are for each map:");

		(Enum.GetValuesAsUnderlyingType<Patch>() as Patch[])!
			.OrderDescending()
			.ForEach(
				patch => {
					if (ImGui.TreeNode(patch.ToString())) {
						ImGui.PushItemWidth(ImGuiPlus.ScaledFontSize() * 4);
						DrawPatchInstanceInputs(patch);
						ImGui.PopItemWidth();
						ImGui.TreePop();
					}
				}
			);
	}

	private unsafe void DrawPatchInstanceInputs(Patch patch) => patch
		.HuntMaps()
		.ForEach(
			mapName => _territoryManager
				.GetTerritoryId(mapName)
				.Select(
					mapId => ImGui.InputScalar(
						_territoryManager.GetTerritoryName(mapId).Value,
						ImGuiDataType.U8,
						(IntPtr)_instances[mapId].GetPointer(),
						(IntPtr)InputScalarStep.GetPointer()
					)
				)
		);

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
			ImGui.SetTooltip($"{Strings.ConfigWindowTemplateResetTooltip}:\n    {Constants.DefaultCopyTemplate}");
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
