using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ScoutHelper.Models.Http;

using TurtleTrainRequestMapData = IDictionary<string, IList<TurtleTrainRequestPointData>>;

public record TurtleTrainRequest {
	[JsonProperty("custom_points")] public IList<string> CustomPoints { get; } = [];

	[JsonProperty("point_data")]
	public IDictionary<string, TurtleTrainRequestMapData> PointData { get; init; }
		= new Dictionary<string, TurtleTrainRequestMapData>();

	public static TurtleTrainRequest CreateRequest(
		IEnumerable<(uint mapId, uint instance, uint pointId, uint mobId)> foundMobs
	) {
		return new TurtleTrainRequest() {
			PointData = foundMobs
				.Select(point => (point.mapId.ToString(), point.instance.ToString(), point.pointId, point.mobId))
				.Select(point => (point.Item1, (point.Item2, new TurtleTrainRequestPointData(point.mobId, point.pointId))))
				.GroupBy(point => point.Item1)
				.Select(
					pointGroup =>
						(pointGroup.Key.ToString(), pointGroup.Select(point => point.Item2).AsMultiDict())
				)
				.ToDict()
		};
	}
}
