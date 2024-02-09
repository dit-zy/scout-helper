using Lumina.Excel;

namespace ScoutHelperTests.TestUtils.MoqHelpers;

public static class MoqUtils {
	#region Mocks

	public static void SetProperty<T>(this T excelRow, string propertyName, object? value)
		where T : ExcelRow, IMockExcelRow {
		excelRow!
			.GetType()!
			.BaseType!
			.GetProperty(propertyName)!
			.GetSetMethod(true)!
			.Invoke(excelRow, new object?[] { value });
	}

	#endregion
}
