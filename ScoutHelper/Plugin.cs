using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ScoutHelper.Localization;
using ScoutHelper.Managers;
using ScoutHelper.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ScoutHelper;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin {

	public const string Name = Constants.PluginName;

	private static readonly List<string> CommandNames = new List<string>() {"/scouth", "/sch"};

	public static Configuration Conf { get; private set; } = null!;

	[RequiredVersion("1.0"), PluginService]
	public static DalamudPluginInterface PluginInterface { get; private set; } = null!;
	[RequiredVersion("1.0"), PluginService]
	public static IPluginLog Log { get; private set; } = null!;
	[RequiredVersion("1.0"), PluginService]
	public static IChatGui ChatGui { get; private set; } = null!;
	[RequiredVersion("1.0"), PluginService]
	public static ICommandManager CommandManager { get; private set; } = null!;
	[RequiredVersion("1.0"), PluginService]
	public static IClientState ClientState { get; private set; } = null!;

	private WindowSystem WindowSystem { get; } = new WindowSystem(Constants.PluginNamespace);
	private HuntHelperManager HuntHelperManager { get; init; }
	private BearManager BearManager { get; init; }

	private ConfigWindow ConfigWindow { get; init; }
	private MainWindow MainWindow { get; init; }

	public Plugin() {
		Conf = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		Conf.Initialize(PluginInterface);

		HuntHelperManager = new HuntHelperManager();
		BearManager = new BearManager(Utils.PluginFilePath(@"Data\Bear.json"));

		ConfigWindow = new ConfigWindow();
		MainWindow = new MainWindow(HuntHelperManager, BearManager, ConfigWindow);

		PluginInterface.LanguageChanged += OnLanguageChanged;
		OnLanguageChanged(PluginInterface.UiLanguage);

		WindowSystem.AddWindow(ConfigWindow);
		WindowSystem.AddWindow(MainWindow);

		CommandNames.ForEach(commandName =>
			CommandManager.AddHandler(commandName, new CommandInfo(OnCommand) {HelpMessage = "Opens the main window."})
		);

		PluginInterface.UiBuilder.Draw += DrawUi;
		PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
	}

	public void Dispose() {
		PluginInterface.LanguageChanged -= OnLanguageChanged;
		CommandNames.ForEach(commandName => CommandManager.RemoveHandler(commandName));

		WindowSystem.RemoveAllWindows();

		ConfigWindow.Dispose();
		MainWindow.Dispose();

		HuntHelperManager.Dispose();
		
		Conf.Save();
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

	private void DrawConfigUi() {
		ConfigWindow.IsOpen = true;
	}
}
