﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ScoutHelper.Models;

namespace ScoutHelper.Utils;

public static partial class XivExtensions {
	[GeneratedRegex(@" \d{1,2}$")] private static partial Regex InstanceRegex();

	public static string WorldName(this IClientState clientState) =>
		clientState.LocalPlayer?.CurrentWorld.ValueNullable?.Name.ToString() ?? "Not Found";

	public static Maybe<string> PlayerTag(this IClientState clientState) =>
		clientState
			.LocalPlayer
			.AsMaybe()
			.Select(
				player => {
					var playerName = player.Name.TextValue;
					var worldName = player.HomeWorld.ValueNullable?.Name.ToString() ?? "Unknown World";
					return $"{playerName}@{worldName}";
				}
			);

	public static string PluginFilePath(this IDalamudPluginInterface pluginInterface, string dataFilename) => Path.Combine(
		pluginInterface.AssemblyLocation.Directory?.FullName!,
		dataFilename
	);

	public static void TaggedPrint(this IChatGui chatGui, string message) {
		chatGui.Print(message, Plugin.Name);
	}

	public static void TaggedPrintError(this IChatGui chatGui, string message) {
		chatGui.PrintError(message, Plugin.Name);
	}

	public static Maybe<TrainMob> FindMob(this IEnumerable<TrainMob> mobList, uint mobId, uint? instance = null) =>
		Maybe.From(mobList.FirstOrDefault(mob => mob.MobId == mobId && mob.Instance == instance));

	public static string UnInstanced(this string mobName) => InstanceRegex().Replace(mobName, "");

	public static uint? Instance(this string mobName) => InstanceRegex()
		.Match(mobName)
		.AsMaybe()
		.Where(match => match.Success)
		.Map(match => uint.Parse(match.Value))
		.GetValueOrDefault();

	public static string AsEchoString(this float value) =>
		value.ToString("F1", CultureInfo.InvariantCulture);

	public static string AsEchoString(this Vector2 vector) =>
		$"({vector.X.AsEchoString()}, {vector.Y.AsEchoString()})";
}
