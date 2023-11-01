using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

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

	public static IDictionary<K, V> VerifyEnumDictionary<K, V>(this IDictionary<K,V> enumDict) where K : struct, Enum {
		var allEnumsAreInDict = (Enum.GetValuesAsUnderlyingType<K>() as K[])!.All(enumDict.ContainsKey);
		if (!allEnumsAreInDict) {
			throw new Exception($"All values of enum [{typeof(K).Name}] must be in the dictionary.");
		}
		return enumDict.ToImmutableDictionary();
	}
	#endregion
}
