using System.Collections.Immutable;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using JetBrains.Annotations;
using ScoutHelper;
using ScoutHelperTests.TestUtils.FsCheck;
using CollectionExtensions = ScoutHelper.CollectionExtensions;

namespace ScoutHelperTests.Utils;

[TestSubject(typeof(CollectionExtensions))]
public class CollectionExtensionsTest {
	[Fact]
	public void ForEach_EmptyEnumerable() {
		// DATA
		IEnumerable<string> testEnumerable = Array.Empty<string>();

		// WHEN
		var actual = new List<string>();
		testEnumerable.ForEach(actual.Add);

		// THEN
		actual.Should().BeEmpty();
	}

	[Fact]
	public void ForEach_WithIndex_EmptyEnumerable() {
		// DATA
		IEnumerable<string> testEnumerable = Array.Empty<string>();

		// WHEN
		var actual = new List<int>();
		testEnumerable.ForEach((s, i) => actual.Add(i + s.Length));

		// THEN
		actual.Should().BeEmpty();
	}

	[Property]
	public Property ForEach_NonEmptyEnumerable() => FsCheckUtils.ForAll(
		Arbs.String().NonEmptyListOf(),
		testEnumerable => {
			// DATA
			int F(string s) => s.Length;
			var expected = testEnumerable.Select(F).ToImmutableArray();

			// WHEN
			var actual = new List<int>();
			testEnumerable.ForEach(s => actual.Add(F(s)));

			// THEN
			actual.Should().Equal(expected);
		}
	);

	[Property]
	public Property ForEach_WithIndex_NonEmptyEnumerable() => FsCheckUtils.ForAll(
		Arbs.String().NonEmptyListOf(),
		testEnumerable => {
			// DATA
			int F(string s, int i) => s.Length + i;
			var expected = testEnumerable.Select(F).ToImmutableArray();

			// WHEN
			var actual = new List<int>();
			testEnumerable.ForEach((s, i) => actual.Add(F(s, i)));

			// THEN
			actual.Should().Equal(expected);
		}
	);

	[Property]
	public Property ForEach_ReturnsOriginalEnumerable() => FsCheckUtils.ForAll(
		Arbs.String().NonEmptyListOf(),
		testEnumerable => {
			// WHEN
			var actualNumItems = 0;
			var actual = testEnumerable.ForEach(_ => actualNumItems++);

			// THEN
			actualNumItems.Should().Be(testEnumerable.Count);
			actual.Should().Equal(testEnumerable);
		}
	);

	[Property]
	public Property ForEach_WithIndex_ReturnsOriginalEnumerable() => FsCheckUtils.ForAll(
		Arbs.String().NonEmptyListOf(),
		testEnumerable => {
			// WHEN
			var actualNumItems = 0;
			var actual = testEnumerable.ForEach((_, _) => actualNumItems++);

			// THEN
			actualNumItems.Should().Be(testEnumerable.Count);
			actual.Should().Equal(testEnumerable);
		}
	);

	[Property]
	public Property VerifyEnumDictionary_ErrorOnIncompleteDictionary() => FsCheckUtils.ForAll(
		Arbs.PartialEnumDict<TestEnum, string>(),
		testDictionary => {
			// WHEN
			var actual = Assert.Throws<Exception>(() => { testDictionary.VerifyEnumDictionary(); });

			// THEN
			actual.Message.Should().BeEquivalentTo("All values of enum [TestEnum] must be in the dictionary.");
		}
	);

	[Property]
	public Property VerifyEnumDictionary_ReturnsImmutableDictionary() => FsCheckUtils.ForAll(
		Arbs.EnumDict<TestEnum, string>(),
		testDictionary => {
			// WHEN
			var actual = new Dictionary<TestEnum, string>(testDictionary).VerifyEnumDictionary();

			// THEN
			actual.Should().BeAssignableTo<ImmutableDictionary<TestEnum, string>>();
		}
	);
}
