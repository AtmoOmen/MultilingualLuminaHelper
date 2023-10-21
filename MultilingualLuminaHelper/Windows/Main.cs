using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace MultilingualLuminaHelper.Windows;

public class Main : Window, IDisposable
{
    private Plugin Plugin;

    public Main(Plugin plugin) : base(
        "Multilingual Lumina Helper", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            this.Plugin.DrawConfigUI();
        }
    }
}
