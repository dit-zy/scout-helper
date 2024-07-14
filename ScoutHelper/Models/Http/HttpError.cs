using System;
using CSharpFunctionalExtensions;

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
