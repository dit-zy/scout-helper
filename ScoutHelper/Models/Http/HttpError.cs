using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Dalamud.Plugin.Services;

namespace ScoutHelper.Models.Http;

public record HttpError(
	HttpErrorType ErrorType,
	Exception? Exception = null
) {
	public static implicit operator HttpError(HttpErrorType errorType) => new(errorType);
}

public enum HttpErrorType {
	Unknown,
	Timeout,
	Canceled,
	HttpException,
}

public static class HttpErrorExtensions {
	public static Task<Result<T, string>> HandleHttpError<T>(
		this Task<Result<T, HttpError>> apiOperation,
		IPluginLog log,
		string timeoutMsg,
		string cancelMsg,
		string httpExceptionMsg,
		string unknownErrorMsg
	) =>
		apiOperation.MapError<T, HttpError, string>(
			error => {
				switch (error.ErrorType) {
					case HttpErrorType.Timeout: {
						log.Error(timeoutMsg);
						return timeoutMsg;
					}
					case HttpErrorType.Canceled: {
						log.Warning(cancelMsg);
						return cancelMsg;
					}
					case HttpErrorType.HttpException:
						log.Error(error.Exception, httpExceptionMsg);
						return httpExceptionMsg;
					default:
						log.Error(error.Exception, unknownErrorMsg);
						return unknownErrorMsg;
				}
			}
		);
}
