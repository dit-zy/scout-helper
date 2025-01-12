using System;
using System.Collections.Generic;
using System.Globalization;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using ScoutHelper.Config;
using ScoutHelper.Localization;
using ScoutHelper.Managers;
using ScoutHelper.Utils;
using ScoutHelper.Windows;
using XIVHuntUtils.Managers;

namespace ScoutHelper;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin {
	public const string Name = Constants.PluginName;

	private static readonly List<string> CommandNames = ["/scouth", "/sch"];

	private readonly IPluginLog _log;

	private readonly WindowSystem _windowSystem = new WindowSystem(Constants.PluginNamespace);
	private readonly ConfigWindow _configWindow;
	private readonly MainWindow _mainWindow;
	private readonly Action _dispose;

	public Plugin(
			IFramework framework,
			IDalamudPluginInterface pluginInterface,
			IPluginLog log,
			IChatGui chatGui,
			ICommandManager commandManager,
			IClientState clientState,
			IDataManager dataManager,
			IObjectTable objectTable
	) {
		_log = log;

		var conf = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		conf.Initialize(_log, pluginInterface);

		var serviceProvider = new ServiceCollection()
				.AddSingleton(framework)
				.AddSingleton(pluginInterface)
				.AddSingleton(_log)
				.AddSingleton(chatGui)
				.AddSingleton(commandManager)
				.AddSingleton(clientState)
				.AddSingleton(dataManager)
				.AddSingleton(objectTable)
				.AddSingleton(conf)
				.AddSingleton(
						new ScoutHelperOptions(
								pluginInterface.PluginFilePath(Constants.BearDataFile),
								pluginInterface.PluginFilePath(Constants.SirenDataFile),
								pluginInterface.PluginFilePath(Constants.TurtleDataFile)
						)
				)
				.AddSingleton<MobManager>()
				.AddSingleton<TerritoryManager>()
				.AddSingleton<HuntHelperManager>()
				.AddSingleton<HuntMarkManager>()
				.AddSingleton<BearManager>()
				.AddSingleton<SirenManager>()
				.AddSingleton<TurtleManager>()
				.AddSingleton<ConfigWindow>()
				.AddSingleton<MainWindow>()
				.AddSingleton<InitializationManager>()
				.BuildServiceProvider();

		var initManager = serviceProvider.GetRequiredService<InitializationManager>();
		initManager.InitializeNecessaryComponents();

		pluginInterface.LanguageChanged += OnLanguageChanged;
		OnLanguageChanged(pluginInterface.UiLanguage);

		_configWindow = serviceProvider.GetRequiredService<ConfigWindow>();
		_mainWindow = serviceProvider.GetRequiredService<MainWindow>();
		_windowSystem.AddWindow(_configWindow);
		_windowSystem.AddWindow(_mainWindow);

		CommandNames.ForEach(
				commandName =>
						commandManager.AddHandler(commandName, new CommandInfo(OnCommand) { HelpMessage = "Opens the main window." })
		);

		pluginInterface.UiBuilder.Draw += DrawUi;
		pluginInterface.UiBuilder.OpenMainUi += DrawMainUi;
		pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;

		// create the dispose action here, so fields don't need to be made just for disposal
		_dispose = () => {
			pluginInterface.LanguageChanged -= OnLanguageChanged;
			CommandNames.ForEach(commandName => commandManager.RemoveHandler(commandName));

			_windowSystem.RemoveAllWindows();

			conf.Save();

			serviceProvider.Dispose();
		};
	}

	public void Dispose() => _dispose.Invoke();

	private void OnLanguageChanged(string languageCode) {
		try {
			_log.Information($"Loading localization for {languageCode}");
			Strings.Culture = CultureInfo.GetCultureInfo(languageCode);
		} catch (Exception e) {
			_log.Error(e, "Unable to load localization for language code: {0}", languageCode);
		}
	}

	private void OnCommand(string command, string args) {
		_mainWindow.IsOpen = true;
	}

	private void DrawUi() {
		_windowSystem.Draw();
	}

	private void DrawMainUi() {
		_mainWindow.IsOpen = true;
	}

	private void DrawConfigUi() {
		_configWindow.IsOpen = true;
	}
}
