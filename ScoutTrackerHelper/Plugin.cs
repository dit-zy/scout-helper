using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ScoutTrackerHelper.Windows;
using System.IO;

namespace ScoutTrackerHelper;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin {

	public const string Name = "Scout Tracker Helper";

	private const string CommandName = "/sth";

	public readonly WindowSystem WindowSystem = new("ScoutTrackerHelper");

	private DalamudPluginInterface PluginInterface { get; init; }
	private ICommandManager CommandManager { get; init; }
	public Configuration Configuration { get; init; }

	private ConfigWindow ConfigWindow { get; init; }
	private MainWindow MainWindow { get; init; }

	public Plugin(
		[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
		[RequiredVersion("1.0")] ICommandManager commandManager
	) {
		PluginInterface = pluginInterface;
		CommandManager = commandManager;

		Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
		Configuration.Initialize(PluginInterface);

		// you might normally want to embed resources and load them from the manifest stream
		var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
		var goatImage = PluginInterface.UiBuilder.LoadImage(imagePath);

		ConfigWindow = new ConfigWindow(this);
		MainWindow = new MainWindow(this, goatImage);

		WindowSystem.AddWindow(ConfigWindow);
		WindowSystem.AddWindow(MainWindow);

		CommandManager.AddHandler(
			CommandName,
			new CommandInfo(OnCommand) {
				HelpMessage = "A useful message to display in /xlhelp",
			}
		);

		PluginInterface.UiBuilder.Draw += DrawUi;
		PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUi;
	}

	public void Dispose() {
		WindowSystem.RemoveAllWindows();

		ConfigWindow.Dispose();
		MainWindow.Dispose();

		CommandManager.RemoveHandler(CommandName);
	}

	private void OnCommand(string command, string args) {
		// in response to the slash command, just display our main ui
		MainWindow.IsOpen = true;
	}

	private void DrawUi() {
		WindowSystem.Draw();
	}

	public void DrawConfigUi() {
		ConfigWindow.IsOpen = true;
	}
}
