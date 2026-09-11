using System.Numerics;
using Raylib_cs;
using SolarSystemSim.Core;
using static Raylib_cs.Raylib;

namespace SolarSystemSim.Celestial;

public class CelestialBody : ISelectable
{
    public event Action<CelestialBody>? MouseEntered;
    public event Action<CelestialBody>? MouseExited;
    public event Action<CelestialBody>? Clicked;

    public bool Selected = false;
    
    private bool _isHovered;
    
    public CelestialBody()
    {
        State = new Simulation.StateVectors(Vector2D.Zero, Vector2D.Zero, 1.0f);
        InitialState = State;
    }
    
    public CelestialBody(Vector2D initialPosition, Vector2D initialVelocity, double mass, float size = 10.0f) : this()
    {
        State = new Simulation.StateVectors(initialPosition, initialVelocity, mass);
        InitialState = State;
        Size = size;
    }
    
    public Simulation.StateVectors InitialState;
    public Simulation.StateVectors State;
    public float Size;
    public Color InitialColor = Color.Gray;
    public Color CurrentColor = Color.Gray;
    public string Name = "Celestial Body";

    private Queue<Vector2> _positionHistory = new();
    public int MaxTrailLength = 12;
    public int FeatherEffectAmount = 16;
    
    public void Draw()
    {
        _positionHistory.Enqueue(State.Position.ToFloat());

        while (_positionHistory.Count > MaxTrailLength)
        {
            _positionHistory.Dequeue();
        }
        
        BeginBlendMode(BlendMode.Alpha);

        int queueIdx = 0;
        foreach (var pos in _positionHistory)
        {
            float trailProgress = (queueIdx + 1.0f) / _positionHistory.Count;
            
            for (int i = 0; i < FeatherEffectAmount; i++)
            {
                float normalized = (i + 1.0f) / FeatherEffectAmount;
                byte fadedAlpha = (byte)(normalized * (255.0f / FeatherEffectAmount) * trailProgress);
                
                DrawCircleV(pos, normalized * Size, CurrentColor with { A = fadedAlpha });
            }

            queueIdx++;
        }
        
        EndBlendMode();
    }

    private bool IsHovering(Camera2D camera)
    {
        var mousePos = GetScreenToWorld2D(GetMousePosition(), camera);
        return Vector2.DistanceSquared(mousePos, State.Position.ToFloat()) < Size * Size;
    }

    public void CheckSelectable(Camera2D camera)
    {
        if(IsHovering(camera) && !_isHovered)
        {
            MouseEntered?.Invoke(this);
            _isHovered = true;
        }
        
        if(!IsHovering(camera) && _isHovered)
        {
            MouseExited?.Invoke(this);
            _isHovered = false;
        }

        if (_isHovered && IsMouseButtonPressed(MouseButton.Left))
        {
            Clicked?.Invoke(this);
        }
    }
}