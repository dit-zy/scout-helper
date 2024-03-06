// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using static ScoutHelper.Models.Territory;

namespace ScoutHelper.Models;

public enum Patch {
	ARR,
	HW,
	SB,
	SHB,
	EW,
}

public static class PatchExtensions {
	private static readonly IDictionary<Patch, IList<Territory>> PatchHuntMaps = new (Patch, IList<Territory>)[] {
			(Patch.ARR, Array.Empty<Territory>()), // TODO: add arr maps
			(Patch.HW, new[] {
				CoerthasWesternHighlands, TheSeaOfClouds, AzysLla,
				TheDravanianForelands, TheDravanianHinterlands, TheChurningMists,
			}),
			(Patch.SB, new[] {
				TheFringes, ThePeaks, TheLochs,
				TheRubySea, Yanxia, TheAzimSteppe,
			}),
			(Patch.SHB, new[] {
				Lakeland, Kholusia, AmhAraeng,
				IlMheg, TheRaktikaGreatwood, TheTempest,
			}),
			(Patch.EW, new[] {
				Labyrinthos, Thavnair, Garlemald,
				MareLamentorum, Elpis, UltimaThule,
			}),
		}
		.Select(patch => (patch.Item1, patch.Item2.AsList()))
		.ToDict()
		.VerifyEnumDictionary();

	private static readonly IDictionary<Patch, uint> PatchMaxMarks = PatchHuntMaps
		.Select(
			patchMaps => (
				patchMaps.Key,
				(uint)patchMaps.Value.Sum(territory => 2 * territory.Instances())
			)
		)
		.Append((Patch.ARR, 17U))
		.ToDict()
		.VerifyEnumDictionary();

	private static readonly IDictionary<Patch, string> PatchEmotes = new Dictionary<Patch, string> {
		{ Patch.ARR, ":2x:" },
		{ Patch.HW, ":3x:" },
		{ Patch.SB, ":4x:" },
		{ Patch.SHB, ":5x:" },
		{ Patch.EW, ":6x:" },
	}.VerifyEnumDictionary();

	public static IList<Territory> HuntMaps(this Patch patch) => PatchHuntMaps[patch];

	public static uint MaxMarks(this Patch patch) => PatchMaxMarks[patch];

	public static string Emote(this Patch patch) => PatchEmotes[patch];
}
