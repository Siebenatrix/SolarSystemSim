using System.Numerics;
using Raylib_cs;
using SolarSystemSim.Celestial;
using SolarSystemSim.Core;
using static Raylib_cs.Raylib;
using rlImGui_cs;

namespace SolarSystemSim;

public static class Program
{
    public static Vector2 WindowSize { get; } = new(1280, 720);
    public static Scene? LoadedScene { get; set; }
    
    public static int Frame { get; private set; }

    public static void Main(string[] args)
    {
        SetConfigFlags(ConfigFlags.ResizableWindow);
        InitWindow((int)WindowSize.X, (int)WindowSize.Y, "Solar Sim");
        SetTargetFPS(120);
        
        LoadScene(new MainMenu());

        rlImGui.Setup();

        while (!WindowShouldClose())
        {
            LoadedScene?.Update();
            
            BeginDrawing();
            ClearBackground(Color.Black);

            LoadedScene?.Draw();
            
            EndDrawing();

            Frame++;
            GetFrameTime();
        }
        
        rlImGui.Shutdown();
        CloseWindow();
    }

    public static void LoadScene(Scene scene)
    {
        LoadedScene?.Unload();
        LoadedScene = scene;
        LoadedScene.Load();
    }
}