using CSharpFunctionalExtensions;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using ScoutHelper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ScoutHelper.Utils;
using System.Numerics;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Dalamud.Game.ClientState.Conditions;
using Lumina.Excel.Sheets;

namespace ScoutHelper.Managers;

public class MovementManager : IDisposable {
    public List<Vector3> KholusiaWaypoints = new(){
        new Vector3((float) -219.9,(float) 476.7,(float) -734.2), // "17.1, 6.8"
        new Vector3((float) -95.7,(float) 428.2,(float) -547.3), // "19.5, 10.5"
        new Vector3((float) 14.9,(float) 366.9,(float) -379.7), // "21.8, 13.9"
        new Vector3((float) 164.8,(float) 342.5,(float) -504.4), // "24.8, 11.4"
        new Vector3((float) 494.5,(float) 302.2,(float) -100.0), // "31.4, 19.5"
        new Vector3((float) 256.1,(float) 326.7,(float) -115.2), // "26.7, 19.2"
        new Vector3((float) 77.4,(float) 351.1,(float) -201.3), // "23.0, 17.5"
        new Vector3((float) -1.4,(float) 332.0,(float) 40.8), // "21.2, 22.4"
        new Vector3((float) -335.5,(float) 379.0,(float) -295.9), // "14.8, 15.6"
        new Vector3((float) -494.4,(float) 367.6,(float) -144.9), // "11.6, 18.6"
        new Vector3((float) -627.0,(float) 79.7,(float) 200.5), // "8.9, 25.5"
        new Vector3((float) -326.9,(float) 65.6, (float) 133.9), // "14.9, 24.2"
        new Vector3((float) -31.7,(float) 32.2,(float) 484.4), // "20.9, 31.2"
        new Vector3((float) 159.8,(float) 34.8,(float) 419.2), // "24.7, 29.8"
        new Vector3((float) 266.7,(float) 47.9,(float) 143.6), // "26.8, 24.3"
        new Vector3((float) 646.5,(float) 58.4,(float) 131.7), // "34.4, 24.1"
        new Vector3((float) 423.4,(float) 26.8,(float) 420.3) // "30.0, 30.0"
    };

    public List<Vector3> LakelandWaypoints = new() {
        new Vector3((float) -489.6,(float) 88.7,(float) -444.5), // "11.7, 12.6"
        new Vector3((float) -94.4,(float) 108.1,(float) -594.9), // "19.7, 9.7"
        new Vector3((float) 87.6,(float) 143.0,(float) -455.1), // "23.2, 12.3"
        new Vector3((float) 303.9,(float) 143.1,(float) -305.7), // "27.7, 15.3"
        new Vector3((float) 751.1,(float) 105.3,(float) -471.8), // "36.6, 12.2"
        new Vector3((float) 679.4,(float) 44.5,(float) -274.9), // "35.1, 16.0"
        new Vector3((float) 399.9,(float) 139.4,(float) -106.6), // "29.5, 19.3"
        new Vector3((float) 459.4,(float) 20.5,(float) 59.4), // "30.7, 22.6"
        new Vector3((float) 709.2,(float) 27.7,(float) 274.7), // "35.7, 27.0"
        new Vector3((float) 684.3,(float) 20.4,(float) 534.5), // "35.2, 32.2"
        new Vector3((float) 258.0,(float) 34.9,(float) 788.6), // "26.7 37.3"
        new Vector3((float) 322.0,(float) 20.8,(float) 463.8), // "27.9 30.8"
        new Vector3((float) 74.9,(float) -1.6,(float) 407.8), // "23.1, 29.7"
        new Vector3((float) 199.8,(float) 19.3,(float) 120.0), // "25.5, 23.9"
        new Vector3((float)-159.7,(float) 15.6,(float) 74.4), // "18.3, 23.0"
        new Vector3((float)-394.3,(float) 24.9,(float) 146.7), // "13.6, 24.4"
        new Vector3((float)-682.7,(float) 86.6,(float) 59.1), // "7.8, 22.6"
        new Vector3((float)-512.9,(float) 40.8,(float) -215.0) // "11.3, 17.2"
    };

