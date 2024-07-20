﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
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

	public static Vector2 V2(float x) => new(x, x);

	public static Vector2 V2(float x, float y) => new(x, y);

	public static Vector4 V4(float x, float y, float z, float w) => new(x, y, z, w);

	public static Vector4 Color(uint r, uint g, uint b) => Color(r, g, b, 256);

	public static Vector4 Color(uint r, uint g, uint b, uint a) => Color((float)r, g, b, a) / 256;

	public static Vector4 Color(float r, float g, float b) => Color(r, g, b, 1f);

	public static Vector4 Color(float r, float g, float b, float a) => V4(r, g, b, a);

	public static T[] GetEnumValues<T>() where T : struct, Enum =>
		Enum.GetValuesAsUnderlyingType<T>() as T[] ?? Array.Empty<T>();

	public static Result<T, E> Try<T, E>(Func<T> action, Func<Exception, E> catchAction) {
		try {
			return action();
		} catch (Exception e) {
			return catchAction(e);
		}
	}

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
			{ "patch-emote", highestPatch.Emote() },
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

	public static float AsFloat(this string str) => float.Parse(str, CultureInfo.InvariantCulture);

	public static Vector2 Transpose(this Vector2 vec) => V2(vec.Y, vec.X);

	#endregion
}
