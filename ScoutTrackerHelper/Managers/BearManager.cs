using CSharpFunctionalExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ScoutTrackerHelper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ScoutTrackerHelper.Managers;

public class BearManager : IDisposable {

	private static HttpClient HttpClient { get; } = new();

	public BearManager() {
		HttpClient.BaseAddress = new Uri(Plugin.Conf.BearApiBaseUrl);
		HttpClient.DefaultRequestHeaders.UserAgent.Add(Constants.UserAgent);
		HttpClient.Timeout = Plugin.Conf.BearApiTimeout;
	}

	public void Dispose() {
		HttpClient.Dispose();

		GC.SuppressFinalize(this);
	}

	private static BearApiSpawnPoint CreateRequestSpawnPoint(TrainMob mob) =>
		new(
			mob.Name + (mob.Instance is < 1 or > 9 ? "" : $" {mob.Instance}"),
			mob.Position.X,
			mob.Position.Y,
			mob.LastSeenUtc
		);

	public async Task<Result<(string Url, string Pass), string>> GenerateBearLink(
		string worldName,
		IEnumerable<TrainMob> trainMobs
	) {
		var spawnPoints = trainMobs.Select(CreateRequestSpawnPoint).ToList();
		var request = JsonConvert.SerializeObject(
			new BearApiTrainRequest(worldName, Plugin.Conf.BearTrainName, "EW", spawnPoints)
		);
		Plugin.Log.Verbose(request);
		var content = new StringContent(request, Encoding.UTF8, Constants.MediaTypeJson);

		try {
			var response = await HttpClient.PostAsync(Plugin.Conf.BearApiTrainPath, content);
			response.EnsureSuccessStatusCode();
			var responseJson = await response.Content.ReadAsStringAsync();
			var trainInfo = JsonConvert
				.DeserializeObject<BearApiTrainResponse>(responseJson)
				.Trains.First();
			var url = $"{Plugin.Conf.BearSiteTrainUrl}/{trainInfo.TrainId}";
			return (url, trainInfo.Password);
		}
		catch (TimeoutException e) {
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
			return "Something failed communicating with Bear :T";
		}
		catch (Exception e) {
			const string message = "An unknown error happened while generating the Bear link D:";
			Plugin.Log.Error(e, message);
			return message;
		}
	}
}
