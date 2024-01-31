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
	// visible for testing
	public static readonly IReadOnlyDictionary<Patch, uint> PatchMaxMarks = new Dictionary<Patch, uint>() {
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
		var tokens = Tokenize(textTemplate);

		tokens.ForEach(
			token => {
				if (token is ['{', _, .., '}'] && variables.TryGetValue(token[1..^1], out var value)) {
					s.Append(value);
					return;
				}

				s.Append(token);
			}
		);

		return s.ToString();
	}

	private static IEnumerable<string> Tokenize(string s) {
		var tokens = new List<string>();

		var nextToken = new StringBuilder();
		var varStarted = false;

		for (var i = 0; i < s.Length; i++) {
			var c = s[i];
			nextToken.Append(c);

			if (c == '\\') {
				if (i == s.Length - 1) continue;

				if (s[i + 1] == '\\' || s[i + 1] == '{' || s[i + 1] == '}') {
					nextToken.PopLast();
					nextToken.Append(s[i + 1]);
					i++;
				}
				continue;
			}

			if (c == '{') {
				nextToken.PopLast();
				tokens.Add(nextToken.ToString());
				nextToken.Clear();
				nextToken.Append('{');
				varStarted = true;
				continue;
			}

			if (c == '}' && varStarted) {
				tokens.Add(nextToken.ToString());
				nextToken.Clear();
				varStarted = false;
			}
		}

		if (0 < nextToken.Length) tokens.Add(nextToken.ToString());

		return tokens;
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

	public static IDictionary<K, V> ToDict<K, V>(this IEnumerable<KeyValuePair<K, V>> source) where K : notnull =>
		source.Select(entry => (entry.Key, entry.Value)).ToDict();

	public static IDictionary<K, V> ToDict<K, V>(this IEnumerable<(K, V)> source) where K : notnull =>
		source.ToImmutableDictionary(entry => entry.Item1, entry => entry.Item2);

	public static StringBuilder PopLast(this StringBuilder builder) => builder.Remove(builder.Length - 1, 1);

	#endregion
}
