using System.Numerics;
using SolarSystemSim.Core;
using static Raylib_cs.Raylib;
using rlImGui_cs;
using ImGuiNET;
using Raylib_cs;

namespace SolarSystemSim.Celestial;

public class SolarSystem : Scene
{
    public static SolarSystem LoadFromFile(CelestialBody[] bodies, double gravConst, string name)
    {
        var system = new SolarSystem();
        foreach (var body in bodies)
        {
            var newBody = system.AddBody(body.InitialState.Position, body.InitialState.Velocity, body.InitialState.Mass);
            newBody.Name = body.Name;
            newBody.InitialColor = body.InitialColor;
            newBody.CurrentColor = newBody.InitialColor;
            newBody.Size = body.Size;
        }
        system.GravitationalConstant = (float)gravConst;
        system._systemName = name;
        return system;
    }
    
    public float GravitationalConstant = 10.0f;
    public readonly List<CelestialBody> Bodies = [];
    public Camera2D Camera;

    private Vector2[][]? _predictionPoints;
    private CelestialBody? _selectedBody;
    private bool _isSimulating;
    private int _predictionIterations = 50000;
    private bool _showPrediction = true;
    private CancellationTokenSource? _cts;
    private int _currentPredictedIterations;
    private bool _bodySettingsVisible;
    private string _systemName = "Solar System";
    private bool _shouldDrawStars = true;
    private int _starsAmount = 500;
    private int _starsBoundary = 2000;
    private double _delta = 0.0001;
    private int _simulationSpeed = 100;
    private bool _isPredicting;
    private double _predictStartTime;
    private int _predictionSubsteps = 100;

    private Vector2 _selectedBodyPos = Vector2.NegativeInfinity;
    private Vector2 _selectedBodyVel = Vector2.NegativeInfinity;
    private float _selectedBodyMass = float.NegativeInfinity;
    private Vector3 _selectedBodyColor = Vector3.Zero;
    private string _selectedBodyName = "Celestial Body";
    private float _selectedBodySize = 10.0f;
    private int _selectedBodyFeatherSteps = 16;
    private int _selectedBodyTrailLength = 12;
    
    

    public CelestialBody AddBody(Vector2D position, Vector2D velocity, double mass)
    {
        Console.WriteLine($"Adding body at {position}");
        var body = new CelestialBody(position, velocity, mass);
        body.MouseEntered += OnBodyEntered;
        body.MouseExited += OnBodyExited;
        body.Clicked += OnBodyClicked;
        Bodies.Add(body);
        return body;
    }

    private void OnBodyEntered(CelestialBody body)
    {
        if(body.Selected) return;
        body.CurrentColor = Color.White;
    }
    
    private void OnBodyExited(CelestialBody body)
    {
        if(body.Selected) return;
        body.CurrentColor = body.InitialColor;
    }
    
    private void OnBodyClicked(CelestialBody body)
    {
        SelectBody(body);
    }

    private void SelectBody(CelestialBody? body)
    {
        _selectedBody?.Selected = false;
        _selectedBody?.CurrentColor = _selectedBody.InitialColor;
        _selectedBody = null;
        _bodySettingsVisible = false;
        if (body != null)
        {
            if(body.Selected) return;
            _selectedBody?.Selected = false;
            _selectedBody?.CurrentColor = _selectedBody.InitialColor;
            _selectedBody = null;
            
            body.Selected = true;
            body.CurrentColor = Color.Blue;
            
            _selectedBody = body;

            _selectedBodyPos = (Vector2)body.State.Position;
            _selectedBodyVel = (Vector2)body.State.Velocity;
            _selectedBodyMass = (float)body.State.Mass;
            _selectedBodyColor = new Vector3(body.InitialColor.R / 255.0f, body.InitialColor.G / 255.0f,
                body.InitialColor.B / 255.0f);
            _selectedBodySize = body.Size;
            _selectedBodyName = body.Name;
            _selectedBodyFeatherSteps = body.FeatherEffectAmount;
            _selectedBodyTrailLength = body.MaxTrailLength;
            _bodySettingsVisible = true;
        }

    }

    public override void Load()
    {
        Camera.Zoom = 1.0f;
        Camera.Target = Vector2.Zero;
    }

