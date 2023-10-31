using Dalamud.Plugin.Services;
using System.IO;

namespace ScoutHelper;

public static class Utils {
	public static string WorldName {
		get => Plugin.ClientState.LocalPlayer?.CurrentWorld?.GameData?.Name.ToString() ?? "Not Found";
	}

	public static string DataFilePath(string dataFilename) => Path.Combine(
		Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
		"Data",
		dataFilename
	);

	#region extensions
	public static void TaggedPrint(this IChatGui chatGui, string message) {
		chatGui.Print(message, Plugin.Name);
	}

	public static void TaggedPrintError(this IChatGui chatGui, string message) {
		chatGui.PrintError(message, Plugin.Name);
	}
	#endregion
}
