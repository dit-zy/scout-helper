using System.Collections.Immutable;
using System.Numerics;
using CSharpFunctionalExtensions;
using FsCheck;
using ScoutHelper;
using ScoutHelper.Models;
using ScoutHelper.Utils;

namespace ScoutHelperTests.TestUtils.FsCheck;

public static class Arbs {
	public static Arbitrary<string> String() =>
		Arb.Default.UnicodeString()
			.Generator
			.Select(s => s.ToString())
			.ToArbitrary();

	public static Arbitrary<string> NonEmptyString() =>
		String()
			.Generator
			.Where(s => !string.IsNullOrEmpty(s))
			.ToArbitrary();

	public static Arbitrary<float> Float() => Arb.Default.Float32();

	public static Arbitrary<Vector2> Vector2() =>
		Float()
			.Choose2()
			.Select(vals => new Vector2(vals.Item1, vals.Item2))
			.ToArbitrary();

	public static Arbitrary<Vector3> Vector3() =>
		Float()
			.Choose3()
			.Select(vals => new Vector3(vals.Item1, vals.Item2, vals.Item3))
			.ToArbitrary();

	public static Arbitrary<Vector4> Vector4() =>
		Float()
			.Choose4()
			.Select(vals => new Vector4(vals.Item1, vals.Item2, vals.Item3, vals.Item4))
			.ToArbitrary();

	public static Arbitrary<IDictionary<K, V>> DictOf<K, V>(Gen<K> keyGen, Gen<V> valueGen) where K : notnull =>
		FsCheckUtils.Zip(keyGen, valueGen)
			.ListOf()
			.Select(entryList => entryList.ToDict())
			.ToArbitrary();

	public static Arbitrary<T?> WithNulls<T>(Gen<T> gen) =>
		RandomFreq(
			gen.Select(value => (T?)value),
			Gen.Constant((T?)default)
		);

	public static Arbitrary<T> OfEnum<T>() where T : struct, Enum => Gen.Elements(Enum.GetValues<T>()).ToArbitrary();

	public static Arbitrary<IDictionary<K, V>> EnumDict<K, V>() where K : struct, Enum where V : notnull {
		var enumValues = Enum.GetValues<K>();
		return Gen.ListOf(enumValues.Length, Arb.Generate<V>())
			.Select(valueList => enumValues.Zip(valueList))
			.Select(entries => entries.ToDict())
			.ToArbitrary();
	}

	public static Arbitrary<IDictionary<K, V>> PartialEnumDict<K, V>() where K : struct, Enum where V : notnull =>
		EnumDict<K, V>().Generator
			.Select(dict => dict.ToList())
			.SelectMany(Gen.Shuffle)
			.SelectMany(
				entries => Gen
					.Choose(1, entries.Length - 1)
					.Select(entries.Skip)
			)
			.Select(entries => entries.ToDict())
			.ToArbitrary();

	public static Arbitrary<CopyTemplateArb> CopyTemplate() {
		var trainList = Gen.Constant(new TrainMob()).ListOf().ToArbitrary();
		var tracker = Arb.Default.String();
		var worldName = Arb.Default.String();
		var highestPatch = OfEnum<Patch>();
		var link = Arb.Default.String();

		return FsCheckUtils.Zip(trainList, tracker, worldName, highestPatch, link)
			.Generator
			.Select(
				arbs => (arbs, new List<(string?, string?)>() {
					("{#}", arbs.a.Count.ToString()),
					("{#max}", arbs.d.MaxMarks().ToString()),
					("{tracker}", arbs.b),
					("{world}", arbs.c),
					("{patch}", arbs.d.ToString()),
					("{link}", arbs.e),
				})
			)
			.SelectMany(
				acc =>
					RandomFreq(
							Gen.Elements<(string?, string?)>(acc.Item2),
							String().Generator
								.Select(s => ((string?)s)?.TrimEnd('\\'))
								.Select(s => (s, s))
						)
						.ListOf()
						.Generator
						.Select(
							chunks => (
								string.Join(null, chunks.Select(chunk => chunk.Item1)),
								string.Join(null, chunks.Select(chunk => chunk.Item2))
							)
						)
						.Select(
							x => new CopyTemplateArb(
								acc.arbs.a,
								acc.arbs.b,
								acc.arbs.c,
								acc.arbs.d,
								acc.arbs.e,
								x.Item1,
								x.Item2
							)
						)
			)
			.ToArbitrary();
	}

	public record struct CopyTemplateArb(
		IList<TrainMob> TrainList,
		string Tracker,
		string WorldName,
		Patch HighestPatch,
		string Link,
		string Template,
		string Expected
	);

	public static Arbitrary<Maybe<T>> MaybeArb<T>(Gen<T> gen, bool includeNulls = false) =>
		(includeNulls ? WithNulls(gen).Generator : gen.Select(value => (T?)value))
		.Select(
			value =>
				Maybe
					.From(value)
					.Select(maybeValue => (T)maybeValue!)
		)
		.ToArbitrary();

	public static Arbitrary<T> RandomFreq<T>(params Gen<T>[] gens) =>
		Gen.Choose(0, 100)
			.ListOf(gens.Length)
			.SelectMany(
				freqs =>
					Gen.Choose(0, freqs.Count - 1)
						.Select(index => freqs.With((index, freqs[index] + 1)))
			)
			.SelectMany(
				freqs =>
					Gen.Frequency(freqs.Zip(gens).Select(f => Tuple.Create(f.First, f.Second)))
			)
			.ToArbitrary();
}

