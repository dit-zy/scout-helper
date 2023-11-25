using System;
using System.Numerics;

namespace ScoutHelper.Models;

public record struct TrainMob(
	string Name,
	uint MobId,
	uint TerritoryId,
	uint MapId,
	uint? Instance,
	Vector2 Position,
	bool Dead,
	DateTime LastSeenUtc
);