    public override void Update()
    {
        Vector2 mouseScreenPos = GetMousePosition();
        Vector2 mouseWorldPos = GetScreenToWorld2D(mouseScreenPos, Camera);
        Camera.Offset = mouseScreenPos;
        Camera.Target = mouseWorldPos;

        
        // Moving Selected Body
        if (IsKeyDown(KeyboardKey.LeftShift) && IsMouseButtonDown(MouseButton.Left) && _selectedBody != null &&
            !_isSimulating)
        {
            _selectedBodyPos = mouseWorldPos;
            _selectedBody.InitialState.Position = _selectedBodyPos;
            _selectedBody.State = _selectedBody.InitialState;
        }
        
        // Changing Velocity Y
        if (IsKeyDown(KeyboardKey.LeftControl) && _selectedBody != null && !_isSimulating)
        {
            if (IsKeyDown(KeyboardKey.LeftAlt))
            {
                _selectedBodyVel.X += GetMouseWheelMove();
                _selectedBody.InitialState.Velocity = _selectedBodyVel;
            }
            else
            {
                _selectedBodyVel.Y += GetMouseWheelMove();
                _selectedBody.InitialState.Velocity = _selectedBodyVel;
            }
            _selectedBody.State = _selectedBody.InitialState;
        }
        
        // Change Mass
        if (IsKeyDown(KeyboardKey.LeftAlt) && !IsKeyDown(KeyboardKey.LeftControl) && _selectedBody != null &&
            !_isSimulating)
        {
            _selectedBodyMass += GetMouseWheelMove();
            _selectedBody.InitialState.Mass = _selectedBodyMass;
            _selectedBody.State = _selectedBody.InitialState;
        }

        // Focus
        if (IsKeyPressed(KeyboardKey.F))
        {
            Camera.Target = _selectedBody != null ? _selectedBody.State.Position.ToFloat() : Vector2.Zero;
            Camera.Zoom = 3.0f;
        }
        
        // Panning
        if (IsMouseButtonDown(MouseButton.Middle))
        {
            Camera.Target += -GetMouseDelta() / Camera.Zoom;
        }
        
        // Add Body
        if (IsMouseButtonPressed(MouseButton.Left) && IsKeyDown(KeyboardKey.LeftAlt) && !_isSimulating)
        {
            var mousePos = GetMousePosition();
            AddBody(GetScreenToWorld2D(mousePos, Camera), Vector2D.Zero, 10);
        }

        // Remove Body
        if (IsKeyPressed(KeyboardKey.Delete) && _selectedBody != null && !_isSimulating)
        {
            if(_predictionPoints != null)
                Array.Clear(_predictionPoints);
            _currentPredictedIterations = 0;
            _predictionPoints = null;
            Bodies.Remove(_selectedBody);
            SelectBody(null);
        }

        // Zooming
        if (!IsKeyDown(KeyboardKey.LeftControl) && !IsKeyDown(KeyboardKey.LeftAlt))
        {
            float mouseScroll = GetMouseWheelMove();
            Camera.Zoom = MathF.Exp(MathF.Log(Camera.Zoom) + mouseScroll * 0.1f);
            Camera.Zoom = Math.Clamp(Camera.Zoom, 0.5f, 3.0f);
        }

        // Deselect
        if (IsMouseButtonPressed(MouseButton.Right))
        {
            SelectBody(null);
        }
        
        foreach (var body in Bodies)
        {
            body.CheckSelectable(Camera);
        }

        if (_isSimulating)
            Simulate();
    }
    
    private void DrawGui()
    {
        rlImGui.Begin();

        DrawSimulationSettingsGui();

        if(!_isSimulating)
        {
            DrawBodyPropertiesGui();

            DrawSystemSettingsGui();
        }
        
        rlImGui.End();
    }

