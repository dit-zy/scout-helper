using System.Collections.Generic;
using Newtonsoft.Json;

namespace ScoutHelper.Models.Http;

public record TurtleTrainRequestPointData {
	[JsonProperty("mob_id")] public uint MobId { get; init; }
	[JsonProperty("point_id")] public uint PointId { get; init; }

	public TurtleTrainRequestPointData(uint mobId, uint pointId) {
		MobId = mobId;
		PointId = pointId;
	}
};
