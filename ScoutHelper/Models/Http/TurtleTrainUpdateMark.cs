using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Common.Math;
using Newtonsoft.Json;

namespace ScoutHelper.Models.Http;

public record struct TurtleTrainUpdateMark {
	[JsonProperty("zone_id")] public uint ZoneId { get; }
	[JsonProperty("mob_id")] public uint MobId { get; }
	[JsonProperty("instance_number")] public uint Instance { get; }
	[JsonProperty("x")] public string X { get; }
	[JsonProperty("y")] public string Y { get; }

	public TurtleTrainUpdateMark(uint zoneId, uint? instance, uint mobId, Vector2 position) {
		ZoneId = zoneId;
		Instance = (uint)((instance ?? 0) < 1 ? 1 : instance!);
		MobId = mobId;
		X = position.X.ToString("F2");
		Y = position.Y.ToString("F2");
	}
}
