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

namespace ScoutHelper.Managers;

public class VNavMeshManager : IDisposable {

	private readonly IPluginLog _log;
    private readonly ICallGateSubscriber<bool> _vnavIsReady;
    private readonly ICallGateSubscriber<Vector3, bool, bool> _vnavSimpleMoveTo;
    private readonly ICallGateSubscriber<bool> _vnavIsRunning;

    public bool Available { get; private set; } = false;

	public VNavMeshManager(
		IDalamudPluginInterface pluginInterface,
		IPluginLog log
	) {
		_log = log;
		Available = true;
        _vnavIsReady = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Nav.IsReady");
        _vnavSimpleMoveTo = pluginInterface.GetIpcSubscriber<Vector3, bool, bool>("vnavmesh.SimpleMove.PathfindAndMoveTo");
        _vnavIsRunning = pluginInterface.GetIpcSubscriber<bool>("vnavmesh.Path.IsRunning");
        CheckReady();
        _log.Debug("------ Wow we are instanced!");
    }

    private void CheckReady()
    {
        try
        {
            var ready = _vnavIsReady.InvokeFunc();
            if (ready)
            {
                _log.Info("VNavMesh is ready!");
                Available = true;
            }
            else
            {
                _log.Warning("VNavMesh is not ready?");
                Available = false;
            }
        }
        catch (IpcNotReadyError)
        {
            _log.Info("VNavMesh is not yet available. Disabling support until it is.");
            Available = false;
        }
    }
    //

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

    public void SimpleMoveTo(Vector3 loc, bool shouldFly)
    {
        try
        {
            var ready = _vnavSimpleMoveTo.InvokeFunc(loc, shouldFly);
        }
        catch (IpcNotReadyError)
        {
            _log.Info("VNavMesh: Could not move. (IpcNotReadyError)");
            Available = false;
        }
    }

    public void Dispose() {
        _log.Debug("------ Wow we are disposed!");
    }

	private void OnDisable() {
		_log.Info("VNavMesh IPC has been disabled. Disabling support.");
		Available = false;
	}
}
