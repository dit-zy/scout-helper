using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace ScoutHelper.Models;

public record BearApiTrainRequest {
	[JsonProperty("worldName")] public string WorldName { get; init; }
	[JsonProperty("trainName")] public string TrainName { get; init; }
	[JsonProperty("patchName")] public string PatchName { get; init; }
	[JsonProperty("spawnpoints")] public IList<BearApiSpawnPoint> SpawnPoints { get; init; }

	[JsonConstructor]
	public BearApiTrainRequest(string worldName, string trainName, string patchName, IList<BearApiSpawnPoint> spawnPoints) {
		WorldName = worldName;
		TrainName = trainName;
		PatchName = patchName;
		SpawnPoints = spawnPoints;
	}
}

public record BearApiSpawnPoint {
	[JsonProperty("huntName")] public string HuntName { get; init; }
	[JsonProperty("pos_x")] public float PosX { get; init; }
	[JsonProperty("pos_y")] public float PosY { get; init; }
	[JsonProperty("time")] public DateTime Time { get; init; }

	[JsonConstructor]
	public BearApiSpawnPoint(string huntName, float posX, float posY, DateTime time) {
		HuntName = huntName;
		PosX = posX;
		PosY = posY;
		Time = time;
	}
}

public record struct BearApiTrainResponse {
	[JsonProperty("trains")] public IList<BearApiTrain> Trains { get; init; }

	[JsonConstructor]
	public BearApiTrainResponse(IList<BearApiTrain> trains) {
		Trains = trains;
	}
}

public record struct BearApiTrain {
	[JsonProperty("trainId")] public string TrainId { get; init; }
	[JsonProperty("password")] public string Password { get; init; }

	[JsonConstructor]
	public BearApiTrain(string trainId, string password) {
		TrainId = trainId;
		Password = password;
	}
}
