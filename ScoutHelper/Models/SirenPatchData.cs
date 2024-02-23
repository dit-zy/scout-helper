using System.Collections.Generic;
using System.Collections.Immutable;

namespace ScoutHelper.Models;

public record SirenPatchData(
	IList<SirenMapData> MobOrder,
	IDictionary<uint, IList<SirenSpawnPoint>> Maps
) {
	public static SirenPatchData From(IEnumerable<SirenMapData> mobOrder, IDictionary<uint, IList<SirenSpawnPoint>> maps) =>
		new(mobOrder.ToImmutable(), maps);
}
