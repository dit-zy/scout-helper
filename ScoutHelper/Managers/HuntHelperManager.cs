﻿using CSharpFunctionalExtensions;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Ipc.Exceptions;
using ScoutHelper.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ScoutHelper.Utils;

namespace ScoutHelper.Managers;

public class HuntHelperManager : IDisposable {
	private const uint SupportedVersion = 1;

	private readonly IPluginLog _log;
	private readonly IChatGui _chat;
	private readonly TurtleManager _turtleManager;
	private readonly ICallGateSubscriber<uint> _cgGetVersion;
	private readonly ICallGateSubscriber<uint, bool> _cgEnable;
	private readonly ICallGateSubscriber<bool> _cgDisable;
	private readonly ICallGateSubscriber<List<TrainMob>> _cgGetTrainList;
	private readonly ICallGateSubscriber<TrainMob, bool> _cgMarkSeen;

	public bool Available { get; private set; } = false;

	public HuntHelperManager(
		IDalamudPluginInterface pluginInterface,
		IPluginLog log,
		IChatGui chat,
		TurtleManager turtleManager
	) {
		_log = log;
		_chat = chat;
		_turtleManager = turtleManager;

		_cgGetVersion = pluginInterface.GetIpcSubscriber<uint>("HH.GetVersion");
		_cgEnable = pluginInterface.GetIpcSubscriber<uint, bool>("HH.Enable");
		_cgDisable = pluginInterface.GetIpcSubscriber<bool>("HH.Disable");
		_cgGetTrainList = pluginInterface.GetIpcSubscriber<List<TrainMob>>("HH.GetTrainList");
		_cgMarkSeen = pluginInterface.GetIpcSubscriber<TrainMob, bool>("HH.channel.MarkSeen");

		CheckVersion();
		_cgEnable.Subscribe(OnEnable);
		_cgDisable.Subscribe(OnDisable);
		_cgMarkSeen.Subscribe(OnMarkSeen);
	}

	public void Dispose() {
		_cgEnable.Unsubscribe(OnEnable);
		_cgDisable.Unsubscribe(OnDisable);
		_cgMarkSeen.Unsubscribe(OnMarkSeen);
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
