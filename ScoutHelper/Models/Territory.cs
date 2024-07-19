using System;
using System.Collections.Generic;
using System.Linq;
using ScoutHelper.Config;
using ScoutHelper.Utils.Functional;
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

	// DT
	Urqopacha,
	Kozamauka,
	YakTel,
	Shaaloani,
	HeritageFound,
	LivingMemory,
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

		{ Urqopacha, "urqopacha" },
		{ Kozamauka, "kozama'uka" },
		{ YakTel, "yak t'el" },
		{ Shaaloani, "shaaloani" },
		{ HeritageFound, "heritage found" },
		{ LivingMemory, "living memory" },
	}.VerifyEnumDictionary();

	private static readonly IDictionary<Territory, uint> DefaultTerritoryInstances =
		GetEnumValues<Territory>()
			.Select(territory => (territory, 1U))
			.Concat(Constants.LatestPatchInstances)
			.ToDict();

	private static Configuration _conf = null!;

	private static IDictionary<Territory, uint> _territoryToId = null!;
	private static IDictionary<uint, Territory> _idToTerritory = null!;

	public static void SetTerritoryInstances(
		Configuration conf,
		IEnumerable<(Territory territory, uint id)> territoryIds
	) {
		if (_conf is not null)
			throw new Exception("cannot set territory instance dictionary after plugin initialization.");

		_conf = conf;
		_territoryToId = territoryIds.ToDict();
		_idToTerritory = _territoryToId.Flip();
	}

	public static Territory AsTerritory(this uint territoryId) => _idToTerritory[territoryId];

	public static string Name(this Territory territory) => TerritoryNames[territory];
	
	public static uint DefaultInstances(this Territory territory) => DefaultTerritoryInstances[territory];

	public static uint Instances(this Territory territory) =>
		_conf
			.Instances
			.GetValueOrDefault(
				_territoryToId.MaybeGet(territory).GetValueOrDefault(0U),
				0U
			);
}
