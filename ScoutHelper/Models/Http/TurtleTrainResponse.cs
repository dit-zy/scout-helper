using Newtonsoft.Json;

namespace ScoutHelper.Models.Http;

public record struct TurtleTrainResponse {
	[JsonProperty("slug")] public string Slug { get; init; }

	[JsonProperty("collaborator_password")]
	public string CollaboratorPassword { get; init; }

	[JsonProperty("readonly_url")] public string ReadonlyUrl { get; init; }

	[JsonProperty("collaborate_url")] public string CollaborateUrl { get; init; }

	[JsonConstructor]
	public TurtleTrainResponse(string slug, string collaboratorPassword, string readonlyUrl, string collaborateUrl) {
		Slug = slug;
		CollaboratorPassword = collaboratorPassword;
		ReadonlyUrl = readonlyUrl;
		CollaborateUrl = collaborateUrl;
	}
}