    public List<Vector3> AhmAhrengWaypoints = new() {
        new Vector3((float) -499.5,(float) 45.0,(float) -105.0), // "11.5, 19.4"
        new Vector3((float) -567.4,(float) 16.9,(float) -475.1), // "10.2, 11.9"
        new Vector3((float) -239.9,(float) 61.8,(float) -564.4), // "16.7, 10.2"
        new Vector3((float) 54.8,(float) 71.1,(float) -549.5), // "22.6, 10.5"
        new Vector3((float) -108.5,(float) 47.7,(float) -269.1), // "19.3, 16.1"
        new Vector3((float) 352.9,(float) -8.3,(float) -458.0), // "28.6, 12.3"
        new Vector3((float) 470.4,(float) -18.2,(float) -391.9), // "30.9, 13.7"
        new Vector3((float) 364.7,(float) -2.9,(float) -60.1), // "28.8, 20.3"
        new Vector3((float) 599.8,(float) -12.3,(float) 2.3), // "33.5, 21.5"
        new Vector3((float) 348.0,(float) -5.7,(float) 227.0), // "28.5, 26.0"
        new Vector3((float) 574.5,(float) -87.7,(float) 620.5), // "33.0, 33.9"
        new Vector3((float) 444.5,(float) -63.8,(float) 688.2), // "30.4, 35.3"
        new Vector3((float) -83.1,(float) -37.4,(float) 743.4), // "19.9, 36.4"
        new Vector3((float) -214.6,(float) -36.3,(float) 504.2), // "17.2, 31.6"
        new Vector3((float) 82.1,(float) -83.6,(float) 410.5), // "23.1, 29.7"
        new Vector3((float) -82.2,(float) -27.1,(float) 364.5), // "19.8, 28.8"
        new Vector3((float) -114.8,(float) -14.5,(float) 170.0), // "19.2, 24.9"
        new Vector3((float) -257.3,(float) 32.7,(float) 124.2) // "16.3, 24.0"
    };
    public List<Vector3> IlMhegWaypoints = new() {
        new Vector3((float) -119.9,(float) 20.9,(float) 279.8), // "19.1, 27.1"
        new Vector3((float) 64.8,(float) 18.7,(float) 364.6), // "22.8, 28.8"
        new Vector3((float) 154.8,(float) 13.7,(float) 559.3), // "24.6, 32.7"
        new Vector3((float) 159.8,(float) 15.5,(float) 789.1), // "24.7, 37.7"
        new Vector3((float) -89.7,(float) 47.1,(float) 669.4), // "19.7, 34.9"
        new Vector3((float) -394.4,(float) 87.7,(float) 629.4), // "13.6, 34.1"
        new Vector3((float) -589.4,(float) 79.1,(float) 459.6), // "9.7 30.7"
        new Vector3((float) -683.7,(float) 56.4,(float) 271.5), // "7.8, 27.0"
        new Vector3((float) -699.3,(float) 50.8,(float) 75.1), // "7.5, 23.0"
        new Vector3((float) -534.6,(float) 29.4,(float) -59.9), // "10.8, 20.3"
        new Vector3((float) -515.0,(float) 55.6,(float) -284.4), // "11.2, 16.0"
        new Vector3((float) -67.4,(float) 73.9,(float) -649.3), // "20.2, 8.5"
        new Vector3((float) 199.6,(float) 99.3,(float) -719.2), // "25.5, 7.1"
        new Vector3((float) 389.5,(float) 110.0,(float) -804.2), // "29.3, 5.4"
        new Vector3((float) 614.3,(float) 123.8,(float) -709.3), // "33.8, 7.3"
        new Vector3((float) 504.6,(float) 110.5,(float) -394.7), // "31.6, 13.6"
        new Vector3((float) 286.8,(float) 59.5,(float) -126.6) // "27.2, 19.0"
    };

