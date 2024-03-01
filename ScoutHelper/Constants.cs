using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Reflection;
using ScoutHelper.Models;

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
	public const string SirenDataFile = @"Data\Siren.json";

	public static readonly DateTime LatestPatchUpdate = DateTime.Parse("2024-01-01T00:00:00Z");
	public static readonly (Territory, uint)[] LatestPatchInstances = {};

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
