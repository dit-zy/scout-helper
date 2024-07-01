using CSharpFunctionalExtensions;
using ScoutHelper.Utils;

namespace ScoutHelper.Managers;

public interface IMobManager {
	
	public Maybe<uint> GetMobId(string mobName);

	public Maybe<string> GetMobName(uint mobId);
}
