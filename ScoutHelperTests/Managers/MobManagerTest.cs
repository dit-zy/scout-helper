using Dalamud;
using Dalamud.Plugin.Services;
using FsCheck;
using FsCheck.Xunit;
using JetBrains.Annotations;
using Lumina.Excel.GeneratedSheets2;
using Moq;
using ScoutHelper.Managers;
using ScoutHelperTests.TestUtils.FsCheck;
using ScoutHelperTests.TestUtils.MoqHelpers;

namespace ScoutHelperTests.Managers;

[TestSubject(typeof(MobManager))]
public class MobManagerTest {
	private readonly Mock<IPluginLog> _log = new(MockBehavior.Strict);
	private readonly Mock<IDataManager> _dataManager = new(MockBehavior.Strict);

	[Property]
	public Property IndexContainsAllElements() => FsCheckUtils.ForAll(
		Arb.Default.UInt32().DistinctListOfPairsWith(Arbs.String()),
		(npcNames) => {
			// DATA
			var npcNameSheet = MockExcelSheet.Create<BNpcName>()
				.AddRows(npcNames.Select(name => MockBNpcName.Create(name.Item1, name.Item2)));

			// GIVEN
			_log.Setup(log => log.Debug(It.IsAny<string>()));
			_dataManager.Setup(dm => dm.GetExcelSheet<BNpcName>(It.IsAny<ClientLanguage>()))
				.Returns(npcNameSheet);

			// WHEN
			var actual = new MobManager(_log.Object, _dataManager.Object);

			// THEN
			_dataManager.Verify(manager => manager.GetExcelSheet<BNpcName>(ClientLanguage.English));
			_log.Verify(log => log.Debug("Building mob data from game files..."));
			_log.Verify(log => log.Debug("Mob data built."));
			_dataManager.VerifyNoOtherCalls();
			_log.VerifyNoOtherCalls();
		}
	);
}
