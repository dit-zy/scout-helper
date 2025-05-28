using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DitzyExtensions;
using DitzyExtensions.Collection;
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
	private static readonly TimeSpan MobPermanenceDuration = TimeSpan.FromSeconds(2);

	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;
	private readonly IClientState _clientState;
	private readonly IPluginLog _log;
	private readonly IChatGui _chat;
	private readonly IMobManager _mobManager;

	private readonly Dictionary<InstanceMob, DateTime> _seenMobs = new();

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

	private unsafe uint CurrentInstance => UIState.Instance()->PublicInstance.InstanceId;

	private void CheckObjectTable() {
		var now = DateTime.Now;

		_seenMobs
			.AsPairs()
			.Where(entry => MobPermanenceDuration < now - entry.val)
			.ForEach(entry => _seenMobs.Remove(entry.key));

		foreach (var obj in _objectTable) {
			if (obj is not IBattleNpc mob) continue;

			if (_mobManager.FindMobName(mob.NameId).HasNoValue) continue;
			if (_seenMobs.ContainsKey(mob.AsInstanceMob(CurrentInstance))) {
				_seenMobs.Put(mob.AsInstanceMob(CurrentInstance), now);
				continue;
			}

			var trainMob = new TrainMob();
			trainMob.Name = mob.Name.ToString();
			trainMob.MobId = mob.NameId;
			trainMob.TerritoryId = _clientState.TerritoryType;
			trainMob.Instance = CurrentInstance;
			trainMob.Position = MathUtils.V2(
				mob.Position.X,
				mob.Position.Z
			).AsMapPosition(trainMob.TerritoryId);
			trainMob.Dead = mob.IsDead;

			_log.Debug("hunt mark spotted: {@mob}", trainMob);

			OnMarkFound?.Invoke(trainMob);
			_seenMobs.Add(trainMob.AsInstanceMob(), now);
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
