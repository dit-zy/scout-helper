using System.Collections.Generic;

namespace ScoutHelper.Models.Json;

public record SirenJsonMapMobs(
	string Map,
	List<string> Mobs
);