    public List<Vector3> RakTikaWaypoints = new()
    {
        new Vector3((float) -238.6,(float) 36.6,(float) 128.9), // "16.7, 24.0"
        new Vector3((float) -334.6,(float) 32.5,(float) 40.0), // "14.8, 22.3"
        new Vector3((float) -319.7,(float) 33.0,(float) 434.5), // "15.1, 30.2"
        new Vector3((float) -193.0,(float) 27.2,(float) 687.1), // "17.8, 35.1"
        new Vector3((float) -475.5,(float) 21.2,(float) 719.1), // "12.0, 35.9"
        new Vector3((float) -648.8,(float) 26.2,(float) 654.9), // "8.5, 34.6"
        new Vector3((float) -579.5,(float) 30.6,(float) 125.1), // "9.9, 24.0"
        new Vector3((float) -599.8,(float) 44.3,(float) -143.7), // "9.5, 18.6"
        new Vector3((float) 24.8,(float) 22.7,(float) -389.6), // "22.0, 13.7"
        new Vector3((float) 50.1,(float) 0.3,(float) -543.9), // "22.5, 10.7"
        new Vector3((float) 246.7,(float) 21.2,(float) -330.9), // "26.3, 14.9"
        new Vector3((float) 594.3,(float) 41.6,(float) 69.9), // "33.4, 22.9"
        new Vector3((float) 416.7,(float) 49.9,(float) 224.6), // "29.7, 26.0"
        new Vector3((float) 239.1,(float) 40.2,(float) 139.9), // "26.3, 24.3"
        new Vector3((float) 196.7,(float) 8.5,(float) 437.2) // "25.3, 30.4"
    };

    public List<Vector3> TempestWaypoints = new()
    {
        new Vector3((float) 460.1,(float) 399.0,(float) -527.6), // "30.7, 10.9"
        new Vector3((float) 755.6,(float) 436.8,(float) -494.0), // "36.7, 11.6"
        new Vector3((float) 811.8,(float) 422.3,(float) -244.1), // "37.7, 16.7"
        new Vector3((float) 729.3,(float) 417.9,(float) -75.1), // "36.1, 20.0"
        new Vector3((float) 609.5,(float) 402.8,(float) -0.0), // "33.7, 21.6"
        new Vector3((float) 384.1,(float) 361.2,(float) 64.9), // "29.1, 22.8"
        new Vector3((float) 159.4,(float) 301.6,(float) 163.5), // "24.7, 24.8"
        new Vector3((float) -310.2,(float) 297.4,(float) -54.8), // "15.4 20.4"
        new Vector3((float) -399.6,(float) 161.9,(float) -204.7), // "13.5, 17.4"
        new Vector3((float) -164.9,(float) 386.9,(float) -404.6), // "18.2, 13.4"
        new Vector3((float) -284.6,(float) 411.2,(float) -565.8), // "15.7, 10.3"
        new Vector3((float) -654.3,(float) 408.3,(float) -654.3), // "8.4, 8.4"
        new Vector3((float) -503.9,(float) 402.8,(float) -811.9), // "11.4, 5.2"
        new Vector3((float) -20.1,(float) 443.5,(float) -689.3), // "21.1, 7.7"
        new Vector3((float) 185.8,(float) 382.7,(float) -427.9), // "25.3, 13.0"
        new Vector3((float) 344.6,(float) 400.7,(float) -654.2), // "28.4, 8.4"
        new Vector3((float) 484.5,(float) 457.8,(float) -874.0) // "31.2, 4.0"
    };

    private readonly IPluginLog _log;
    private readonly ICallGateSubscriber<bool> _vnavIsReady;
    private readonly ICallGateSubscriber<int> _vnavNumWaypoints;
    private readonly ICallGateSubscriber<Vector3, bool, bool> _vnavSimpleMoveTo;
    private readonly ICallGateSubscriber<bool> _vnavIsRunning;
    private readonly ICallGateSubscriber<string, bool> _lifestreamExecuteCommand;
    private readonly ICallGateSubscriber<bool> _lifestreamIsBusy;
    private TimeSpan _lastUpdate = new(0);
    private TimeSpan _execDelay = new(0, 0, 1);
    private ushort _targetTerritory = 0;

