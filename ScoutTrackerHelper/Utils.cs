using Dalamud.Plugin.Services;

namespace ScoutTrackerHelper;

public static class Utils {
	public static string WorldName {
		get => Plugin.ClientState.LocalPlayer?.CurrentWorld?.GameData?.Name.ToString() ?? "Not Found";
	}
	public static void TaggedPrint(this IChatGui chatGui, string message) {
		chatGui.Print(message, Plugin.Name);
	}
	public static void TaggedPrintError(this IChatGui chatGui, string message) {
		chatGui.PrintError(message, Plugin.Name);
	}
}
