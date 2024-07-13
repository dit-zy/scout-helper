using System.Collections.Generic;

namespace ScoutHelper.Models.Json;

public record TurtleJsonPatchData(
	uint Id,
	IDictionary<string, uint> Mobs,
	IDictionary<string, TurtleJsonMapData> Maps
);
