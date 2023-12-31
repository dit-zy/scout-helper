using System.Net.Http.Headers;
using System.Reflection;

namespace ScoutHelper;

public static class Constants {
	#region plugin constants

	public const string PluginName = "Scout Helper";
	public static readonly string PluginVersion;
	public static readonly string PluginNamespace = PluginName.Replace(" ", "");

	#endregion

	#region core constants

	public const string DefaultCopyTemplate = "{patch} {#}/{#max} {world} [{tracker}]({link})";
	public const string BearDataFile = @"Data\Bear.json";

	#endregion

	#region web constants

	public static readonly MediaTypeWithQualityHeaderValue MediaTypeJson =
		MediaTypeWithQualityHeaderValue.Parse("application/json");

	public static readonly ProductInfoHeaderValue UserAgent = new ProductInfoHeaderValue(
		PluginNamespace,
		PluginVersion
	);

	#endregion

	static Constants() {
		PluginVersion = Assembly.GetCallingAssembly().GetName().Version?.ToString() ?? "?.?.?.?";
	}
}
