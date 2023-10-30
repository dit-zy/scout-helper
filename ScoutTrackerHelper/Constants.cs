using System.Net.Http.Headers;

namespace ScoutTrackerHelper;

public static class Constants {

	#region plugin constants
	public const string PluginName = "Scout Helper";
	public const string PluginVersion = "0.1.0";
	#endregion

	#region web constants
	public static readonly MediaTypeWithQualityHeaderValue MediaTypeJson =
		MediaTypeWithQualityHeaderValue.Parse("application/json");
	public static readonly ProductInfoHeaderValue UserAgent = new ProductInfoHeaderValue(
		PluginName.Replace(" ", ""),
		PluginVersion
	);
	#endregion
}
