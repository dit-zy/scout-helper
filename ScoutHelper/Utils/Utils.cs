using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using ImGuiNET;
using Lumina.Text;
using ScoutHelper.Models;

namespace ScoutHelper.Utils;

public static partial class Utils {
	public static void CreateTooltip(string text, float width = 12f) {
		ImGui.BeginTooltip();
		ImGui.PushTextWrapPos(ImGui.GetFontSize() * width);
		ImGui.TextUnformatted(text);
		ImGui.PopTextWrapPos();
		ImGui.EndTooltip();
	}

	public static Vector2 V2(float x, float y) => new(x, y);

	public static T[] GetEnumValues<T>() where T : struct, Enum =>
		Enum.GetValuesAsUnderlyingType<T>() as T[] ?? Array.Empty<T>();

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
			{ "#max", highestPatch.MaxMarks().ToString() },
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

	public static StringBuilder PopLast(this StringBuilder builder) => builder.Remove(builder.Length - 1, 1);

	public static string Upper(this string s) => s.ToUpperInvariant();

	public static string Lower(this string s) => s.ToLowerInvariant();

	public static string Join(this IEnumerable<string> source, string? separator) => string.Join(separator, source);

	public static SeString ToSeString(this string str) => new(str);

	#endregion
}

public static class UtilExtensions {
	private static readonly IDictionary<Patch, uint> PatchMaxMarks = new Dictionary<Patch, uint>() {
		{ Patch.ARR, 17 },
		{ Patch.HW, 12 },
		{ Patch.SB, 12 },
		{ Patch.SHB, 12 },
		{ Patch.EW, 16 },
	}.VerifyEnumDictionary();

	private static readonly IDictionary<Patch, IList<string>> PatchHuntMaps = new Dictionary<Patch, IList<string>>() {
		{
			Patch.ARR, new List<string>() { }
		}, {
			Patch.HW, new List<string>() {
				"coerthas western highlands", "the sea of clouds", "azys lla",
				"the dravanian forelands", "the dravanian hinterlands", "the churning mists",
			}
		}, {
			Patch.SB, new List<string>() {
				"the fringes", "the peaks", "the lochs",
				"the ruby sea", "yanxia", "the azim steppe",
			}
		}, {
			Patch.SHB, new List<string>() {
				"lakeland", "kholusia", "amh araeng",
				"il mheg", "the rak'tika greatwood", "the tempest",
			}
		}, {
			Patch.EW, new List<string>() {
				"labyrinthos", "thavnair", "garlemald",
				"mare lamentorum", "elpis", "ultima thule",
			}
		},
	}.VerifyEnumDictionary();

	public static uint MaxMarks(this Patch patch) => PatchMaxMarks[patch];

	public static IList<string> HuntMaps(this Patch patch) => PatchHuntMaps[patch];
}
