using System.Collections.Generic;
using System.Linq;
using static ScoutHelper.Models.Territory;
using static ScoutHelper.Utils.Utils;

namespace ScoutHelper.Models;

public enum Territory {
	// HW
	CoerthasWesternHighlands,
	TheSeaOfClouds,
	AzysLla,
	TheDravanianForelands,
	TheDravanianHinterlands,
	TheChurningMists,

	// SB
	TheFringes,
	ThePeaks,
	TheLochs,
	TheRubySea,
	Yanxia,
	TheAzimSteppe,

	// SHB
	Lakeland,
	Kholusia,
	AmhAraeng,
	IlMheg,
	TheRaktikaGreatwood,
	TheTempest,

	// EW
	Labyrinthos,
	Thavnair,
	Garlemald,
	MareLamentorum,
	Elpis,
	UltimaThule,
}

public static class TerritoryExtensions {
	private static readonly IDictionary<Territory, string> TerritoryNames = new Dictionary<Territory, string>() {
		{ CoerthasWesternHighlands, "coerthas western highlands" },
		{ TheSeaOfClouds, "the sea of clouds" },
		{ AzysLla, "azys lla" },
		{ TheDravanianForelands, "the dravanian forelands" },
		{ TheDravanianHinterlands, "the dravanian hinterlands" },
		{ TheChurningMists, "the churning mists" },

		{ TheFringes, "the fringes" },
		{ ThePeaks, "the peaks" },
		{ TheLochs, "the lochs" },
		{ TheRubySea, "the ruby sea" },
		{ Yanxia, "yanxia" },
		{ TheAzimSteppe, "the azim steppe" },

		{ Lakeland, "lakeland" },
		{ Kholusia, "kholusia" },
		{ AmhAraeng, "amh araeng" },
		{ IlMheg, "il mheg" },
		{ TheRaktikaGreatwood, "the rak'tika greatwood" },
		{ TheTempest, "the tempest" },

		{ Labyrinthos, "labyrinthos" },
		{ Thavnair, "thavnair" },
		{ Garlemald, "garlemald" },
		{ MareLamentorum, "mare lamentorum" },
		{ Elpis, "elpis" },
		{ UltimaThule, "ultima thule" },
	}.VerifyEnumDictionary();

	private static readonly IDictionary<Territory, uint> TerritoryInstances = GetEnumValues<Territory>()
		.Select(territory => (territory, 1U))
		.Concat(Constants.LatestPatchInstances)
		.ToDict()
		.VerifyEnumDictionary();

	public static string Name(this Territory territory) => TerritoryNames[territory];
	
	public static uint Instances(this Territory territory) => TerritoryInstances[territory];
}
