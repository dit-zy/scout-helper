using ScoutHelper;
using ScoutHelper.Config;
using ScoutHelper.Models;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelperTests;

public class TestFixture : IDisposable {
	public Configuration Conf { get; }

	public TestFixture() {
		Conf = new Configuration();
		var territoryIds = GetEnumValues<Territory>()
			.PairWith(territory => (uint)territory)
			.AsList();
		territoryIds
			.Select(territoryId => (territoryId.second, territoryId.first.DefaultInstances()))
			.UseToUpdate(Conf.Instances);
		TerritoryExtensions.SetTerritoryInstances(Conf, territoryIds);
	}

	public void Dispose() {
		// nothing to dispose yet
	}
}
