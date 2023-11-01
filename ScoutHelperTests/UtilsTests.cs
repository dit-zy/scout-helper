using ScoutHelper;
using System.Collections.Immutable;

namespace ScoutHelperTests;

public class UtilsTests {
	
	[Fact]
	public void VerifyEnumDictionary_ErrorOnIncompleteDictionary() {
		// DATA
		var testDictionary = new Dictionary<TestEnum, int>() {
			{TestEnum.A, 0},
			{TestEnum.B, 1}
		};

		// WHEN
		var actual = Assert.Throws<Exception>(() => {
			testDictionary.VerifyEnumDictionary();
		});
		
		// THEN
		Assert.Equal("All values of enum [TestEnum] must be in the dictionary.", actual.Message);
	}
	
	[Fact]
	public void VerifyEnumDictionary_ReturnsImmutableDictionary() {
		// DATA
		var testDictionary = new Dictionary<TestEnum, int>() {
			{TestEnum.A, 0},
			{TestEnum.B, 1},
			{TestEnum.C, 2}
		};

		// WHEN
		var actual = testDictionary.VerifyEnumDictionary();
		
		// THEN
		Assert.IsAssignableFrom<ImmutableDictionary<TestEnum, int>>(actual);
	}
}

internal enum TestEnum {
	A,
	B,
	C
}
