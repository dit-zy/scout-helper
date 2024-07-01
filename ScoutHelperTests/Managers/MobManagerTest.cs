using CSharpFunctionalExtensions;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using JetBrains.Annotations;
using Lumina.Excel.GeneratedSheets2;
using Moq;
using ScoutHelper;
using ScoutHelper.Managers;
using ScoutHelper.Utils;
using ScoutHelperTests.TestUtils.FsCheck;
using ScoutHelperTests.TestUtils.MoqHelpers;
using static ScoutHelperTests.TestUtils.FsCheck.FsCheckUtils;
using NameList = System.Collections.Generic.IList<(uint mobId, string mobName)>;

namespace ScoutHelperTests.Managers;

[TestSubject(typeof(MobManager))]
public class MobManagerTest {
	private readonly Mock<IPluginLog> _log = new(MockBehavior.Strict);
	private readonly Mock<IDataManager> _dataManager = new(MockBehavior.Strict);

	[Property]
	public Property IndexContainsAllElements() => ForAll(
		Arb.Default.UInt32().DistinctListOfPairsWith(Arbs.String()),
		(npcNames) => {
			// DATA
			var npcNameSheet = MockExcelSheet.Create<BNpcName>()
				.AddRows(npcNames.Select(name => MockBNpcName.Create(name.Item1, name.Item2)));
			var notoriousMonsterSheet = MockExcelSheet.Create<NotoriousMonster>()
				.AddRows(npcNames.Select(name => MockNotoriousMonster.Create(name.Item1, name.Item1)));

			// GIVEN
			_log.Setup(log => log.Debug(It.IsAny<string>()));
			_dataManager.Setup(dm => dm.GetExcelSheet<BNpcName>(It.IsAny<ClientLanguage>()))
				.Returns(npcNameSheet);
			_dataManager.Setup(dm => dm.GetExcelSheet<NotoriousMonster>(It.IsAny<ClientLanguage>()))
				.Returns(notoriousMonsterSheet);

			// WHEN
			var mobManager = new MobManager(_log.Object, _dataManager.Object);

			// THEN
			_dataManager.Verify(manager => manager.GetExcelSheet<BNpcName>(ClientLanguage.English));
			_dataManager.Verify(manager => manager.GetExcelSheet<NotoriousMonster>(ClientLanguage.English));
			_log.Verify(log => log.Debug("Building mob data from game files..."));
			_log.Verify(log => log.Debug("Mob data built."));
			_dataManager.VerifyNoOtherCalls();
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

	[Property]
	public Property IndexHandlesDuplicates() => ForAll(
		DupIndexItemsArb(),
		inputs => {
			// DATA
			var npcNames = inputs.names
				.Concat(inputs.dupNames)
				.AsList();
			var numDupNames = inputs.dupNames.DistinctBy(name => name.mobName).Count();

			var npcNameSheet = MockExcelSheet .Create<BNpcName>()
				.AddRows(npcNames.Select(name => MockBNpcName.Create(name.mobId, name.mobName)));
			var notoriousMonsterSheet = MockExcelSheet.Create<NotoriousMonster>()
				.AddRows(npcNames.Select(name => MockNotoriousMonster.Create(name.mobId, name.mobId)));

			// GIVEN
			_log.Reset();
			_dataManager.Reset();
			
			_log.Setup(log => log.Debug(It.IsAny<string>()));
			_log.Setup(log => log.Debug(It.IsAny<string>(), It.IsAny<object[]>()));
			_dataManager.Setup(dm => dm.GetExcelSheet<BNpcName>(It.IsAny<ClientLanguage>()))
				.Returns(npcNameSheet);
			_dataManager.Setup(dm => dm.GetExcelSheet<NotoriousMonster>(It.IsAny<ClientLanguage>()))
				.Returns(notoriousMonsterSheet);

			// WHEN
			var mobManager = new MobManager(_log.Object, _dataManager.Object);

			// THEN
			_dataManager.Verify(manager => manager.GetExcelSheet<BNpcName>(ClientLanguage.English));
			_dataManager.Verify(manager => manager.GetExcelSheet<NotoriousMonster>(ClientLanguage.English));
			_log.Verify(log => log.Debug("Building mob data from game files..."));
			_log.Verify(log => log.Debug("Mob data built."));
			_log.Verify(
				log => log.Debug(
					It.Is<string>(msg => msg.StartsWith("Duplicate mobs found for name")),
					It.IsAny<object[]>()
				),
				Times.Exactly(numDupNames)
			);
			_dataManager.VerifyNoOtherCalls();
			_log.VerifyNoOtherCalls();

			inputs.names.ForEach(
				name => {
					mobManager.GetMobName(name.mobId).Should().Be(Maybe.From(name.mobName.Lower()));
					mobManager.GetMobId(name.mobName).Should().Be(Maybe.From(name.mobId));
				}
			);

			inputs.dupNames.ForEach(
				name => { mobManager.GetMobName(name.mobId).Should().Be(Maybe<string>.None); }
			);
		}
	);

	private static Arbitrary<(NameList names, NameList dupNames)> DupIndexItemsArb() {
		return Arb.Default
			.UInt32()
			.DistinctListOfPairsWith(Arbs.String())
			.NonEmpty()
			.SelectMany(
				names => Arb.Default.UInt32()
					.Where(mobId => names.All(name => mobId != name.Item1))
					.ZipWith(Gen.Elements<(uint, string name)>(names).KeepSecond())
					.NonEmptyListOf()
					.Select(dupNames => dupNames.WithDistinctFirst())
					.Select(dupNames => (names, dupNames.AsList()))
			)
			.ToArbitrary();
	}
}
