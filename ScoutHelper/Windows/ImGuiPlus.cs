using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using static OtterGui.Widgets.ToggleButton;

namespace ScoutHelper.Windows;

public static class ImGuiPlus {
	private static readonly Stack<Vector2> CursorPosStack = new(4);

	public static Vector2 WithCursorPos(Action<Vector2> action) =>
		WithCursorPos<object?>(
			cursorPos => {
				action.Invoke(cursorPos);
				return null;
			}
		).finalCursorPos;

	public static (Vector2 finalCursorPos, T result) WithCursorPos<T>(Func<T> function) =>
		WithCursorPos<T>(_ => function.Invoke());

	public static (Vector2 finalCursorPos, T result) WithCursorPos<T>(Func<Vector2, T> function) {
		var startingCursorPos = PushCursorPos();
		Vector2 finalCursorPos;
		T result;
		try {
			result = function.Invoke(startingCursorPos);
		} finally {
			finalCursorPos = PopCursorPos();
		}
		return (finalCursorPos, result);
	}

	public static Vector2 PushCursorPos() {
		var cursorPos = ImGui.GetCursorPos();
		CursorPosStack.Push(cursorPos);
		return cursorPos;
	}

	public static Vector2 PopCursorPos() {
		var finalCursorPos = ImGui.GetCursorPos();
		ImGui.SetCursorPos(CursorPosStack.Pop());
		return finalCursorPos;
	}

	public static void Heading(string text, float scale = 1.25f, bool centered = false) {
		var font = ImGui.GetFont();
		var originalScale = font.Scale;
		font.Scale *= scale;
		ImGui.PushFont(font);
		if (centered) ImGuiHelpers.CenteredText(text);
		else ImGui.Text(text);
		font.Scale = originalScale;
		ImGui.PopFont();
	}

	public static bool ClickableHelpMarker(string helpText, float width = 20f) =>
		ClickableHelpMarker(() => ImGui.TextUnformatted(helpText), width);

	public static bool ClickableHelpMarker(Action tooltipContents, float width = 20f) {
		ImGui.SameLine();

		var originalScale = UiBuilder.IconFont.Scale;
		UiBuilder.IconFont.Scale *= 0.60f;
		ImGui.PushFont(UiBuilder.IconFont);
		ImGui.TextDisabled(FontAwesomeIcon.Question.ToIconString());
		UiBuilder.IconFont.Scale = originalScale;
		ImGui.PopFont();

		var clicked = ImGui.IsItemClicked();

		if (ImGui.IsItemHovered()) {
			ImGui.BeginTooltip();
			ImGui.PushTextWrapPos(ImGui.GetFontSize() * width);
			tooltipContents.Invoke();
			ImGui.PopTextWrapPos();
			ImGui.EndTooltip();
		}

		return clicked;
	}

	public static bool ToggleBar(string strId, ref uint selection, Vector2 size, params string[] labels) {
		var newSelection = selection;
		var buttonSize = size with { X = size.X / labels.Length };

		var buttonConfigs = labels.Select(label => (label, cornerFlags: ImDrawFlags.RoundCornersNone)).ToArray();
		buttonConfigs[0].cornerFlags = ImDrawFlags.RoundCornersLeft;
		buttonConfigs[^1].cornerFlags = ImDrawFlags.RoundCornersRight;

		buttonConfigs.ForEach(
			(i, buttonConfig) => {
				if (0 < i) ImGui.SameLine(0, 0);
				ToggleBarButton(buttonConfig.label, i, ref newSelection, buttonSize, buttonConfig.cornerFlags);
			}
		);

		var selectionChanged = newSelection != selection;
		selection = newSelection;
		return selectionChanged;
	}

	private static unsafe bool ToggleBarButton(
		string label,
		uint buttonIndex,
		ref uint selection,
		Vector2 size,
		ImDrawFlags imDrawFlags
	) {
		var selected = selection == buttonIndex;

		var baseButtonColor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);
		var baseButtonColorActive = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonActive);
		var baseButtonColorHovered = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);

		var buttonColor = selected ? baseButtonColorActive : baseButtonColor;
		var buttonColorHovered = selected ? baseButtonColorActive : baseButtonColorHovered with { W = 0.4f };

		using var scopedColor1 = ImRaii.PushColor(ImGuiCol.Button, buttonColor);
		using var scopedColor2 = ImRaii.PushColor(ImGuiCol.ButtonHovered, buttonColorHovered);

		var pressed = ButtonEx(label, size, ImGuiButtonFlags.MouseButtonDefault, imDrawFlags);

		if (pressed) selection = buttonIndex;

		return pressed;
	}
}
