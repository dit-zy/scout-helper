using CSharpFunctionalExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScoutHelper.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ScoutHelper.Managers;

public class BearManager : IDisposable {

	private static HttpClient HttpClient { get; } = new();

	private IDictionary<uint, (Patch patch, string name)> MobIdToBearName { get; init; }

	public BearManager(string dataFilePath) {
		HttpClient.BaseAddress = new Uri(Plugin.Conf.BearApiBaseUrl);
		HttpClient.DefaultRequestHeaders.UserAgent.Add(Constants.UserAgent);
		HttpClient.Timeout = Plugin.Conf.BearApiTimeout;

		MobIdToBearName = LoadData(dataFilePath);
	}

	public void Dispose() {
		HttpClient.Dispose();

		GC.SuppressFinalize(this);
	}

	private static IDictionary<uint, (Patch patch, string name)> LoadData(string dataFilePath) {

		if (!File.Exists(dataFilePath)) {
			throw new Exception($"Can't find {dataFilePath}");
		}

		var data = JsonConvert.DeserializeObject<IDictionary<string, JObject>>(File.ReadAllText(dataFilePath));
		if (data == null) {
			throw new Exception("Failed to read in Bear data ;-;");
		}

		return data
			.SelectMany(
				patchData => {
					if (!Enum.TryParse(patchData.Key, out Patch patch)) {
						throw new Exception($"Unknown patch: {patchData.Key}");
					}
					return (patchData.Value as IDictionary<string, JToken>).Select(
						mob => {
							var mobName = mob.Key;
							var mobId = (uint)mob.Value;
							return (mobName, patch, mobId);
						}
					);
				}
			)
			.ToImmutableDictionary(
				mob => mob.mobId,
				mob => (mob.patch, mob.mobName)
			);
	}

	private BearApiSpawnPoint CreateRequestSpawnPoint(TrainMob mob) {
		var huntName = MobIdToBearName[mob.MobId].name;
		if (mob.Instance is >= 1 and <= 9) {
			huntName += $" {mob.Instance}";
		}
		return new BearApiSpawnPoint(
			huntName,
			mob.Position.X,
			mob.Position.Y,
			mob.LastSeenUtc
		);
	}

	public async Task<Result<(string Url, string Pass), string>> GenerateBearLink(
		string worldName,
		IList<TrainMob> trainMobs
	) {
		var bearSupportedMobs = trainMobs.Where(mob => MobIdToBearName.ContainsKey(mob.MobId)).ToList();
		var spawnPoints = bearSupportedMobs.Select(CreateRequestSpawnPoint).ToList();
		var patchName = bearSupportedMobs
			.Select(mob => MobIdToBearName[mob.MobId].patch)
			.Distinct()
			.Max()
			.BearName();

		var requestPayload = JsonConvert.SerializeObject(
			new BearApiTrainRequest(worldName, Plugin.Conf.BearTrainName, patchName, spawnPoints)
		);
		Plugin.Log.Debug("Request payload: {0}", requestPayload);
		var requestContent = new StringContent(requestPayload, Encoding.UTF8, Constants.MediaTypeJson);

		try {
			var response = await HttpClient.PostAsync(Plugin.Conf.BearApiTrainPath, requestContent);
			Plugin.Log.Debug(
				"Request: {0}\n\nResponse: {1}",
				response.RequestMessage!.ToString(),
				response.ToString()
			);

			response.EnsureSuccessStatusCode();

			var responseJson = await response.Content.ReadAsStringAsync();
			var trainInfo = JsonConvert.DeserializeObject<BearApiTrainResponse>(responseJson).Trains.First();

			var url = $"{Plugin.Conf.BearSiteTrainUrl}/{trainInfo.TrainId}";
			return (url, trainInfo.Password);
		}
		catch (TimeoutException) {
			const string message = "Timed out posting the train to Bear ;-;";
			Plugin.Log.Error(message);
			return message;
		}
		catch (OperationCanceledException e) {
			const string message = "Generating the Bear link was canceled >_>";
			Plugin.Log.Warning(e, message);
			return message;
		}
		catch (HttpRequestException e) {
			Plugin.Log.Error(e, "Posting the train to Bear failed.");
			return "Something failed when communicating with Bear :T";
		}
		catch (Exception e) {
			const string message = "An unknown error happened while generating the Bear link D:";
			Plugin.Log.Error(e, message);
			return message;
		}
	}
}

public static class BearExtensions {
	private static readonly IDictionary<Patch, string> BearPatchNames = new Dictionary<Patch, string> {
		{Patch.ARR, "ARR"},
		{Patch.HW, "HW"},
		{Patch.SB, "SB"},
		{Patch.SHB, "ShB"},
		{Patch.EW, "EW"}
	}.VerifyEnumDictionary();
	
	public static string BearName(this Patch patch) {
		return BearPatchNames[patch];
	}
}
