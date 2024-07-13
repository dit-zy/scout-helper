using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace ScoutHelper.Models;

public record TurtleMapData(
	uint TurtleId,
	IDictionary<uint, Vector2> SpawnPoints
);