    private void DrawSimulationSettingsGui()
    {
        ImGui.SetNextWindowPos(new Vector2(4, 4), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(300, 170), ImGuiCond.FirstUseEver);
        ImGui.Begin("Simulation Settings");
        ImGui.PushItemWidth(100f);
        if (_isSimulating)
        {
            if (ImGui.Button("Stop Simulation"))
                StopSimulation();
            ImGui.SliderInt("Simulation Speed", ref _simulationSpeed, 1, 1000);
        }
        else
        {
            if (ImGui.Button("Start Simulation"))
                StartSimulation();
            ImGui.InputDouble("Delta (Lower for accuracy)", ref _delta);
            
            ImGui.SeparatorText("Prediction");
            if (_isPredicting)
            {
                if (ImGui.Button("Stop Predicting"))
                    StopPrediction();
                ImGui.Text($"Steps: {_currentPredictedIterations}/{_predictionIterations}");
                double timeElapsed = GetTime() - _predictStartTime;
                if (timeElapsed != 0)
                {
                    double stepsPerSecond = _currentPredictedIterations / timeElapsed;
                    int stepsRemaining = _predictionIterations - _currentPredictedIterations;
                    ImGui.Text($"Time Remaining: {Math.Round(stepsRemaining / stepsPerSecond, 2)} sec ({Math.Round(stepsPerSecond)} steps/sec)");
                }
            }
            else
            {
                if (ImGui.Button("Start Predicting"))
                    StartPrediction(_predictionIterations, _predictionSubsteps);
                ImGui.SameLine();
                if(ImGui.Button("Clear Prediction") && _predictionPoints != null)
                {
                    _currentPredictedIterations = 0;
                    Array.Clear(_predictionPoints);
                }
                if (ImGui.DragInt("Iterations", ref _predictionIterations))
                    _predictionIterations = Math.Max(0, _predictionIterations);
                if (ImGui.DragInt("Substeps", ref _predictionSubsteps))
                    _predictionSubsteps = Math.Max(0, _predictionSubsteps);
            }
        }
        ImGui.End();
    }

    private void DrawSystemSettingsGui()
    {
        ImGui.SetNextWindowPos(new Vector2(4, 180), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(270, 280), ImGuiCond.FirstUseEver);
        ImGui.Begin("System Settings");
        ImGui.PushItemWidth(100f);
        ImGui.Text($"FPS: {GetFPS()}");
        if(ImGui.Button("Back To Menu"))
        {
            Program.LoadScene(new MainMenu());
            StopPrediction();
        }
        ImGui.SeparatorText("System");
        ImGui.DragFloat("Gravitational Constant", ref GravitationalConstant);
        ImGui.SeparatorText("Saving");
        ImGui.InputText("System Name", ref _systemName, 32);
        if (ImGui.Button("Save System"))
            _ = SaveManager.SaveSolarSystem(Bodies.ToArray(), GravitationalConstant, $"{_systemName}.json");
        ImGui.SeparatorText("Stars");
        ImGui.Checkbox("Draw Stars", ref _shouldDrawStars);
        if (_shouldDrawStars)
        {
            ImGui.DragInt("Stars Amount", ref _starsAmount, 5, 0, int.MaxValue);
            ImGui.DragInt("Stars Render Boundary", ref _starsBoundary, 10, 0, int.MaxValue);
        }
        ImGui.End();
    }
    
    private void DrawBodyPropertiesGui()
    {
        if (_bodySettingsVisible)
        {
            ImGui.SetNextWindowPos(new Vector2(310, 4), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(240, 390), ImGuiCond.FirstUseEver);
            ImGui.Begin("Body Settings", ref _bodySettingsVisible);
            ImGui.PushItemWidth(150f);
            ImGui.SeparatorText("Simulation");
            if (ImGui.DragFloat2("Position", ref _selectedBodyPos, 0.1f))
                _selectedBody?.InitialState.Position = _selectedBodyPos;
            if (ImGui.DragFloat2("Velocity", ref _selectedBodyVel, 0.1f))
                _selectedBody?.InitialState.Velocity = _selectedBodyVel;
            if (ImGui.DragFloat("Mass", ref _selectedBodyMass, 0.1f, 0, float.MaxValue))
                _selectedBody?.InitialState.Mass = _selectedBodyMass;
            ImGui.SeparatorText("Visual");
            if(ImGui.ColorPicker3("Color", ref _selectedBodyColor))
            {
                _selectedBody?.InitialColor = new Color(_selectedBodyColor.X, _selectedBodyColor.Y,
                    _selectedBodyColor.Z);
                _selectedBody?.CurrentColor = _selectedBody.InitialColor;
            }
            
            _selectedBody?.State = _selectedBody.InitialState;
            
            if (ImGui.DragFloat("Size", ref _selectedBodySize, 0.1f, 0f, 200f))
                _selectedBody?.Size = _selectedBodySize;
            
            if (ImGui.InputText("Name", ref _selectedBodyName, 32))
                _selectedBody?.Name = _selectedBodyName;
            
            if (ImGui.InputInt("Feather Amount (Blur)", ref _selectedBodyFeatherSteps))
            {
                _selectedBodyFeatherSteps = Math.Clamp(_selectedBodyFeatherSteps, 1, 64);
                _selectedBody?.FeatherEffectAmount = _selectedBodyFeatherSteps;
            }
            if(ImGui.InputInt("Trail Amount (Motion Blur)", ref _selectedBodyTrailLength))
            {
                _selectedBodyTrailLength = Math.Clamp(_selectedBodyTrailLength, 1, 64);
                _selectedBody?.MaxTrailLength = _selectedBodyTrailLength;
            }
                
            ImGui.End();
        }
        else
        {
            SelectBody(null);
        }
    }

