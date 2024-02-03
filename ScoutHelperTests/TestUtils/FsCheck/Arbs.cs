using FsCheck;
using ScoutHelper;
using ScoutHelper.Models;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelperTests.TestUtils.FsCheck;

public static class Arbs {
	public static Arbitrary<string> NonEmptyString() =>
		Arb.Default.NonEmptyString()
			.Generator
			.Select(nes => nes.ToString())
			.ToArbitrary();

	public static Arbitrary<T> OfEnum<T>() where T : struct, Enum => Gen.Elements(Enum.GetValues<T>()).ToArbitrary();

	public static Arbitrary<List<T>> ListOf<T>(Gen<T> gen) =>
		Gen.ListOf(gen)
			.Select(list => list.ToList())
			.ToArbitrary();

	public static Arbitrary<ICollection<T>> NonEmptyList<T>() =>
		Arb.Default
			.List<T>()
			.Filter(list => 0 < list.Count)
			.Generator
			.Select(list => (ICollection<T>)list)
			.ToArbitrary();

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
		var trainList = Arbs.ListOf(Gen.Constant(new TrainMob()));
		var tracker = Arb.Default.String();
		var worldName = Arb.Default.String();
		var highestPatch = OfEnum<Patch>();
		var link = Arb.Default.String();

		return FsCheckUtils.Zip(trainList, tracker, worldName, highestPatch, link)
			.Generator
			.Select(
				arbs => (arbs, new List<(string?, string?)>() {
					("{#}", arbs.a.Count.ToString()),
					("{#max}", PatchMaxMarks[arbs.d].ToString()),
					("{tracker}", arbs.b),
					("{world}", arbs.c),
					("{patch}", arbs.d.ToString()),
					("{link}", arbs.e),
				})
			)
			.SelectMany(
				acc =>
					Gen.Choose(0, 10)
						.SelectMany(
							f =>
								Gen.Frequency(
									Tuple.Create(f, Gen.Elements<(string?, string?)>(acc.Item2)),
									Tuple.Create(
										10 - f,
										Arb.Generate<UnicodeString>()
											.Select(s => s?.ToString()?.TrimEnd('\\'))
											.Select(s => (s, s))
									)
								)
						)
						.ListOf()
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
		List<TrainMob> TrainList,
		string Tracker,
		string WorldName,
		Patch HighestPatch,
		string Link,
		string Template,
		string Expected
	);
}
