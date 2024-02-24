using System.Collections.Generic;

namespace ScoutHelper.Models.Json;

public record SirenJsonPatchData(
	List<SirenJsonMapMobs> MobOrder,
	Dictionary<string, Dictionary<string, SirenJsonSpawnPoint>> Maps
);
