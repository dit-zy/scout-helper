using CSharpFunctionalExtensions;
using Dalamud;
using Dalamud.Plugin.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using JetBrains.Annotations;
using Lumina.Excel.GeneratedSheets2;
using Moq;
using ScoutHelper;
using ScoutHelper.Config;
using ScoutHelper.Managers;
using ScoutHelper.Utils;
using ScoutHelperTests.TestUtils.FsCheck;
using ScoutHelperTests.TestUtils.MoqHelpers;
using static ScoutHelperTests.TestUtils.FsCheck.FsCheckUtils;
using Configuration = ScoutHelper.Config.Configuration;

namespace ScoutHelperTests.Managers;

[TestSubject(typeof(SirenManager))]
public class SirenManagerTest {
	private readonly Mock<IPluginLog> _log = new(MockBehavior.Strict);
	private readonly Mock<ITerritoryManager> _territoryManager = new(MockBehavior.Strict);
	private readonly Mock<IMobManager> _mobManager = new(MockBehavior.Strict);

	[Property]
	public Property IndexContainsAllElements() => ForAll(
		Arb.Default.UInt32().DistinctListOfPairsWith(Arbs.String()),
		(npcNames) => {
			// DATA

			// GIVEN
			_log.Setup(log => log.Debug(It.IsAny<string>()));

			var mobManager = _mobManager.Object;

			// WHEN
			var conf = new Configuration();
			var opts = new ScoutHelperOptions("", "");
			var sirenManager = CreateSirenManager(conf, opts);

			// THEN
			_log.Verify(log => log.Debug("Building mob data from game files..."));
			_log.Verify(log => log.Debug("Mob data built."));
			_log.VerifyNoOtherCalls();

			npcNames.ForEach(
				entry => {
					var mobId = entry.Item1;
					var mobName = entry.Item2;
					mobManager.GetMobName(mobId).Should().Be(Maybe.From(mobName.Lower()));
					mobManager.GetMobId(mobName).Should().Be(Maybe.From(mobId));
				}
			);
		}
	);

	private SirenManager CreateSirenManager(Configuration conf, ScoutHelperOptions opts) =>
		new(
			_log.Object,
			conf,
			opts,
			_territoryManager.Object,
			_mobManager.Object
		);
}
