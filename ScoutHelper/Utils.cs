using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Plugin.Services;
using ImGuiNET;
using ScoutHelper.Models;

namespace ScoutHelper;

public static class Utils {
	public static string WorldName =>
		Plugin.ClientState.LocalPlayer?.CurrentWorld?.GameData?.Name.ToString() ?? "Not Found";

	public static string PluginFilePath(string dataFilename) => Path.Combine(
		Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
		dataFilename
	);

	public static void CreateTooltip(string text, float width = 12f) {
		ImGui.BeginTooltip();
		ImGui.PushTextWrapPos(ImGui.GetFontSize() * width);
		ImGui.TextUnformatted(text);
		ImGui.PopTextWrapPos();
		ImGui.EndTooltip();
	}

	public static Vector2 V2(float x, float y) => new(x, y);

	public static string FormatTemplate(
		string textTemplate,
		IList<TrainMob> trainList,
		string tracker,
		Patch highestPatch,
		string url
	) {
		var regex = new Regex(@"\\?\{[^{}]+\\?\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		var matches = regex.Matches(textTemplate);

		if (matches.Count == 0) {
			return textTemplate;
		}

		var variables = new Dictionary<string, string>() {
			{ "#", trainList.Count.ToString() },
			{ "#max", trainList.Count.ToString() },
			{ "link", url },
			{ "patch", highestPatch.ToString() },
			{ "tracker", tracker },
			{ "world", WorldName },
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

		return s.ToString();
	}

	#region extensions

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
		source.ForEach((_, value) => action.Invoke(value));

	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<uint, T> action) {
		var values = source as T[] ?? source.ToArray();
		for (var i = 0U; i < values.Length; ++i) {
			action.Invoke(i, values[i]);
		}

		return values;
	}

	#endregion
}
