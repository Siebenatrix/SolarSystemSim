using System.Numerics;
using SolarSystemSim.Core;
using ZeroElectric.Vinculum;
using static ZeroElectric.Vinculum.Raylib;

namespace SolarSystemSim.Celestial;

public class CelestialBody : ISelectable
{
    private const int FeatherAmount = 16;
    
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
    public Color InitialColor = GRAY;
    public Color CurrentColor = GRAY;
    public string Name = "Celestial Body";

    private Queue<Vector2> _positionHistory = new();
    private int _maxTrailLength = 12;
    
    public void Draw()
    {
        _positionHistory.Enqueue(State.Position.ToFloat());

        if (_positionHistory.Count > _maxTrailLength)
        {
            _positionHistory.Dequeue();
        }
        
        BeginBlendMode(BlendMode.BLEND_ALPHA);

        int queueIdx = 0;
        foreach (var pos in _positionHistory)
        {
            float trailProgress = (queueIdx + 1.0f) / _positionHistory.Count;
            
            for (int i = 0; i < FeatherAmount; i++)
            {
                float normalized = (i + 1.0f) / FeatherAmount;
                byte fadedAlpha = (byte)(normalized * 25 * trailProgress);
                
                DrawCircleV(pos, normalized * Size, CurrentColor with { a = fadedAlpha });
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

        if (_isHovered && IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            Clicked?.Invoke(this);
        }
    }
}