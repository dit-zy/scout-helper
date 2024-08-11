using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CSharpFunctionalExtensions;
using Newtonsoft.Json;

namespace ScoutHelper.Models.Http;

public record struct TurtleTrainUpdateRequest {
	[JsonProperty("collaborator_password")]
	public string CollaboratorPassword { get; }
	
	[JsonProperty("update_user")]
	public string? UpdateUser { get; }

	[JsonProperty("sightings")] public IList<TurtleTrainUpdateMark> Sightings { get; }

	public TurtleTrainUpdateRequest(
		string collaboratorPassword,
		Maybe<string> updateUser,
		IEnumerable<(uint zoneId, uint instance, uint mobId, Vector2 position)> marks
	) {
		CollaboratorPassword = collaboratorPassword;
		UpdateUser = updateUser.GetValueOrDefault();
		Sightings = marks
			.Select(mark => new TurtleTrainUpdateMark(mark.zoneId, mark.instance, mark.mobId, mark.position))
			.AsList();
	}
}