    public List<Vector3> EnqueuedWaypoints = new();

    public bool Available { get; private set; } = false;

    public bool CanAct
    {
        get
        {
            if (Dalamud.ClientState.LocalPlayer == null)
                return false;
            if (Dalamud.Conditions[ConditionFlag.BetweenAreas] ||
                Dalamud.Conditions[ConditionFlag.BetweenAreas51] ||
                Dalamud.Conditions[ConditionFlag.BeingMoved] ||
                Dalamud.Conditions[ConditionFlag.Casting] ||
                Dalamud.Conditions[ConditionFlag.Casting87] ||
                Dalamud.Conditions[ConditionFlag.Jumping] ||
                Dalamud.Conditions[ConditionFlag.Jumping61] ||
                Dalamud.Conditions[ConditionFlag.LoggingOut] ||
                Dalamud.Conditions[ConditionFlag.Occupied] ||
                Dalamud.Conditions[ConditionFlag.Unconscious] ||
                Dalamud.ClientState.LocalPlayer.CurrentHp < 1)
                return false;
            return true;
        }
    }

    // 813 -> Lakeland

    public MovementManager(
		IDalamudPluginInterface pluginInterface,
		IPluginLog log
	) {
		_log = log;
		Available = true;
        _vnavIsReady = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        _vnavNumWaypoints = pluginInterface.GetIpcSubscriber<int>("vnavmesh.Path.NumWaypoints");
        _vnavSimpleMoveTo = pluginInterface.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
        _vnavIsRunning = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
        _lifestreamExecuteCommand = pluginInterface.GetIpcSubscriber<string,bool>("Lifestream.ExecuteCommand");
        _lifestreamIsBusy = pluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
        _log.Debug("------ Wow we are instanced!");
        Dalamud.Framework.Update += Tick;
    }

    private unsafe void DoUpdate(IFramework framework)
    {
        _log.Debug("DoUpdate! ways " + EnqueuedWaypoints.Count + " !rdy " + !IsReady() + " run " + IsRunning() + " busy " + IsBusy() + " !canact" + !CanAct);
        if (EnqueuedWaypoints.Count < 1 || !IsReady() || IsRunning() || IsBusy() || !CanAct) {
            return;
        }
        _log.Debug("Update run");
        _log.Debug("We are in " + Dalamud.ClientState.TerritoryType + "tgt " + _targetTerritory);
        if (Dalamud.ClientState.TerritoryType != _targetTerritory)
        {
            _log.Debug("We are not in the target territory, not pathing...");
            return;
        }
        var am = ActionManager.Instance();
        _log.Debug("Are we mounted? " + Dalamud.Conditions[ConditionFlag.Mounted]);
        if (!Dalamud.Conditions[ConditionFlag.Mounted])
        {
            _log.Debug("We are not mounted, mounting...");
            am->UseAction(ActionType.GeneralAction, 24);
            return;
        }
        _log.Debug("DoUpdate! NumWaypoints " + NumWaypoints());
        var res = SimpleMoveTo(EnqueuedWaypoints[0], true);
        _log.Debug("DoUpdate! path find to " + EnqueuedWaypoints[0] +" was " + res);
        if (res)
        {
            EnqueuedWaypoints.RemoveAt(0);
            _log.Debug(EnqueuedWaypoints.Count + " Waypoints left...");
        } else
        {
            _log.Debug("Wanted to pathfind & move but could not...");
        }
    }

    public void Stop()
    {
        EnqueuedWaypoints = new();
    }

    private void Tick(IFramework framework)
    {
        _lastUpdate += framework.UpdateDelta;
        if(_lastUpdate > _execDelay)
        {
            DoUpdate(framework);
            _lastUpdate = new(0);
        }
    }

