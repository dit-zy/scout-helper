using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpFunctionalExtensions;

namespace ScoutHelper;

public static class CollectionExtensions {
	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action) =>
		source.ForEach((value, _) => action.Invoke(value));

	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T, int> action) {
		var values = source as T[] ?? source.ToArray();
		for (var i = 0; i < values.Length; ++i) {
			action.Invoke(values[i], i);
		}
		return values;
	}

	public static IDictionary<K, V> ToDict<K, V>(this IEnumerable<KeyValuePair<K, V>> source) where K : notnull =>
		source.Select(entry => (entry.Key, entry.Value)).ToDict();

	public static IDictionary<K, V> ToDict<K, V>(this IEnumerable<(K, V)> source) where K : notnull =>
		source
			.DistinctBy(entry => entry.Item1)
			.ToImmutableDictionary(entry => entry.Item1, entry => entry.Item2);

	public static IDictionary<K, V> ToMutableDict<K, V>(this IEnumerable<KeyValuePair<K, V>> source) where K : notnull =>
		source.Select(entry => (entry.Key, entry.Value)).ToMutableDict();

	public static IDictionary<K, V> ToMutableDict<K, V>(this IEnumerable<(K, V)> source) where K : notnull =>
		source
			.DistinctBy(entry => entry.Item1)
			.ToDictionary(entry => entry.Item1, entry => entry.Item2);

	public static IDictionary<K, V> With<K, V>(this IDictionary<K, V> source, params (K, V)[] entries) where K : notnull {
		var dict = source.IsReadOnly ? source.ToMutableDict() : source;
		entries.ForEach(entry => dict[entry.Item1] = entry.Item2);
		return source.IsReadOnly ? dict.ToDict() : source;
	}

	public static IDictionary<K, V> Without<K, V>(this IDictionary<K, V> source, params K[] keys) where K : notnull {
		var dict = source.IsReadOnly ? source.ToMutableDict() : source;
		keys.ForEach(key => dict.Remove(key));
		return source.IsReadOnly ? dict.ToDict() : source;
	}

	public static IEnumerable<U?> SelectWhere<T, U>(this IEnumerable<T> source, Func<T, (bool, U?)> filteredSelector) =>
		source
			.Select(filteredSelector)
			.Where(result => result.Item1)
			.Select(result => result.Item2);

	public static IList<T> ToImmutable<T>(this IEnumerable<T> source) => source.ToImmutableList();

	public static IDictionary<K, V> VerifyEnumDictionary<K, V>(this IDictionary<K, V> enumDict)
		where K : struct, Enum {
		var allEnumsAreInDict = (Enum.GetValuesAsUnderlyingType<K>() as K[])!.All(enumDict.ContainsKey);
		if (!allEnumsAreInDict) {
			throw new Exception($"All values of enum [{typeof(K).Name}] must be in the dictionary.");
		}

		return enumDict.ToImmutableDictionary();
	}

	public static IDictionary<V, K> Flip<K, V>(this IDictionary<K, V> source) where V : notnull =>
		source
			.Select(entry => (entry.Value, entry.Key))
			.ToDict();
}