    private void DrawStars()
    {
        SetRandomSeed(0);
        for (int i = 0; i < _starsAmount; i++)
        {
            var random = new Vector2(GetRandomValue(-_starsBoundary, _starsBoundary), GetRandomValue(-_starsBoundary, _starsBoundary));
            DrawCircle((int)random.X, (int)random.Y, GetRandomValue(10, 20) * 0.1f, Color.White);
        }
    }
    
    public override void Draw()
    {
        BeginMode2D(Camera);
        if (_shouldDrawStars)
            DrawStars();
        
        foreach (var body in Bodies)
        {
            body.Draw();
        }
        
        if (_predictionPoints != null && _showPrediction)
        {
            for (var b = 0; b < _predictionPoints.Length; b++)
            {
                var color = Bodies[b].CurrentColor;
                DrawLineStrip(_predictionPoints[b], _currentPredictedIterations + 1, color);
            }
        }
        EndMode2D();

        DrawGui();
    }

    private void StartSimulation()
    {
        StopPrediction();
        _isSimulating = true;
    }
    
    private void StopSimulation()
    {
        _isSimulating = false;
        foreach (var body in Bodies)
        {
            body.State = body.InitialState;
        }

        _showPrediction = true;
    }

    private void Simulate()
    {
        var currentStates = new Simulation.StateVectors[Bodies.Count];
        for (var b = 0; b < Bodies.Count; b++)
        {
            currentStates[b] = Bodies[b].State;
        }
        
        for (int step = 0; step < _simulationSpeed; step++)
        {
            currentStates = Simulation.GetNextStepRk4(currentStates, GravitationalConstant, _delta);
        }

        for (int b = 0; b < Bodies.Count; b++)
        {
            Bodies[b].State = currentStates[b];
        }
    }

    private void StartPrediction(int iterations, int substeps)
    {
        _ = Task.Run(() => Predict(iterations, substeps));
        _isPredicting = true;
        _predictStartTime = GetTime();
    }
    
    private void StopPrediction()
    {
        _cts?.Cancel();
        _isPredicting = false;
        _predictStartTime = 0.0;
    }
    
    private async Task Predict(int iterations, int substeps)
    {
        if (_cts != null)
        {
            await _cts?.CancelAsync()!;
            _cts?.Dispose();
        }
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        
        _currentPredictedIterations = 0;
        _predictionPoints = new Vector2[Bodies.Count][];
        var currentStates = new Simulation.StateVectors[Bodies.Count];
        
        for (var b = 0; b < Bodies.Count; b++)
        {
            var body = Bodies[b];
            currentStates[b] = body.InitialState;
            
            _predictionPoints[b] = new Vector2[iterations + 1];
            _predictionPoints[b][0] = (Vector2)body.State.Position;
        }
        
        try
        {
            for (int i = 1; i <= iterations; i++)
            {
                if (token.IsCancellationRequested) return;
                
                for (int s = 0; s < substeps; s++)
                {
                    currentStates = Simulation.GetNextStepRk4(currentStates, GravitationalConstant, _delta);
                }
                
                for (int b = 0; b < currentStates.Length; b++)
                {
                    _predictionPoints[b][i] = (Vector2)currentStates[b].Position;
                }

                _currentPredictedIterations = i;
            }

            Console.WriteLine($"Predicted {Bodies.Count} Bodies with {iterations} iterations");
            _isPredicting = false;
            _predictStartTime = 0.0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Prediction stopped {e.Message}");
        }
    }
}