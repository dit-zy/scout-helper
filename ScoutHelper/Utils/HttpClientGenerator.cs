using System;
using System.Net.Http;
using Dalamud.Plugin.Services;

namespace ScoutHelper.Utils;

public class HttpClientGenerator : IDisposable {
	private readonly IPluginLog _log;
	private readonly Func<string> _baseUrlSupplier;
	private readonly Action<HttpClient> _clientConfigurer;

	private HttpClient _client = new();

	public HttpClient Client {
		get {
			var latestUrl = _baseUrlSupplier().AsUri();
			if (_client.BaseAddress != latestUrl) {
				InitializeNewClient();
			}

			return _client;
		}
	}

	public HttpClientGenerator(IPluginLog log, Func<string> baseUrlSupplier, Action<HttpClient> clientConfigurer) {
		_log = log;
		_baseUrlSupplier = baseUrlSupplier;
		_clientConfigurer = clientConfigurer;

		InitializeNewClient();
	}

	public void Dispose() {
		_client.Dispose();

		GC.SuppressFinalize(this);
	}

	private void InitializeNewClient() {
		var client = new HttpClient();
		client.BaseAddress = _baseUrlSupplier().AsUri();
		_log.Debug("generating a new http client for base address: {0:l}", client.BaseAddress.ToString());
		client.DefaultRequestHeaders.UserAgent.Add(Constants.UserAgent);
		client.DefaultRequestHeaders.Accept.Add(Constants.MediaTypeJson);
		_clientConfigurer(client);

		_client.Dispose();
		_client = client;
	}
}
