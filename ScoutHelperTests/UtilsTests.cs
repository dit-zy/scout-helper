using ScoutHelper;
using System.Collections.Immutable;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using ScoutHelper.Models;

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
	public void ForEach_NonEmptyEnumerable(IList<string?> testEnumerable) {
		// DATA
		int F(string? s) => s?.Length ?? 0;
		var expected = testEnumerable.Select((Func<string?, int>)F).ToImmutableArray();

		// WHEN
		var actual = new List<int>();
		testEnumerable.ForEach(s => actual.Add(F(s)));

		// THEN
		actual.Should().Equal(expected);
	}

	[Property]
	public void ForEach_WithIndex_NonEmptyEnumerable(IList<string?> testEnumerable) {
		// DATA
		int F(string? s, int i) => (s?.Length ?? 0) + i;
		var expected = testEnumerable.Select((Func<string?, int, int>)F).ToImmutableArray();

		// WHEN
		var actual = new List<int>();
		testEnumerable.ForEach((s, i) => actual.Add(F(s, i)));

		// THEN
		actual.Should().Equal(expected);
	}

	[Property]
	public void ForEach_ReturnsOriginalEnumerable(IList<string> testEnumerable) {
		// WHEN
		var actualNumItems = 0;
		var actual = testEnumerable.ForEach(_ => actualNumItems++);

		// THEN
		actualNumItems.Should().Be(testEnumerable.Count);
		actual.Should().Equal(testEnumerable);
	}

	[Property]
	public void ForEach_WithIndex_ReturnsOriginalEnumerable(IList<string> testEnumerable) {
		// WHEN
		var actualNumItems = 0;
		var actual = testEnumerable.ForEach((_, _) => actualNumItems++);

		// THEN
		actualNumItems.Should().Be(testEnumerable.Count);
		actual.Should().Equal(testEnumerable);
	}

	[Fact]
	public void VerifyEnumDictionary_ErrorOnIncompleteDictionary() {
		// DATA
		var testDictionary = new Dictionary<TestEnum, int>() {
			{ TestEnum.A, 0 },
			{ TestEnum.B, 1 }
		};

		// WHEN
		var actual = Assert.Throws<Exception>(() => { testDictionary.VerifyEnumDictionary(); });

		// THEN
		actual.Message.Should().BeEquivalentTo("All values of enum [TestEnum] must be in the dictionary.");
	}

	[Fact]
	public void VerifyEnumDictionary_ReturnsImmutableDictionary() {
		// DATA
		var testDictionary = new Dictionary<TestEnum, int>() {
			{ TestEnum.A, 0 },
			{ TestEnum.B, 1 },
			{ TestEnum.C, 2 }
		};

		// WHEN
		var actual = testDictionary.VerifyEnumDictionary();

		// THEN
		actual.Should().BeAssignableTo<ImmutableDictionary<TestEnum, int>>();
	}

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

	[Fact]
	public void FormatTemplate() {
		// DATA
		var testTrainList = new List<TrainMob>() { new(), new(), new() };
		var testTracker = "YATS";
		var testWorldName = "Odin";
		var testPatch = Patch.SHB;
		var patchMaxMarks = 12;
		var testLink = "https://ya.ts/?q=7";

		var testTemplates = new List<(string template, string expected)>() {
			("{patch} {#}/{#max} {world} [{tracker}]({link})", "SHB 3/12 Odin [YATS](https://ya.ts/?q=7)"),
			("#{#} {tracker} {not a var} {patch}", "#3 YATS {not a var} SHB"),
			("\\{link\\}{link} {world\\} \\{#}", "{link}https://ya.ts/?q=7 {world} {#}"),
		};

		var expected = testTemplates.Select(spec => spec.expected).ToImmutableList();

		// WHEN
		var actual = testTemplates.Select(
			spec => Utils.FormatTemplate(
				spec.template,
				testTrainList,
				testTracker,
				testWorldName,
				testPatch,
				testLink
			)
		).ToImmutableList();

		// THEN
		actual.Should().BeEquivalentTo(expected);
	}
}

internal enum TestEnum {
	A,
	B,
	C
}
