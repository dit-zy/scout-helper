using System.Collections.Generic;
using System.Collections.Immutable;

namespace ScoutHelper.Models;

public record SirenPatchData(
	IList<uint> MobOrder,
	IDictionary<uint, IList<SirenSpawnPoint>> Maps
) {
	public static SirenPatchData From(IEnumerable<uint> mobOrder, IDictionary<uint, IList<SirenSpawnPoint>> maps) =>
		new(mobOrder.ToImmutableList(), maps);
}
