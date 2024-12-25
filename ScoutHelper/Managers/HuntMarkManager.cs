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
		IDalamudPluginInterface pluginInterface,
		IFramework framework,
		IObjectTable objectTable,
        IClientState clientState,
        IPluginLog log,
		IChatGui chat
	) {
		_framework = framework;
		_log = log;
		_chat = chat;
		_clientState = clientState;
		_objectTable = objectTable;
	}

    private static float ConvertToMapCoordinate(float pos, float mapScale)
    {
        return 2048f / mapScale + pos / 50f + 1f;
    }
    private static Vector2 ConvertToMapCoordinate(Vector3 pos, float mapScale)
    {
        return new Vector2(
            ConvertToMapCoordinate(pos.X, mapScale),
            ConvertToMapCoordinate(pos.Z, mapScale)
        );
    }
    private float GetMapZoneScale(uint territoryId)
    {
        //EVERYTHING EXCEPT HEAVENSWARD HAS A SCALE OF 100, BUT FOR SOME REASON HW HAS 95
        if (territoryId is >= 397 and <= 402) return 95f;
        return 100f;
    }

	private unsafe uint GetCurrentInstance()
	{
		return UIState.Instance()->PublicInstance.InstanceId;
	}

    private void CheckObjectTable()
    {
        foreach (var obj in _objectTable)
        {
            if (obj is not IBattleNpc mob) continue;
            var battlenpc = mob as IBattleNpc;			
			
            if (_ARankbNPCIds.Contains(battlenpc.NameId))
            {
				if(_sentARankIds.Contains(battlenpc.NameId))
				{
                    //_log.Debug($"Got that A already...");
                    continue;
				}
				var trainMob = new ScoutHelper.Models.TrainMob();
				
				trainMob.Name = battlenpc.Name.ToString();
				trainMob.MobId = battlenpc.NameId;
				trainMob.TerritoryId = _clientState.TerritoryType;
				//trainMob.MapId =
				trainMob.Instance = GetCurrentInstance();
                trainMob.Position = ConvertToMapCoordinate(battlenpc.Position, GetMapZoneScale(trainMob.TerritoryId));
				trainMob.Dead = battlenpc.IsDead;
                //trainMob.LastSeenUtc = 
                _log.Debug($"I spy with my little eye: {trainMob.Name} ({trainMob.MobId}) in {trainMob.TerritoryId} i{trainMob.Instance} @{trainMob.Position} Dead?{trainMob.Dead}");
				OnMarkFound.Invoke(trainMob);
				_sentARankIds.Add(trainMob.MobId);
            }
        }
    }

    private void Tick(IFramework framework)
    {
        _lastUpdate += framework.UpdateDelta;
        if (_lastUpdate > _execDelay)
        {
            DoUpdate(framework);
            _lastUpdate = new(0);
        }
    }

	private void DoUpdate(IFramework framework)
	{
		//_log.Debug("HMM: UPDAATE!");
		CheckObjectTable();
	}

    public void StartLooking(MobDict MobIdToTurtleId)
	{
		_log.Debug("HuntMarkManager: Start looking for Ranks");
        _ARankbNPCIds = MobIdToTurtleId.Keys.ToList();
		_sentARankIds = new();
        _framework.Update += Tick;
    }

	public void StopLooking()
	{
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
