using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;

namespace ScoutHelper.Models.Http;

public record struct TurtleTrainUpdateRequest {
	[JsonProperty("collaborator_password")]
	public string CollaboratorPassword { get; }

	[JsonProperty("sightings")] public IList<TurtleTrainUpdateMark> Sightings { get; }

	public TurtleTrainUpdateRequest(
		string collaboratorPassword,
		IEnumerable<(uint zoneId, uint instance, uint mobId, Vector2 position)> marks
	) {
		CollaboratorPassword = collaboratorPassword;
		Sightings = marks
			.Select(mark => new TurtleTrainUpdateMark(mark.zoneId, mark.instance, mark.mobId, mark.position))
			.AsList();
	}
}
