using CSharpFunctionalExtensions;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using JetBrains.Annotations;
using ScoutHelper;
using ScoutHelper.Utils.Functional;
using ScoutHelperTests.TestUtils.FsCheck;
using static ScoutHelperTests.TestUtils.FsCheck.FsCheckUtils;

namespace ScoutHelperTests.Utils.Functional;

[TestSubject(typeof(FunctionalExtensions))]
public class FunctionalExtensionsTest {
	[Property]
	public Property MaybeGet_MissingKey() => ForAll(
		Arbs.String().DictWith(Arbs.String()),
		Arbs.String(),
		(arbitraryDict, lookupKey) => {
			// DATA
			var dict = arbitraryDict.Without(lookupKey);

			// WHEN
			var actual = dict.MaybeGet(lookupKey);

			// THEN
			actual.Should().Be(Maybe<string>.None);
		}
	);

	[Property]
	public Property MaybeGet_ContainsKey() => ForAll(
		Arbs.String().DictWith(Arbs.String()),
		Arbs.String(),
		Arbs.String(),
		(arbitraryDict, lookupKey, lookupValue) => {
			// DATA
			var dict = arbitraryDict.With((lookupKey, lookupValue));

			// WHEN
			var actual = dict.MaybeGet(lookupKey);

			// THEN
			actual.Should().Be(Maybe.From(lookupValue));
		}
	);

	[Property]
	public Property MaybeGet_EmptyDict() => ForAll(
		Arbs.String(),
		lookupKey => {
			// DATA
			var dict = new Dictionary<string, string>();

			// WHEN
			var actual = dict.MaybeGet(lookupKey);

			// THEN
			actual.Should().Be(Maybe<string>.None);
		}
	);

	[Property]
	public Property SelectMaybe_ExistingList() => ForAll(
		Arbs.String().ToMaybeArb().NonEmptyListOf(),
		(list) => {
			// DATA
			var expectedValues = list
				.Where(maybe => !Maybe<string>.None.Equals(maybe))
				.Select(value => value.Value);
			
			// WHEN
			var actual = list.SelectMaybe();
			
			// THEN
			actual.Should().BeEquivalentTo(expectedValues);
		}
	);

	// select maybe
	// reduce

	// == result ==
	// as pair
	// for each error
	// join
	// select results
	// select values
	// to accresult
	// with value
}
