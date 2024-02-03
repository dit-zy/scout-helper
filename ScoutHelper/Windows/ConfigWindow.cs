using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using ScoutHelper.Config;
using ScoutHelper.Localization;
using ScoutHelper.Models;
using ScoutHelper.Utils;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Windows;

public class ConfigWindow : Window, IDisposable {
	private readonly IClientState _clientState;
	private readonly IPluginLog _log;
	private readonly Configuration _conf;

	private string _fullTextTemplate;
	private string _previewFullText;

	private static readonly IList<TrainMob> PreviewTrainList = new[] {
			"Gourmand", "Chef's Kiss", "Little Mischief", "Poub"
		}
		.Select(
			name => new TrainMob(name, 3654, 1634, 3214, 2, V2(35, 64), false, DateTime.Now)
		)
		.ToImmutableList();

	public ConfigWindow(IClientState clientState, IPluginLog log, Configuration conf) : base(
		Strings.ConfigWindowTitle
	) {
		_clientState = clientState;
		_log = log;
		_conf = conf;

		_fullTextTemplate = _conf.CopyTemplate;

		SizeConstraints = new WindowSizeConstraints() {
			MinimumSize = V2(384, 256),
			MaximumSize = V2(float.MaxValue, float.MaxValue)
		};

		_previewFullText = ComputePreviewFullText();
	}

	public void Dispose() {
		UpdateConfig();

		GC.SuppressFinalize(this);
	}

	private void UpdateConfig() {
		_conf.CopyTemplate = _fullTextTemplate;
		_conf.Save();

		_log.Debug("config saved");
	}

	public override void Draw() {
		ImGuiPlus.Heading(Strings.ConfigWindowSectionLabelFullText);
		DrawParagraphSpacing();
		DrawTextInput();
		DrawTemplatePreview();
		ImGui.NewLine();
		DrawTemplateDescription();
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