    public void ScoutLakeland()
    {
        if (EnqueuedWaypoints.Count > 0)
            return;
        _targetTerritory = 813;
        if (Dalamud.ClientState.TerritoryType != 813)
        {
            LifestreamExecuteCommand("tp The Ostall Imperative");
        }
        EnqueuedWaypoints.AddRange(LakelandWaypoints);
    }

    public void ScoutKholusia()
    {
        if (EnqueuedWaypoints.Count > 0)
            return;
        _targetTerritory = 814;
        if (Dalamud.ClientState.TerritoryType != 814)
        {
            LifestreamExecuteCommand("tp Tomra");
        }
        EnqueuedWaypoints.AddRange(KholusiaWaypoints);
    }

    public void ScoutAhmAhreng()
    {
        if (EnqueuedWaypoints.Count > 0)
            return;
        _targetTerritory = 815;
        if (Dalamud.ClientState.TerritoryType != 815)
        {
            LifestreamExecuteCommand("tp Twine");
        }
        EnqueuedWaypoints.AddRange(AhmAhrengWaypoints);
    }

    public void ScoutIlMheg()
    {
        if (EnqueuedWaypoints.Count > 0)
            return;
        _targetTerritory = 816;
        if (Dalamud.ClientState.TerritoryType != 816)
        {
            LifestreamExecuteCommand("tp Lydha Lran");
        }
        EnqueuedWaypoints.AddRange(IlMhegWaypoints);
    }

    public void ScoutRakTika()
    {
        if (EnqueuedWaypoints.Count > 0)
            return;
        _targetTerritory = 817;
        if (Dalamud.ClientState.TerritoryType != 817)
        {
            LifestreamExecuteCommand("tp Slitherbough");
        }
        EnqueuedWaypoints.AddRange(RakTikaWaypoints);
    }

    public void ScoutTempest()
    {
        if (EnqueuedWaypoints.Count > 0)
            return;
        _targetTerritory = 818;
        if (Dalamud.ClientState.TerritoryType != 818)
        {
            LifestreamExecuteCommand("tp Ondo Cups");
        }
        EnqueuedWaypoints.AddRange(TempestWaypoints);
    }

    private bool IsReady()
    {
        try
        {
            return _vnavIsReady.InvokeFunc();
        }
        catch (IpcNotReadyError)
        {
            _log.Info("VNavMesh is not yet available. Disabling support until it is.");
            return false;
        }
    }

    private int NumWaypoints()
    {
        try
        {
            return _vnavNumWaypoints.InvokeFunc();
        }
        catch (IpcNotReadyError)
        {
            _log.Info("VNavMesh is not yet available. Disabling support until it is.");
            return 0;
        }
    }
    //

    public void LifestreamExecuteCommand(string command)
    {
        try
        {
            _lifestreamExecuteCommand.InvokeAction(command);
        }
        catch (IpcNotReadyError)
        {
            _log.Warning("Lifestream: Could not execute command "+command+" (IpcNotReadyError)");
        }
    }

    public bool IsRunning()
    {
        try
        {
            return _vnavIsRunning.InvokeFunc();
        }
        catch (IpcNotReadyError)
        {
            _log.Warning("VNavMesh: Could not check if path is running (IpcNotReadyError)");
            return false;
        }
    }

    public bool IsBusy()
    {
        try
        {
            return _lifestreamIsBusy.InvokeFunc();
        }
        catch (IpcNotReadyError)
        {
            _log.Warning("Lifestream: Could not check if is busy (IpcNotReadyError)");
            return false;
        }
    }

    public bool SimpleMoveTo(Vector3 loc, bool shouldFly)
    {
        try
        {
            return _vnavSimpleMoveTo.InvokeFunc(loc, shouldFly);
        }
        catch (IpcNotReadyError)
        {
            _log.Info("VNavMesh: Could not move. (IpcNotReadyError)");
            return false;
        }
    }

    public void Dispose() {
        _log.Debug("------ Wow we are disposed!");
        Dalamud.Framework.Update -= Tick;
    }

	private void OnDisable() {
		_log.Info("VNavMesh IPC has been disabled. Disabling support.");
		Available = false;
	}
}
