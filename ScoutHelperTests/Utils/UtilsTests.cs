using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Moq;
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
		Arbs.CopyTemplate(),
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
}

internal enum TestEnum {
	A,
	B,
	C,
	D,
	E
}
