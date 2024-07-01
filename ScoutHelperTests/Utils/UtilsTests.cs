using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using ScoutHelper.Models;
using ScoutHelper.Utils;
using ScoutHelperTests.TestUtils.FsCheck;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelperTests.Utils;

public class UtilsTests {
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
	public Property FormatTemplate_FormatsCorrectly() => FsCheckUtils.ForAll(
		CopyTemplate(),
		copyTemplate => {
			// WHEN
			var actual = FormatTemplate(
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

	private static Arbitrary<CopyTemplateArb> CopyTemplate() {
		var trainList = Gen.Constant(new TrainMob()).ListOf().ToArbitrary();
		var tracker = Arb.Default.String();
		var worldName = Arb.Default.String();
		var highestPatch = Arbs.OfEnum<Patch>();
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
					("{patch-emote}", arbs.d.Emote()),
					("{link}", arbs.e),
				})
			)
			.SelectMany(
				acc =>
					Arbs.RandomFreq(
							Gen.Elements<(string?, string?)>(acc.Item2),
							Arbs.String().Generator
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

	private record struct CopyTemplateArb(
		IList<TrainMob> TrainList,
		string Tracker,
		string WorldName,
		Patch HighestPatch,
		string Link,
		string Template,
		string Expected
	);

	private enum TestEnum {
		A,
		B,
		C,
		D,
		E
	}
}
