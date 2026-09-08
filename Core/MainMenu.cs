using System.Numerics;
using SolarSystemSim.Celestial;
using static ZeroElectric.Vinculum.Raylib;
using static ZeroElectric.Vinculum.RayGui;

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
        if (Program.Frame % 120 == 0)
            FindSaveFiles();
        
        DrawText("Load Solar System:", GetScreenWidth() * 0.01f, GetScreenHeight() * 0.01f, 25.0f, WHITE);
        if(GuiButton(GuiHelper.NewRectangle(new Vector2(0.01f, 0.05f), new Vector2(0.2f, 0.1f)), "Create New System") == 1)
            Program.LoadScene(new SolarSystem());

        ShowSaves();
    }

    private void ShowSaves()
    {
        for (var s = 0; s < _saveFiles.Count; s++)
        {
            string path = _saveFiles[s];
            string name = path.Replace("Saves\\", "");
            if (GuiButton(
                    GuiHelper.NewRectangle(new Vector2(0.01f, 0.06f * s + 0.11f), new Vector2(0.2f, 0.06f * s + 0.16f)),
                    $"Load {name}") == 1)
            {
                var data = SaveManager.LoadSolarSystem(path);
                Program.LoadScene(SolarSystem.LoadFromFile(data.Bodies, data.GravConst));
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