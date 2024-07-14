using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScoutHelper.Utils;

public static class AsyncExtensions {
	public static async void Then<T>(this Task<T> task, Action<T> action) =>
		action(await task);

	public static async Task<U> Then<T, U>(this Task<T> task, Func<T, U> transform) {
		var result = await task;
		return transform(result);
	}

	public static async Task<U> Then<T, U>(this Task<T> task, Func<T, Task<U>> transform) {
		var result = await task;
		return await transform(result);
	}

	public static async Task<IEnumerable<U>> Select<T, U>(this Task<IEnumerable<T>> task, Func<T, U> transform) =>
		(await task).Select(transform);

	public static async Task<IEnumerable<T>> ForEach<T>(this Task<IEnumerable<T>> task, Action<T> action) =>
		(await task).Select(
			value => {
				action(value);
				return value;
			}
		);
}
