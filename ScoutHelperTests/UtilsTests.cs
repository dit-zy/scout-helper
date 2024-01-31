using System.Collections.Immutable;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using ScoutHelper;
using ScoutHelperTests.Util.FsCheck;

namespace ScoutHelperTests;

public class UtilsTests {

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
		Arbs.NonEmptyList<string?>(),
		testEnumerable => {
			// DATA
			int F(string? s) => s?.Length ?? 0;
			var expected = testEnumerable.Select((Func<string?, int>)F).ToImmutableArray();

			// WHEN
			var actual = new List<int>();
			testEnumerable.ForEach(s => actual.Add(F(s)));

			// THEN
			actual.Should().Equal(expected);
		}
	);

	[Property]
	public Property ForEach_WithIndex_NonEmptyEnumerable() => FsCheckUtils.ForAll(
		Arbs.NonEmptyList<string?>(),
		testEnumerable => {
			// DATA
			int F(string? s, int i) => (s?.Length ?? 0) + i;
			var expected = testEnumerable.Select((Func<string?, int, int>)F).ToImmutableArray();

			// WHEN
			var actual = new List<int>();
			testEnumerable.ForEach((s, i) => actual.Add(F(s, i)));

			// THEN
			actual.Should().Equal(expected);
		}
	);

	[Property]
	public Property ForEach_ReturnsOriginalEnumerable() => FsCheckUtils.ForAll(
		Arbs.NonEmptyList<string?>(),
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
		Arbs.NonEmptyList<string?>(),
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

	[Fact]
	public void WorldName_NoPlayer() {
		// DATA
		var clientState = new Mock<IClientState>();

		// GIVEN
		clientState.Setup(state => state.LocalPlayer).Returns(null as PlayerCharacter);

		// WHEN
		var worldName = clientState.Object.WorldName();

		// THEN
		worldName.Should().Be("Not Found");
	}

	[Property]
	public Property FormatTemplate() => FsCheckUtils.ForAll(
		Arbs.CopyTemplate(),
		copyTemplate => {
			// WHEN
			var actual = Utils.FormatTemplate(
				copyTemplate.Template,
				copyTemplate.TrainList,
				copyTemplate.Tracker,
				copyTemplate.WorldName,
				copyTemplate.HighestPatch,
				copyTemplate.Link
			);

			// THEN
			actual.Should().Be(copyTemplate.Expected);
		}
	);
}

internal enum TestEnum {
	A,
	B,
	C,
	D,
	E
}
