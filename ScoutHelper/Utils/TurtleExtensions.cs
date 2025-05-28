using System.Globalization;

namespace ScoutHelper.Utils;

public static class TurtleExtensions {
	public static uint AsTurtleInstance(this uint? instance) {
		return instance is null or 0 ? 1 : (uint)instance;
	}

	public static string AsTurtleApiString(this float value) =>
		value.ToString("F2", CultureInfo.InvariantCulture);
}
