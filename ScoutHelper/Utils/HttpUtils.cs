using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;
using ScoutHelper.Models.Http;

namespace ScoutHelper.Utils;

public static class HttpUtils {
	public static Task<Result<U, HttpError>> DoRequest<T, U>(
		IPluginLog log,
		T requestObject,
		Func<HttpContent, Task<HttpResponseMessage>> requestAction
	) =>
		DoRequest(log, requestObject, requestAction)
			.Then(
				result => result.Bind<string, U, HttpError>(
					responseJson => Utils.Try(
						() => JsonConvert.DeserializeObject<U>(responseJson)!,
						e => new HttpError(HttpErrorType.Unknown, e)
					)
				)
			);

	public static async Task<Result<string, HttpError>> DoRequest<T>(
		IPluginLog log,
		T requestObject,
		Func<HttpContent, Task<HttpResponseMessage>> requestAction
	) {
		try {
			var requestPayload = JsonConvert.SerializeObject(requestObject);
			log.Debug("Request body: {0}", requestPayload);
			var requestContent = new StringContent(requestPayload, Encoding.UTF8, Constants.MediaTypeJson);

			var response = await requestAction(requestContent);
			log.Debug(
				"Request: {0}\n\nResponse: {1}",
				response.RequestMessage!.ToString(),
				response.ToString()
			);

			response.EnsureSuccessStatusCode();

			var responseJson = await response.Content.ReadAsStringAsync();
			log.Debug("Response body: {0}", responseJson);
			return responseJson;
		} catch (TimeoutException) {
			return new HttpError(HttpErrorType.Timeout);
		} catch (OperationCanceledException) {
			return new HttpError(HttpErrorType.Canceled);
		} catch (HttpRequestException e) {
			return new HttpError(HttpErrorType.HttpException, e);
		} catch (Exception e) {
			return new HttpError(HttpErrorType.Unknown, e);
		}
	}
}
