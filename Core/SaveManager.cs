using System.Text.Json;
using Raylib_cs;
using SolarSystemSim.Celestial;

namespace SolarSystemSim.Core;

public static class SaveManager
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };
    private record SaveData(double GravConst, BodySaveData[] Bodies);

    private record BodySaveData(
        string Name,
        double PositionX,
        double PositionY,
        double VelocityX,
        double VelocityY,
        float Size,
        double Mass,
        byte ColorR,
        byte ColorG,
        byte ColorB,
        byte ColorA
    );
    
    public static async Task SaveSolarSystem(CelestialBody[] bodies, double gravConst, string fileName)
    {
        string path = Path.Combine("Saves", fileName);
        if (!Directory.Exists("Saves"))
            Directory.CreateDirectory("Saves");

        var bodyData = bodies.Select(b => new BodySaveData(
            b.Name,
            b.InitialState.Position.X,
            b.InitialState.Position.Y,
            b.InitialState.Velocity.X,
            b.InitialState.Velocity.Y,
            b.Size,
            b.InitialState.Mass,
            b.InitialColor.R, b.InitialColor.G, b.InitialColor.B, b.InitialColor.A
        )).ToArray();

        var saveData = new SaveData(gravConst, bodyData);
        
        await using var createStream = File.Create(path);
        await JsonSerializer.SerializeAsync(createStream, saveData, Options);
    }

    public static (CelestialBody[] Bodies, double GravConst) LoadSolarSystem(string fileName)
    {
        string path = fileName;
        if(fileName.StartsWith("Saves/"))
            path = Path.Combine("Saves", fileName);

        using var openStream = File.OpenRead(path);
        var saveData = JsonSerializer.Deserialize<SaveData>(openStream);

        if (saveData == null) return ([], 10f);

        CelestialBody[] bodies = saveData.Bodies.Select(data =>
        {
            var state = new Simulation.StateVectors
            {
                Position = new Vector2D(data.PositionX, data.PositionY),
                Velocity = new Vector2D(data.VelocityX, data.VelocityY),
                Mass = data.Mass
            };

            return new CelestialBody
            {
                Name = data.Name,
                InitialState = state,
                State = state,
                Size = data.Size,
                InitialColor = new Color(data.ColorR, data.ColorG, data.ColorB, data.ColorA)
            };
        }).ToArray();

        return (bodies, saveData.GravConst);
    }
}