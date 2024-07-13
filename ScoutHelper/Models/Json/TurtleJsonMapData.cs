using System.Collections.Generic;

namespace ScoutHelper.Models.Json;

public record TurtleJsonMapData(
	uint Id,
	IDictionary<uint, TurtleJsonSpawnPoint> Points
);
