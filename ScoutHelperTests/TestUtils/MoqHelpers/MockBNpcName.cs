using Lumina.Excel.GeneratedSheets2;
using ScoutHelper.Utils;

namespace ScoutHelperTests.TestUtils.MoqHelpers;

public class MockBNpcName : BNpcName, IMockExcelRow {
	private MockBNpcName(uint rowId, string singular) {
		RowId = rowId;
		this.SetProperty("Singular", singular.ToSeString());
	}

	public static MockBNpcName Create(uint rowId, string singular) {
		return new MockBNpcName(rowId, singular);
	}
}
