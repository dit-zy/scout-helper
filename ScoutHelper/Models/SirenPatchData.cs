using System.Collections.Generic;
using System.Collections.Immutable;

namespace ScoutHelper.Models;

public record SirenPatchData(
	IList<(uint mobId, uint? instance)> MobOrder,
	IDictionary<uint, IList<SirenSpawnPoint>> Maps
) {
	public static SirenPatchData From(IEnumerable<(uint, uint?)> mobOrder, IDictionary<uint, IList<SirenSpawnPoint>> maps) =>
		new(mobOrder.ToImmutableList(), maps);
}
