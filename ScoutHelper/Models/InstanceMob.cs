using Dalamud.Game.ClientState.Objects.Types;

namespace ScoutHelper.Models;

internal record InstanceMob(
	uint MobId,
	uint Instance
);

internal static class InstanceMobExtensions {
	public static InstanceMob AsInstanceMob(this TrainMob mob) => new InstanceMob(mob.MobId, mob.Instance ?? 1u);
	public static InstanceMob AsInstanceMob(this IBattleNpc npc, uint instance) => new InstanceMob(npc.NameId, instance);
}
