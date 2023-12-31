using CSharpFunctionalExtensions;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using ScoutHelper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ScoutHelper.Managers;

public class HuntHelperManager : IDisposable {
	private const uint SupportedVersion = 1;

	private readonly IPluginLog _log;
	private readonly ICallGateSubscriber<uint> _cgGetVersion;
	private readonly ICallGateSubscriber<uint, bool> _cgEnable;
	private readonly ICallGateSubscriber<bool> _cgDisable;
	private readonly ICallGateSubscriber<List<TrainMob>> _cgGetTrainList;

	public bool Available { get; private set; } = false;

	public HuntHelperManager(DalamudPluginInterface pluginInterface, IPluginLog log) {
		_log = log;

		_cgGetVersion = pluginInterface.GetIpcSubscriber<uint>("HH.GetVersion");
		_cgEnable = pluginInterface.GetIpcSubscriber<uint, bool>("HH.Enable");
		_cgDisable = pluginInterface.GetIpcSubscriber<bool>("HH.Disable");
		_cgGetTrainList = pluginInterface.GetIpcSubscriber<List<TrainMob>>("HH.GetTrainList");

		CheckVersion();
		_cgEnable.Subscribe(OnEnable);
		_cgDisable.Subscribe(OnDisable);
	}

	public void Dispose() {
		_cgEnable.Unsubscribe(OnEnable);
		_cgDisable.Unsubscribe(OnDisable);
	}

	private void OnEnable(uint version) {
		CheckVersion(version);
	}

	private void OnDisable() {
		_log.Info("Hunt Helper IPC has been disabled. Disabling support.");
		Available = false;
	}

	private void CheckVersion(uint? version = null) {
		try {
			version ??= _cgGetVersion.InvokeFunc();
			if (version == SupportedVersion) {
				_log.Info("Hunt Helper IPC version {0} detected. Enabling support.", version);
				Available = true;
			} else {
				_log.Warning(
					"Hunt Helper IPC version {0} required, but version {1} detected. Disabling support.",
					SupportedVersion,
					version
				);
				Available = false;
			}
		} catch (IpcNotReadyError) {
			_log.Info("Hunt Helper is not yet available. Disabling support until it is.");
			Available = false;
		}
	}

	public Result<List<TrainMob>, string> GetTrainList() {
		if (!Available) {
			return "Hunt Helper is not currently available ;-;";
		}

		try {
			return _cgGetTrainList.InvokeFunc();
		} catch (IpcNotReadyError) {
			_log.Warning(
				"Hunt Helper appears to have disappeared ;-;. Can't get the train data ;-;. Disabling support until it comes back."
			);
			Available = false;
			return "Hunt Helper has disappeared from my sight ;-;";
		} catch (IpcError e) {
			const string message = "Hmm...something unexpected happened while retrieving train data from Hunt Helper :T";
			_log.Error(e, message);
			return message;
		}
	}
}
