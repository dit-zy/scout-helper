﻿using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Data;
using ScoutHelper.Localization;
using ScoutHelper.Managers;
using ScoutHelper.Windows;
using System;
using System.Globalization;

namespace ScoutHelper;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin {

	public const string Name = "Scout Tracker Helper";

	private const string CommandName = "/sth";

	private WindowSystem WindowSystem { get; } = new("ScoutHelper");

	public static Configuration Conf { get; private set; } = null!;
	[PluginService] public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
	[PluginService] public static IPluginLog Log { get; private set; } = null!;
	[PluginService] public static IChatGui ChatGui { get; private set; } = null!;
	[PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] public static IClientState ClientState { get; private set; } = null!;

	private HuntHelperManager HuntHelperManager { get; init; }
	private BearManager BearManager { get; init; }

	private ConfigWindow ConfigWindow { get; init; }
	private MainWindow MainWindow { get; init; }

	public Plugin() {
		Conf = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		Conf.Initialize(PluginInterface);

		HuntHelperManager = new HuntHelperManager();
		BearManager = new BearManager(Utils.DataFilePath("Bear.json"));

		ConfigWindow = new ConfigWindow();
		MainWindow = new MainWindow(HuntHelperManager, BearManager);

		PluginInterface.LanguageChanged += OnLanguageChanged;
		OnLanguageChanged(PluginInterface.UiLanguage);

		WindowSystem.AddWindow(ConfigWindow);
		WindowSystem.AddWindow(MainWindow);

		CommandManager.AddHandler(
			CommandName,
			new CommandInfo(OnCommand) {
				HelpMessage = "Opens the main window.",
			}
		);

		PluginInterface.UiBuilder.Draw += DrawUi;
		PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
	}

	public void Dispose() {
		PluginInterface.LanguageChanged -= OnLanguageChanged;
		CommandManager.RemoveHandler(CommandName);

		WindowSystem.RemoveAllWindows();

		ConfigWindow.Dispose();
		MainWindow.Dispose();

		HuntHelperManager.Dispose();
	}

	private static void OnLanguageChanged(string languageCode) {
		try {
			Log.Information($"Loading localization for {languageCode}");
			Strings.Culture = new CultureInfo(languageCode);
		}
		catch (Exception e) {
			Log.Error(e, "Unable to load localization for language code: {0}", languageCode);
		}
	}

	private void OnCommand(string command, string args) {
		MainWindow.IsOpen = true;
	}

	private void DrawUi() {
		WindowSystem.Draw();
	}

	public void DrawConfigUi() {
		ConfigWindow.IsOpen = true;
	}
}
