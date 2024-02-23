using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

namespace ScoutHelper.Models;

public record SirenMapData(
	uint MapId,
	IList<uint> Mobs
) {
	public static SirenMapData From(uint mapId, IEnumerable<uint> mobs) =>
		new(mapId, mobs.ToImmutable());
}
