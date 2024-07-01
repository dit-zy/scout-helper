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
	[GeneratedRegex(@" \d{1,2}$")] private static partial Regex InstanceRegex();

	public static string WorldName(this IClientState clientState) =>
		clientState.LocalPlayer?.CurrentWorld.GameData?.Name.ToString() ?? "Not Found";

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
}
