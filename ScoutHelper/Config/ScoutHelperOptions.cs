using Microsoft.Extensions.Options;

namespace ScoutHelper.Config;

public record ScoutHelperOptions(
	string BearDataFile
) { }
