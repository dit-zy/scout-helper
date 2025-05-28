using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CSharpFunctionalExtensions;
using Newtonsoft.Json;
using ScoutHelper.Utils;

namespace ScoutHelper.Models.Http;

public record struct TurtleTrainSpawnOccupiedRequest {
	[JsonProperty("collaborator_password")]
	public string CollaboratorPassword { get; }

	[JsonProperty("update_user")] public string? UpdateUser { get; }

	[JsonProperty("zone_id")] public uint ZoneId { get; }

	[JsonProperty("instance_number")] public uint InstanceNumber { get; }

	[JsonProperty("x")] public string X { get; }

	[JsonProperty("y")] public string Y { get; }

	[JsonProperty("status")] public uint Status { get; }

	public TurtleTrainSpawnOccupiedRequest(
		string collaboratorPassword,
		Maybe<string> updateUser,
		uint zoneId,
		uint instance,
		Vector2 position,
		bool occupied
	) {
		CollaboratorPassword = collaboratorPassword;
		UpdateUser = updateUser.GetValueOrDefault();
		ZoneId = zoneId;
		InstanceNumber = instance;
		X = position.X.AsTurtleApiString();
		Y = position.Y.AsTurtleApiString();
		Status = occupied ? 1U : 0U;
	}
}
