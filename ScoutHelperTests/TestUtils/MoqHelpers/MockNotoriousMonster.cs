using Lumina.Excel.GeneratedSheets2;
using static ScoutHelperTests.TestUtils.MoqHelpers.MoqUtils;

namespace ScoutHelperTests.TestUtils.MoqHelpers;

public class MockNotoriousMonster : NotoriousMonster, IMockExcelRow {
	private MockNotoriousMonster(uint rowId, uint bnpcNameId) {
		RowId = rowId;
		this.SetProperty("BNpcName", MockLazyRow<BNpcName>(bnpcNameId));
	}

	public static MockNotoriousMonster Create(uint rowId, uint bnpcNameId) {
		return new MockNotoriousMonster(rowId, bnpcNameId);
	}
}
