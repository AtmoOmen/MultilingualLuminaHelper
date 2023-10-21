using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using MultilingualLuminaHelper.Windows;

namespace MultilingualLuminaHelper
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "MultilingualLuminaHelper";
        private const string CommandName = "/mlh";
        public static Plugin Instance = null!;

        private DalamudPluginInterface PluginInterface { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("MultilingualLuminaHelper");
        private Main Main { get; init; }

        public Plugin(DalamudPluginInterface pluginInterface)
        {
            Instance = this;
            PluginInterface = pluginInterface;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            Service.Initialize(pluginInterface);

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the main window."
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Main = new Main(this);
            WindowSystem.AddWindow(Main);
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();

            Main.Dispose();

            Service.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            Main.IsOpen = !Main.IsOpen;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            Main.IsOpen = !Main.IsOpen;
        }
    }
}
