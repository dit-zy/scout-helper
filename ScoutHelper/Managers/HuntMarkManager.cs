using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DitzyExtensions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ScoutHelper.Models;
using XIVHuntUtils.Managers;
using XIVHuntUtils.Models;
using static XIVHuntUtils.Utils.XivUtils;
using TrainMob = ScoutHelper.Models.TrainMob;

namespace ScoutHelper.Managers;

using MobDict = IDictionary<uint, (Patch patch, uint turtleMobId)>;

public class HuntMarkManager : IDisposable {
	private static readonly TimeSpan ExecDelay = TimeSpan.FromSeconds(1);

	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;
	private readonly IClientState _clientState;
	private readonly IPluginLog _log;
	private readonly IChatGui _chat;
	private readonly IMobManager _mobManager;

	private readonly ISet<InstanceMob> _seenMobs = new HashSet<InstanceMob>();

	private DateTime _lastUpdate = DateTime.Now;

	public event Action<TrainMob>? OnMarkFound;

	public HuntMarkManager(
		IFramework framework,
		IPluginLog log,
		IChatGui chat,
		IClientState clientState,
		IObjectTable objectTable,
		IMobManager mobManager
	) {
		_framework = framework;
		_log = log;
		_chat = chat;
		_clientState = clientState;
		_objectTable = objectTable;
		_mobManager = mobManager;
	}

	[Obsolete("this method is only needed until xiv hunt utils updates with an equivalent.")]
	private bool IsHwTerritory(uint territoryId) {
		return territoryId is >= 397 and <= 402;
	}

	private unsafe uint CurrentInstance => UIState.Instance()->PublicInstance.InstanceId;

	private void CheckObjectTable() {
		foreach (var obj in _objectTable) {
			if (obj is not IBattleNpc mob) continue;

			if (_mobManager.FindMobName(mob.NameId).HasNoValue) continue;
			if (_seenMobs.Contains(mob.AsInstanceMob(CurrentInstance))) continue;

			var trainMob = new TrainMob();
			trainMob.Name = mob.Name.ToString();
			trainMob.MobId = mob.NameId;
			trainMob.TerritoryId = _clientState.TerritoryType;
			trainMob.Instance = CurrentInstance;
			trainMob.Position = MathUtils.V2(
				mob.Position.X,
				mob.Position.Z
			).AsMapPosition(IsHwTerritory(trainMob.TerritoryId));
			trainMob.Dead = mob.IsDead;

			_log.Debug("hunt mark spotted: {@mob}", trainMob);

			OnMarkFound?.Invoke(trainMob);
			_seenMobs.Add(trainMob.AsInstanceMob());
		}
	}

	private void Tick(IFramework framework) {
		var now = DateTime.Now;
		if (now - _lastUpdate <= ExecDelay) return;

		CheckObjectTable();
		_lastUpdate = now;
	}

	public void StartLooking() {
		_log.Debug("start watching for hunt marks...");
		_seenMobs.Clear();
		_framework.Update += Tick;
	}

	public void StopLooking() {
		_log.Debug("stop watching for hunt marks.");
		_framework.Update -= Tick;
	}

	public void Dispose() {
		StopLooking();
		GC.SuppressFinalize(this);
	}
}
