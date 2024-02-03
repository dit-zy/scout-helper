using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ScoutHelper.Models;

namespace ScoutHelper.Utils;

public static partial class XivExtensions {
	[GeneratedRegex(@" \d{1,2}$")] private static partial Regex UnInstanceRegex();

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

	public static Maybe<TrainMob> FindMob(this IEnumerable<TrainMob> mobList, uint mobId) =>
		Maybe.From(mobList.FirstOrDefault(mob => mob.MobId == mobId));

	public static string UnInstanced(this string mobName) => UnInstanceRegex().Replace(mobName, "");
}
