using CSharpFunctionalExtensions;
using FsCheck;
using ScoutHelper;
using ScoutHelper.Models;
using ScoutHelper.Utils;
using static ScoutHelper.Utils.Utils;

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
		(includeNulls ? WithNulls(gen).Generator : gen.Select(value => (T?)value!))
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
					Gen.Frequency(freqs.Zip(gens).Select(f => Tuple.Create(f.First, f.Second)))
			)
			.ToArbitrary();
}

public static class ArbExtensions {
	public static Arbitrary<IList<T>> ListOf<T>(this Arbitrary<T> arb) => arb.Generator.ListOf().ToArbitrary();

	public static Arbitrary<IList<T>> NonEmptyListOf<T>(this Arbitrary<T> arb) =>
		arb.Generator.NonEmptyListOf().ToArbitrary();

	public static Arbitrary<IDictionary<K, V>> DictWith<K, V>(
		this Arbitrary<K> keyArb,
		Arbitrary<V> valueArb,
		params K[] excluding
	) where K : notnull =>
		Arbs.DictOf(keyArb.Generator, valueArb.Generator)
			.Generator
			.Select(dict => dict.ToMutableDict())
			.Select(
				dict => {
					excluding.ForEach(key => dict.Remove(key));
					return dict.ToDict();
				}
			)
			.ToArbitrary();

	public static Arbitrary<Maybe<T>> ToMaybeArb<T>(this Arbitrary<T> arb, bool includeNulls = false) =>
		Arbs.MaybeArb(arb.Generator, includeNulls);
}
