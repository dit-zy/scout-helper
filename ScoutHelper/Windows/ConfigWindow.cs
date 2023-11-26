using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ScoutHelper.Localization;
using ScoutHelper.Models;
using static ScoutHelper.Utils;

namespace ScoutHelper.Windows;

public class ConfigWindow : Window, IDisposable {
	private string _fullTextTemplate = Plugin.Conf.CopyTemplate;
	private string _previewFullText;

	private static readonly IList<TrainMob> PreviewTrainList = new[] {
			"Gourmand", "Chef's Kiss", "Little Mischief", "Poub"
		}
		.Select(
			name => new TrainMob(name, 3654, 1634, 3214, 2, V2(35, 64), false, DateTime.Now)
		)
		.ToImmutableList();

	public ConfigWindow() : base(
		Strings.ConfigWindowTitle
	) {
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
		Plugin.Conf.CopyTemplate = _fullTextTemplate;
		Plugin.Conf.Save();

		Plugin.Log.Debug("config saved");
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

		ImGui.Button(Strings.ConfigWindowTemplateResetButton);
		if (ImGui.IsItemHovered()) {
			ImGui.SetTooltip($"{Strings.ConfigWindowTemplateResetTooltip}:\n\n  {Constants.DefaultCopyTemplate}");
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
		Patch.SHB,
		"https://example.com"
	);
}
