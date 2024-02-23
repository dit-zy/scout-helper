using System.Collections;
using Lumina.Data;
using Lumina.Data.Files.Excel;
using Lumina.Excel;

namespace ScoutHelperTests.TestUtils.MoqHelpers;

public class MockExcelSheet<T> : ExcelSheet<T>, IEnumerable<T> where T : ExcelRow {
	private readonly List<T> _rows = new();

	internal MockExcelSheet() : base(new ExcelHeaderFile(), "test sheet", Language.English, null!) { }

	public new IEnumerator<T> GetEnumerator() => _rows.GetEnumerator();
	
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public MockExcelSheet<T> AddRow(T row) {
		_rows.Add(row);
		return this;
	}

	public MockExcelSheet<T> AddRows(IEnumerable<T> rows) {
		_rows.AddRange(rows);
		return this;
	}
}

public static class MockExcelSheet {
	public static MockExcelSheet<T> Create<T>() where T : ExcelRow => new();
}
