using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using CSharpFunctionalExtensions;
using ScoutHelper.Models;

namespace ScoutHelper.Utils.Functional;

public static class FunctionalExtensions {
	#region maybe

	public static IEnumerable<T> SelectMaybe<T>(this IEnumerable<Maybe<T>> source) =>
		source.SelectMaybe(value => value);

	public static IEnumerable<U> SelectMaybe<T, U>(this IEnumerable<T> source, Func<T, Maybe<U>> selector) =>
		source.SelectWhere(
				value => {
					var result = selector.Invoke(value);
					return (result.HasValue, result);
				}
			)
			.Select(result => result.Value);

	public static IEnumerable<Maybe<U>> MaybeSelect<T, U>(this IEnumerable<Maybe<T>> source, Func<T, U> transform) =>
		source.Select(maybeValue => maybeValue.Select(transform));

	public static IEnumerable<Maybe<U>> MaybeSelectMany<T, U>(this IEnumerable<Maybe<T>> source, Func<T, Maybe<U>> transform) =>
		source.Select(maybeValue => maybeValue.SelectMany(transform));

	public static Maybe<V> MaybeGet<K, V>(this IDictionary<K, V> source, K key) {
		if (source.TryGetValue(key, out var value)) {
			return value;
		}
		return Maybe.None;
	}

	#endregion

	#region results

	public static AccResults<IEnumerable<T>, string> SelectResults<T>(this IEnumerable<Result<T>> source) =>
		source.SelectResults(result => result);

	public static AccResults<IEnumerable<U>, string> SelectResults<T, U>(
		this IEnumerable<T> source,
		Func<T, Result<U>> selector
	) =>
		source.SelectResults(
			value => selector.Invoke(value).Match(
				Result.Success<U, string>,
				Result.Failure<U, string>
			)
		);

	public static AccResults<IEnumerable<T>, E> SelectResults<T, E>(this IEnumerable<Result<T, E>> source) =>
		source.SelectResults(result => result);

	public static AccResults<IEnumerable<U>, E> SelectResults<T, U, E>(
		this IEnumerable<T> source,
		Func<T, Result<U, E>> selector
	) =>
		source.BindResults(
			result => selector
				.Invoke(result)
				.Map(AccResults.From<U, E>)
		);

	public static AccResults<IEnumerable<U>, E> BindResults<T, U, E>(
		this IEnumerable<T> source,
		Func<T, Result<AccResults<U, E>, E>> transform
	) =>
		source.Reduce(
				(acc, value) => {
					transform.Invoke(value).Match(
						success => {
							acc.results.Add(success.Value);
							acc.errors.AddRange(success.Errors);
						},
						error => acc.errors.Add(error)
					);
					return acc;
				},
				(results: new List<U>(), errors: new List<E>())
			)
			.ToAccResult(results => (IEnumerable<U>)results);

	public static AccResults<IEnumerable<U>, E> SelectValues<T, U, E>(
		this AccResults<IEnumerable<T>, E> source,
		Func<T, U> selector
	) {
		var newValue = source.Value.Select(selector);
		return new AccResults<IEnumerable<U>, E>(newValue, source.Errors);
	}

	public static AccResults<IEnumerable<U>, E> SelectResults<T, U, E>(
		this AccResults<IEnumerable<T>, E> source,
		Func<T, Result<U, E>> selector
	) {
		var newAccResults = source.Value.SelectResults(selector);
		return new AccResults<IEnumerable<U>, E>(newAccResults.Value, source.Errors.Concat(newAccResults.Errors));
	}

	public static AccResults<V, E> Join<T, U, V, E>(
		this AccResults<T, E> source,
		AccResults<U, E> secondSource,
		Func<T, U, V> combiner
	) =>
		new(
			combiner.Invoke(source.Value, secondSource.Value),
			source.Errors.Concat(secondSource.Errors)
		);

	public static AccResults<U, E> WithValue<T, U, E>(this AccResults<T, E> source, Func<T, U> valueModifier) =>
		new(valueModifier.Invoke(source.Value), source.Errors);

	public static AccResults<T, E> ForEachError<T, E>(this AccResults<T, E> source, Action<E> action) {
		source.Errors.ForEach(action);
		return source;
	}

	public static AccResults<TWrap, E> ForEachValue<T, TWrap, E>(this AccResults<TWrap, E> source, Action<T> action)
		where TWrap : IEnumerable<T> {
		source.Value.ForEach(action);
		return source;
	}

	public static (T, IEnumerable<E>) AsPair<T, E>(this AccResults<T, E> source) {
		return (source.Value, source.Errors);
	}

	public static AccResults<T, E> ToAccResult<T, E>(this (T, IEnumerable<E>) pair) => new(pair.Item1, pair.Item2);

	public static AccResults<U, E> ToAccResult<T, U, E>(this (T, IEnumerable<E>) pair, Func<T, U> transformer) =>
		new(transformer.Invoke(pair.Item1), pair.Item2);

	public static AccResults<T, E> ToAccResult<T, E>(this T value) => AccResults.From<T, E>(value);

	public static AccResults<IEnumerable<U>, E> SelectMany<T, U, E>(
		this IEnumerable<T> source,
		Func<T, AccResults<U, E>> selector
	) {
		return source.Select(selector)
			.Reduce(
				(acc, results) => {
					acc.values.Add(results.Value);
					acc.errors.AddRange(results.Errors);
					return acc;
				},
				(values: new List<U>(), errors: new List<E>())
			)
			.ToAccResult(values => (IEnumerable<U>)values);
	}

	#endregion

	#region pure functions

	public static ACC Reduce<T, ACC>(this IEnumerable<T> source, Func<ACC, T, ACC> reducer, ACC initial) {
		var acc = initial;
		source.ForEach(value => { acc = reducer.Invoke(acc, value); });
		return acc;
	}

	public static IEnumerable<int> Sequence(this Range range) {
		for (int i = range.Start.Value; i < range.End.Value; i++) {
			yield return i;
		}
	}

	#endregion
}