public static class ArbExtensions {
	public static Arbitrary<IList<T>> ListOf<T>(this Arbitrary<T> arb) => arb.Generator.ListOf().ToArbitrary();

	public static Arbitrary<IList<T>> NonEmptyListOf<T>(this Arbitrary<T> arb) =>
		arb.Generator.NonEmptyListOf().ToArbitrary();

	public static Arbitrary<IList<T>> NonEmpty<T>(this Gen<IList<T>> gen) =>
		gen.Where(list => list.IsNotEmpty()).ToArbitrary();

	public static Arbitrary<IList<T>> NonEmpty<T>(this Arbitrary<IList<T>> arb) =>
		arb.Generator.Where(list => list.IsNotEmpty()).ToArbitrary();

	public static Arbitrary<IDictionary<K, V>> DictWith<K, V>(
		this Arbitrary<K> keyArb,
		Arbitrary<V> valueArb,
		params K[] keysToExclude
	) where K : notnull =>
		Arbs.DictOf(keyArb.Generator, valueArb.Generator)
			.Excluding(keysToExclude);

	public static Arbitrary<IDictionary<K, V>> DistinctDictWith<K, V>(
		this Arbitrary<K> keyArb,
		Arbitrary<V> valueArb,
		params K[] keysToExclude
	) where K : notnull where V : notnull =>
		Arbs.DictOf(keyArb.Generator, valueArb.Generator)
			// flipping forces the values (now keys) to be distinct. double flip, to force keys and values to be distinct.
			.Select(dict => dict.Flip().Flip())
			.Excluding(keysToExclude);

	public static Arbitrary<IDictionary<K, V>> Excluding<K, V>(
		this Arbitrary<IDictionary<K, V>> source,
		params K[] keysToExclude
	) where K : notnull =>
		source
			.Generator
			.Excluding(keysToExclude);

	public static Arbitrary<IDictionary<K, V>> Excluding<K, V>(
		this Gen<IDictionary<K, V>> source,
		params K[] keysToExclude
	) where K : notnull =>
		source
			.Select(dict => dict.ToMutableDict())
			.Select(
				dict => {
					keysToExclude.ForEach(key => dict.Remove(key));
					return dict.ToDict();
				}
			)
			.ToArbitrary();

	public static Arbitrary<(T, U)> ZipWith<T, U>(this Arbitrary<T> arb, Arbitrary<U> secondArb) =>
		FsCheckUtils.Zip(arb, secondArb);

	public static Arbitrary<(T, U)> ZipWith<T, U>(this Arbitrary<T> arb, Gen<U> gen) =>
		FsCheckUtils.Zip(arb.Generator, gen).ToArbitrary();

	public static Arbitrary<(T, U)> ZipWith<T, U>(this Gen<T> gen, Arbitrary<U> arb) =>
		FsCheckUtils.Zip(gen, arb.Generator).ToArbitrary();

	public static Arbitrary<(T, U)> ZipWith<T, U>(this Gen<T> gen, Gen<U> secondGen) =>
		FsCheckUtils.Zip(gen, secondGen).ToArbitrary();

	public static Arbitrary<IList<(T, U)>> DistinctListOfPairsWith<T, U>(this Arbitrary<T> arb, Arbitrary<U> secondArb)
		where T : notnull where U : notnull =>
		arb
			.DistinctDictWith(secondArb)
			.Select(dict => dict.Select(entry => (entry.Key, entry.Value)))
			.Select(entries => (IList<(T, U)>)entries.ToImmutableList())
			.ToArbitrary();

	public static Arbitrary<Maybe<T>> ToMaybeArb<T>(this Arbitrary<T> arb, bool includeNulls = false) =>
		Arbs.MaybeArb(arb.Generator, includeNulls);

	public static Gen<U> Select<T, U>(this Arbitrary<T> arb, Func<T, U> selector) =>
		arb
			.Generator
			.Select(selector);

	public static Gen<U> SelectMany<T, U>(this Arbitrary<T> arb, Func<T, Gen<U>> selector) =>
		arb
			.Generator
			.SelectMany(selector);

	public static Gen<T> Where<T>(this Arbitrary<T> arb, Func<T, bool> predicate) => arb.Generator.Where(predicate);

	public static Gen<A> KeepFirst<A, B>(this Gen<(A, B)> source) => source.Select(pair => pair.Item1);

	public static Gen<B> KeepSecond<A, B>(this Gen<(A, B)> source) => source.Select(pair => pair.Item2);

	public static Arbitrary<(A a, A b)> Choose2<A>(this Arbitrary<A> source) =>
		source
			.Generator
			.Two()
			.Select(vals => (vals.Item1, vals.Item2))
			.ToArbitrary();

	public static Arbitrary<(A a, A b, A c)> Choose3<A>(this Arbitrary<A> source) =>
		source
			.Generator
			.Three()
			.Select(vals => (vals.Item1, vals.Item2, vals.Item3))
			.ToArbitrary();

	public static Arbitrary<(A a, A b, A c, A d)> Choose4<A>(this Arbitrary<A> source) =>
		source
			.Generator
			.Four()
			.Select(vals => (vals.Item1, vals.Item2, vals.Item3, vals.Item4))
			.ToArbitrary();
}
