using System.Numerics;
using SolarSystemSim.Celestial;
using SolarSystemSim.Core;
using ZeroElectric.Vinculum;
using static ZeroElectric.Vinculum.Raylib;

namespace SolarSystemSim;

public static class Program
{
    public static Vector2 WindowSize { get; } = new(1280, 720);
    public static Scene? LoadedScene { get; set; }
    
    public static int Frame { get; private set; } = 0;
    public static double Delta { get; private set; } = 0;
    public const double FixedDelta = 0.0001f;
    
    public static void Main(string[] args)
    {
        SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
        InitWindow((int)WindowSize.X, (int)WindowSize.Y, "Feast Your Eyes");
        SetTargetFPS(120);
        
        LoadScene(new MainMenu());

        while (!WindowShouldClose())
        {
            LoadedScene?.Update();
            
            BeginDrawing();
            ClearBackground(BLACK);

            LoadedScene?.Draw();
            
            // DrawFPS(GetScreenWidth() - 250, 0);
            EndDrawing();

            Frame++;
            Delta = GetFrameTime();
        }
        
        CloseWindow();
    }

    public static void LoadScene(Scene scene)
    {
        LoadedScene?.Unload();
        LoadedScene = scene;
        LoadedScene.Load();
    }
}