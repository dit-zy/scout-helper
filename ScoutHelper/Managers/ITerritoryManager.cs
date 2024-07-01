using CSharpFunctionalExtensions;
using ScoutHelper.Utils;

namespace ScoutHelper.Managers;

public interface ITerritoryManager {
	
	public Maybe<uint> FindTerritoryId(string territoryName);

	public Result<uint, string> GetTerritoryId(string territoryName);

	public Maybe<string> GetTerritoryName(uint territoryId);
}
