using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DitzyExtensions.Functional;

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

	public static IList<T> AsList<T>(this IEnumerable<T> source) => source.ToImmutableList();

	public static IList<T> AsMutableList<T>(this IEnumerable<T> source) => source.ToList();

	public static IDictionary<K, V> ToDict<K, V>(this IEnumerable<KeyValuePair<K, V>> source) where K : notnull =>
		source.Select(entry => (entry.Key, entry.Value)).ToDict();

	public static IDictionary<K, V> ToDict<K, V>(this IEnumerable<(K key, V value)> source) where K : notnull =>
		source
			.GroupBy(entry => entry.key)
			.Select(grouping => grouping.Last())
			.ToImmutableDictionary(entry => entry.key, entry => entry.value);

	public static IDictionary<K, V> ToMutableDict<K, V>(this IEnumerable<KeyValuePair<K, V>> source) where K : notnull =>
		source.Select(entry => (entry.Key, entry.Value)).ToMutableDict();

	public static IDictionary<K, V> ToMutableDict<K, V>(this IEnumerable<(K, V)> source) where K : notnull =>
		source
			.ToDict()
			.ToDictionary(entry => entry.Key, entry => entry.Value);

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

	#region dicts

	public static IDictionary<K, V> VerifyEnumDictionary<K, V>(this IDictionary<K, V> enumDict)
		where K : struct, Enum {
		var allEnumsAreInDict = (Enum.GetValuesAsUnderlyingType<K>() as K[])!.All(enumDict.ContainsKey);
		if (!allEnumsAreInDict) {
			throw new Exception($"All values of enum [{typeof(K).Name}] must be in the dictionary.");
		}

		return enumDict.ToDict();
	}

	public static IDictionary<V, K> Flip<K, V>(this IDictionary<K, V> source) where V : notnull =>
		source
			.Select(entry => (entry.Value, entry.Key))
			.ToDict();

	public static IDictionary<K, V> Update<K, V>(
		this IDictionary<K, V> source,
		IEnumerable<(K key, V value)> updateEntries
	) where K : notnull {
		if (source.IsReadOnly)
			return source
				.AsPairs()
				.Concat(updateEntries)
				.ToDict();

		updateEntries.ForEach(entry => source[entry.key] = entry.value);
		return source;
	}

	public static IDictionary<K, V> UseToUpdate<K, V>(
		this IEnumerable<(K, V)> updateEntries,
		IDictionary<K, V> target
	) where K : notnull =>
		target.Update(updateEntries);

	public static IDictionary<T, IList<U>> AsMultiDict<T, U>(this IEnumerable<(T t, U u)> source) where T : notnull =>
		source.Reduce(
				(acc, next) => {
					if (!acc.TryGetValue(next.t, out var values)) {
						values = new List<U>();
						acc.Add(next.t, values);
					}
					values.Add(next.u);
					return acc;
				},
				new Dictionary<T, IList<U>>()
			)
			.AsPairs()
			.Select(entry => (entry.key, entry.val.AsList()))
			.ToDict();

	#endregion

	#region lists

	public static bool IsEmpty<T>(this ICollection<T> source) => source.Count == 0;

	public static bool IsNotEmpty<T>(this ICollection<T> source) => 0 < source.Count;

	public static IList<T> AsSingletonList<T>(this T value) => new List<T>() { value }.AsList();

	public static (IEnumerable<T> ts, IEnumerable<U> us, IEnumerable<V> vs) Unzip<T, U, V>(
		this IEnumerable<(T t, U u, V v)> source
	) =>
		source.Unzip((ts, us, vs) => (ts, us, vs));

	public static R Unzip<T, U, V, R>(
		this IEnumerable<(T t, U u, V v)> source,
		Func<IEnumerable<T>, IEnumerable<U>, IEnumerable<V>, R> transform
	) {
		var (ts, us, vs) = source
			.Reduce(
				(acc, element) => {
					acc.ts.Add(element.t);
					acc.us.Add(element.u);
					acc.vs.Add(element.v);
					return acc;
				},
				(ts: new List<T>(), us: new List<U>(), vs: new List<V>())
			);
		return transform.Invoke(ts, us, vs);
	}

	public static (IEnumerable<T> ts, IEnumerable<U> us) Unzip<T, U>(this IEnumerable<(T t, U u)> source) =>
		source.Unzip((ts, us) => (ts, us));

	public static R Unzip<T, U, R>(
		this IEnumerable<(T t, U u)> source,
		Func<IEnumerable<T>, IEnumerable<U>, R> transform
	) {
		var (ts, us) = source
			.Reduce(
				(acc, pair) => {
					acc.ts.Add(pair.t);
					acc.us.Add(pair.u);
					return acc;
				},
				(ts: new List<T>(), us: new List<U>())
			);
		return transform.Invoke(ts, us);
	}

	#endregion

	#region pairs

	public static IEnumerable<(K key, V val)> AsPairs<K, V>(this IDictionary<K, V> source) =>
		source.Select(entry => (entry.Key, entry.Value)).AsList();

	public static IEnumerable<(B first, A second)> Flip<A, B>(this IEnumerable<(A first, B second)> source) =>
		source.Select(entry => (entry.second, entry.first)).AsList();

	public static IEnumerable<(A, B)> WithDistinctFirst<A, B>(this IEnumerable<(A, B)> source) where A : notnull =>
		source
			.ToDict()
			.AsPairs()
			.AsList();

	#endregion

	#region tuples

	public static (T1, T2, U) Flatten<T1, T2, U>(this ((T1 t1, T2 t2) t, U u) source) =>
		(source.t.t1, source.t.t2, source.u);

	public static (T1, T2, T3, U) Flatten<T1, T2, T3, U>(this ((T1 t1, T2 t2, T3 t3) t, U u) source) =>
		(source.t.t1, source.t.t2, source.t.t3, source.u);

	public static (T1, T2, T3, T4, U) Flatten<T1, T2, T3, T4, U>(this ((T1 t1, T2 t2, T3 t3, T4 t4) t, U u) source) =>
		(source.t.t1, source.t.t2, source.t.t3, source.t.t4, source.u);

	#endregion
}
