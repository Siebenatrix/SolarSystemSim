using System.Numerics;
using SolarSystemSim.Celestial;
using static Raylib_cs.Raylib;
using ImGuiNET;
using rlImGui_cs;

namespace SolarSystemSim.Core;

public class MainMenu: Scene
{
    private List<string> _saveFiles = [];

    public override void Load()
    {
        FindSaveFiles();
    }

    public override void Draw()
    {
        rlImGui.Begin();
        if (Program.Frame % 120 == 0)
            FindSaveFiles();

        ImGui.SetNextWindowPos(new Vector2(8, 8), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(300, 700), ImGuiCond.FirstUseEver);
        ImGui.Begin("Load Solar System:");
        if (ImGui.Button("Create New System"))
            Program.LoadScene(new SolarSystem());

        ShowSaves();
        ImGui.End();
        rlImGui.End();
    }

    private void ShowSaves()
    {
        for (var s = 0; s < _saveFiles.Count; s++)
        {
            string path = _saveFiles[s];
            string name = path.Replace("Saves\\", "");
            if (ImGui.Button($"Load {name}"))
            {
                var data = SaveManager.LoadSolarSystem(path);
                Program.LoadScene(SolarSystem.LoadFromFile(data.Bodies, data.GravConst, name.Replace(".json", "")));
            }
        }
    }

    private void FindSaveFiles()
    {
        if (!Directory.Exists("Saves"))
            Directory.CreateDirectory("Saves");
        
        _saveFiles = new List<string>(Directory.EnumerateFiles("Saves"));
    }
}