using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ImGuiNET;
using ScoutHelper.Models;

namespace ScoutHelper;

public static partial class Utils {
	private static readonly IReadOnlyDictionary<Patch, uint> PatchMaxMarks = new Dictionary<Patch, uint>() {
		{ Patch.ARR, 17 },
		{ Patch.HW, 12 },
		{ Patch.SB, 12 },
		{ Patch.SHB, 12 },
		{ Patch.EW, 16 },
	}.VerifyEnumDictionary();

	public static void CreateTooltip(string text, float width = 12f) {
		ImGui.BeginTooltip();
		ImGui.PushTextWrapPos(ImGui.GetFontSize() * width);
		ImGui.TextUnformatted(text);
		ImGui.PopTextWrapPos();
		ImGui.EndTooltip();
	}

	public static Vector2 V2(float x, float y) => new(x, y);

	[GeneratedRegex(@"\\?\{((?!\\?\}).)+\\?\}", RegexOptions.IgnoreCase)]
	private static partial Regex TemplateParseRegex();

	public static string FormatTemplate(
		string textTemplate,
		IList<TrainMob> trainList,
		string tracker,
		string worldName,
		Patch highestPatch,
		string link
	) {
		var matches = TemplateParseRegex().Matches(textTemplate);

		if (matches.Count == 0) {
			return textTemplate;
		}

		var variables = new Dictionary<string, string>() {
			{ "#", trainList.Count.ToString() },
			{ "#max", PatchMaxMarks[highestPatch].ToString() },
			{ "link", link },
			{ "patch", highestPatch.ToString() },
			{ "tracker", tracker },
			{ "world", worldName },
		}.AsReadOnly();

		var s = new StringBuilder(textTemplate.Length);
		var i = 0;
		foreach (Match match in matches) {
			var m = match.Value;
			if (m[0] == '\\' || m[^2] == '\\') continue;

			var variable = m.Substring(1, m.Length - 2).ToLowerInvariant();
			if (!variables.ContainsKey(variable)) continue;

			s.Append(textTemplate.AsSpan(i, match.Index - i));
			s.Append(variables[variable]);

			i = match.Index + match.Length;
		}

		if (i < textTemplate.Length) {
			s.Append(textTemplate.AsSpan(i));
		}

		return s
			.ToString()
			.Replace("\\{", "{")
			.Replace("\\}", "}");
	}

	#region extensions

	public static string WorldName(this IClientState clientState) =>
		clientState.LocalPlayer?.CurrentWorld.GameData?.Name.ToString() ?? "Not Found";

	public static string PluginFilePath(this DalamudPluginInterface pluginInterface, string dataFilename) => Path.Combine(
		pluginInterface.AssemblyLocation.Directory?.FullName!,
		dataFilename
	);

	public static void TaggedPrint(this IChatGui chatGui, string message) {
		chatGui.Print(message, Plugin.Name);
	}

	public static void TaggedPrintError(this IChatGui chatGui, string message) {
		chatGui.PrintError(message, Plugin.Name);
	}

	public static IReadOnlyDictionary<K, V> VerifyEnumDictionary<K, V>(this IDictionary<K, V> enumDict)
		where K : struct, Enum {
		var allEnumsAreInDict = (Enum.GetValuesAsUnderlyingType<K>() as K[])!.All(enumDict.ContainsKey);
		if (!allEnumsAreInDict) {
			throw new Exception($"All values of enum [{typeof(K).Name}] must be in the dictionary.");
		}

		return enumDict.ToImmutableDictionary();
	}

	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action) =>
		source.ForEach((value, _) => action.Invoke(value));

	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T, int> action) {
		var values = source as T[] ?? source.ToArray();
		for (var i = 0; i < values.Length; ++i) {
			action.Invoke(values[i], i);
		}
		return values;
	}

	#endregion
}
