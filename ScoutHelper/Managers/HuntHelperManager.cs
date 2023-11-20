using CSharpFunctionalExtensions;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using ScoutHelper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScoutHelper.Managers;

public class HuntHelperManager : IDisposable {

	private const uint SupportedVersion = 1;

	private readonly ICallGateSubscriber<uint> _cgGetVersion;
	private readonly ICallGateSubscriber<uint, bool> _cgEnable;
	private readonly ICallGateSubscriber<bool> _cgDisable;
	private readonly ICallGateSubscriber<List<TrainMob>> _cgGetTrainList;

	public bool Available { get; private set; } = false;

	public HuntHelperManager() {
		_cgGetVersion = Plugin.PluginInterface.GetIpcSubscriber<uint>("HH.GetVersion");
		_cgEnable = Plugin.PluginInterface.GetIpcSubscriber<uint, bool>("HH.Enable");
		_cgDisable = Plugin.PluginInterface.GetIpcSubscriber<bool>("HH.Disable");
		_cgGetTrainList = Plugin.PluginInterface.GetIpcSubscriber<List<TrainMob>>("HH.GetTrainList");

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
		Plugin.Log.Info("Hunt Helper IPC has been disabled. Disabling support.");
		Available = false;
	}

	private void CheckVersion(uint? version = null) {
		try {
			version ??= _cgGetVersion.InvokeFunc();
			if (version == SupportedVersion) {
				Plugin.Log.Info("Hunt Helper IPC version {0} detected. Enabling support.", version);
				Available = true;
			}
			else {
				Plugin.Log.Warning(
					"Hunt Helper IPC version {0} required, but version {1} detected. Disabling support.",
					SupportedVersion,
					version
				);
				Available = false;
			}
		}
		catch (IpcNotReadyError) {
			Plugin.Log.Info("Hunt Helper is not yet available. Disabling support until it is.");
			Available = false;
		}
	}

	public Result<List<TrainMob>, string> GetTrainList() {

		if (!Available) {
			return "Hunt Helper is not currently available ;-;";
		}

		try {
			return _cgGetTrainList.InvokeFunc();
		}
		catch (IpcNotReadyError) {
			Plugin.Log.Warning("Hunt Helper appears to have disappeared ;-;. Can't get the train data ;-;. Disabling support until it comes back.");
			Available = false;
			return "Hunt Helper has disappeared from my sight ;-;";
		}
		catch (IpcError e) {
			const string message = "Hmm...something unexpected happened while retrieving train data from Hunt Helper :T";
			Plugin.Log.Error(e, message);
			return message;
		}
	}
}
