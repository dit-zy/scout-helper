using System.Numerics;
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

public class UtilsTests : IClassFixture<TestFixture> {
	private readonly TestFixture _fixture;

	public UtilsTests(TestFixture fixture) {
		_fixture = fixture;
	}

	[Property]
	public Property V2_Single() => FsCheckUtils.ForAll(
		Arbs.Float(),
		xy =>
			ScoutHelper.Utils.Utils.V2(xy).Should().Be(new Vector2(xy, xy))
	);

	[Property]
	public Property V2() => FsCheckUtils.ForAll(
		Arbs.Vector2(),
		expected =>
			ScoutHelper.Utils.Utils.V2(expected.X, expected.Y).Should().Be(expected)
	);

	[Property]
	public Property V4() => FsCheckUtils.ForAll(
		Arbs.Vector4(),
		expected =>
			ScoutHelper.Utils.Utils.V4(expected.X, expected.Y, expected.Z, expected.W).Should().Be(expected)
	);

	[Property]
	public Property Color() => FsCheckUtils.ForAll(
		Arbs.Vector4(),
		expected =>
			ScoutHelper.Utils.Utils.Color(expected.X, expected.Y, expected.Z, expected.W).Should().Be(expected)
	);

	[Property]
	public Property Color_3() => FsCheckUtils.ForAll(
		Arbs.Vector3(),
		expected =>
			ScoutHelper.Utils.Utils.Color(expected.X, expected.Y, expected.Z)
				.Should().Be(new Vector4(expected.X, expected.Y, expected.Z, 1f))
	);

	[Property]
	public Property Color_Uint() => FsCheckUtils.ForAll(
		Arb.Default.UInt32().Choose4(),
		components =>
			ScoutHelper.Utils.Utils.Color(components.a, components.b, components.c, components.d)
				.Should().Be(new Vector4(components.a / 256f, components.b / 256f, components.c / 256f, components.d / 256f))
	);

	[Property]
	public Property Color_Uint3() => FsCheckUtils.ForAll(
		Arb.Default.UInt32().Choose3(),
		components =>
			ScoutHelper.Utils.Utils.Color(components.a, components.b, components.c)
				.Should().Be(new Vector4(components.a / 256f, components.b / 256f, components.c / 256f, 1f))
	);

	[Fact]
	public void GetEnumValues() {
		// DATA
		var expected = new[] { TestEnum.A, TestEnum.B, TestEnum.C, TestEnum.D, TestEnum.E };

		// WHEN
		var actual = GetEnumValues<TestEnum>();

		// THEN
		actual.Should().Equal(expected);
	}

	[Fact]
	public void WorldName_NoPlayer() {
		// DATA
		var clientState = new Mock<IClientState>();

		// GIVEN
		clientState.Setup(state => state.LocalPlayer).Returns(null as IPlayerCharacter);

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
