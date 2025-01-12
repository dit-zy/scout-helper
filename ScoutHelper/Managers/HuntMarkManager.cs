using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Types;
using XIVHuntUtils.Models;
using System.Linq;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using static XIVHuntUtils.Utils.XivUtils;

namespace ScoutHelper.Managers;
using MobDict = IDictionary<uint, (Patch patch, uint turtleMobId)>;
public class HuntMarkManager : IDisposable {

	private readonly IFramework _framework;
	private readonly IObjectTable _objectTable;
	private readonly IClientState _clientState;
	private readonly IPluginLog _log;
	private readonly IChatGui _chat;

	private TimeSpan _lastUpdate = new(0);
	private TimeSpan _execDelay = new(0, 0, 1);

	private List<uint> _ARankbNPCIds = new();
	private List<uint> _sentARankIds = new();

	public event Action<ScoutHelper.Models.TrainMob> OnMarkFound;

	public HuntMarkManager(
				IFramework framework,
				IPluginLog log,
				IChatGui chat,
				IClientState clientState,
				IObjectTable objectTable
		) {
		_framework = framework;
		_log = log;
		_chat = chat;
		_clientState = clientState;
		_objectTable = objectTable;
	}

	private bool IsHWTerritory(uint territoryId) {
		//EVERYTHING EXCEPT HEAVENSWARD HAS A SCALE OF 100, BUT FOR SOME REASON HW HAS 95
		if (territoryId is >= 397 and <= 402) return true;
		return false;
	}

	private unsafe uint GetCurrentInstance() {
		return UIState.Instance()->PublicInstance.InstanceId;
	}

	private void CheckObjectTable() {
		foreach (var obj in _objectTable) {
			if (obj is not IBattleNpc mob) continue;
			var battlenpc = mob as IBattleNpc;

			if (_ARankbNPCIds.Contains(battlenpc.NameId)) {
				if (_sentARankIds.Contains(battlenpc.NameId)) {
					//_log.Debug($"Got that A already...");
					continue;
				}
				var trainMob = new ScoutHelper.Models.TrainMob();

				trainMob.Name = battlenpc.Name.ToString();
				trainMob.MobId = battlenpc.NameId;
				trainMob.TerritoryId = _clientState.TerritoryType;
				//trainMob.MapId =
				trainMob.Instance = GetCurrentInstance();
				trainMob.Position = XIVHuntUtils.Utils.XivUtils.AsMapPosition(new Vector2(trainMob.Position.X, trainMob.Position.Z), IsHWTerritory(trainMob.TerritoryId));
				trainMob.Dead = battlenpc.IsDead;
				//trainMob.LastSeenUtc = 
				_log.Debug($"I spy with my little eye: {trainMob.Name} ({trainMob.MobId}) in {trainMob.TerritoryId} i{trainMob.Instance} @{trainMob.Position} Dead?{trainMob.Dead}");
				OnMarkFound.Invoke(trainMob);
				_sentARankIds.Add(trainMob.MobId);
			}
		}
	}

	private void Tick(IFramework framework) {
		_lastUpdate += framework.UpdateDelta;
		if (_lastUpdate > _execDelay) {
			DoUpdate(framework);
			_lastUpdate = new(0);
		}
	}

	private void DoUpdate(IFramework framework) {
		CheckObjectTable();
	}

	public void StartLooking(MobDict MobIdToTurtleId) {
		_log.Debug("HuntMarkManager: Start looking for Ranks");
		_ARankbNPCIds = MobIdToTurtleId.Keys.ToList();
		_sentARankIds = new();
		_framework.Update += Tick;
	}

	public void StopLooking() {
		_log.Debug("HuntMarkManager: Stop looking for Ranks");
		_framework.Update -= Tick;
	}

	public void Dispose() {
		_framework.Update -= Tick;
	}

	/*
private void OnMarkSeen(TrainMob mark) {
	if (!_turtleManager.IsTurtleCollabbing) return;

	_turtleManager.UpdateCurrentSession(mark.AsSingletonList())
		.ContinueWith(
			task => {
				switch (task.Result) {
					case TurtleHttpStatus.Success:
						_chat.TaggedPrint($"added {mark.Name} to the turtle session.");
						break;
					case TurtleHttpStatus.NoSupportedMobs:
						_chat.TaggedPrint($"{mark.Name} was seen, but is not supported by turtle and will not be added to the session.");
						break;
					case TurtleHttpStatus.HttpError:
						_chat.TaggedPrintError($"something went wrong when adding {mark.Name} to the turtle session ;-;.");
						break;
				}
			},
			TaskContinuationOptions.OnlyOnRanToCompletion
		)
		.ContinueWith(
			task => _log.Error(task.Exception, "failed to update turtle session"),
			TaskContinuationOptions.OnlyOnFaulted
		);
}
*/

}
