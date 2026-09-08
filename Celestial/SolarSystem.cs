using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using SolarSystemSim.Core;
using ZeroElectric.Vinculum;
using static ZeroElectric.Vinculum.RayGui;
using static ZeroElectric.Vinculum.Raylib;

namespace SolarSystemSim.Celestial;

public class SolarSystem : Scene
{
    public static SolarSystem LoadFromFile(CelestialBody[] bodies, double gravConst)
    {
        var system = new SolarSystem();
        foreach (var body in bodies)
        {
            var newBody = system.AddBody(body.InitialState.Position, body.InitialState.Velocity, body.InitialState.Mass);
            newBody.Name = body.Name;
            newBody.InitialColor = body.InitialColor;
            newBody.CurrentColor = newBody.InitialColor;
        }
        system.GravitationalConstant = gravConst;
        system._gravitationalConstantBuffer = Encoding.UTF8.GetBytes(gravConst.ToString(CultureInfo.InvariantCulture) + " ");
        return system;
    }
    
    public double GravitationalConstant = 10.0f;
    public readonly List<CelestialBody> Bodies = [];
    public Camera2D Camera;

    private Vector2[][]? _predictionPoints;
    private CelestialBody? _selectedBody;
    private bool _isSimulating;
    private int _predictionIterations = 50000;
    private bool _showPrediction = true;
    private CancellationTokenSource? _cts;
    private int _currentPredictedIterations;
    
    // GUI
    private bool _iterationEditing;
    private bool _gravConstEditing;
    private bool _saveModalShown;
    private readonly byte[] _saveNameBuffer = new byte[32];
    
    private bool _positionXEditing;
    private bool _positionYEditing;
    private bool _velocityXEditing;
    private bool _velocityYEditing;
    private bool _massEditing;
    private bool _nameEditing;
    
    private byte[] _gravitationalConstantBuffer = new byte[8];

    private readonly byte[] _positionXBuffer = new byte[8];
    private double _positionX;
    private readonly byte[] _positionYBuffer = new byte[8];
    private double _positionY;
    private readonly byte[] _velocityXBuffer = new byte[8];
    private double _velocityX;
    private readonly byte[] _velocityYBuffer = new byte[8];
    private double _velocityY;
    private readonly byte[] _massBuffer = new byte[8];
    private double _mass = 10;
    private float _simulationsSpeed = 500.0f;
    private readonly byte[] _nameBuffer = new byte[32];

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
        body.CurrentColor = WHITE;
    }
    
    private void OnBodyExited(CelestialBody body)
    {
        if(body.Selected) return;
        body.CurrentColor = body.InitialColor;
    }
    
    private void OnBodyClicked(CelestialBody body)
    {
        if(body.Selected) return;
        _selectedBody?.Selected = false;
        _selectedBody?.CurrentColor = _selectedBody.InitialColor;
        _selectedBody = null;
        
        body.Selected = true;
        body.CurrentColor = BLUE;
        
        _positionX = body.State.Position.X;
        Array.Clear(_positionXBuffer);
        byte[] tempBytes = Encoding.UTF8.GetBytes(_positionX.ToString(CultureInfo.InvariantCulture));
        Buffer.BlockCopy(tempBytes, 0, _positionXBuffer, 0, Math.Min(tempBytes.Length, _positionXBuffer.Length - 1));
        
        _positionY = body.State.Position.Y;
        Array.Clear(_positionYBuffer);
        tempBytes = Encoding.UTF8.GetBytes(_positionY.ToString(CultureInfo.InvariantCulture));
        Buffer.BlockCopy(tempBytes, 0, _positionYBuffer, 0, Math.Min(tempBytes.Length, _positionYBuffer.Length - 1));
        
        _velocityX = body.State.Velocity.X;
        Array.Clear(_velocityXBuffer);
        tempBytes = Encoding.UTF8.GetBytes(_velocityX.ToString(CultureInfo.InvariantCulture));
        Buffer.BlockCopy(tempBytes, 0, _velocityXBuffer, 0, Math.Min(tempBytes.Length, _velocityXBuffer.Length - 1));
        
        _velocityY = body.State.Velocity.Y;
        Array.Clear(_velocityYBuffer);
        tempBytes = Encoding.UTF8.GetBytes(_velocityY.ToString(CultureInfo.InvariantCulture));
        Buffer.BlockCopy(tempBytes, 0, _velocityYBuffer, 0, Math.Min(tempBytes.Length, _velocityYBuffer.Length - 1));
        
        _mass = body.State.Mass;
        Array.Clear(_massBuffer);
        tempBytes = Encoding.UTF8.GetBytes(_mass.ToString(CultureInfo.InvariantCulture));
        Buffer.BlockCopy(tempBytes, 0, _massBuffer, 0, Math.Min(tempBytes.Length, _massBuffer.Length - 1));
        
        Array.Clear(_nameBuffer);
        tempBytes = Encoding.UTF8.GetBytes(body.Name.ToString(CultureInfo.InvariantCulture));
        Buffer.BlockCopy(tempBytes, 0, _nameBuffer, 0, Math.Min(tempBytes.Length, _nameBuffer.Length - 1));
        
        _selectedBody = body;
    }

    public override void Load()
    {
        Camera.zoom = 1.0f;
        Camera.target = Vector2.Zero;
    }

    public override void Update()
    {
        Vector2 mouseScreenPos = GetMousePosition();
        Vector2 mouseWorldPos = GetScreenToWorld2D(mouseScreenPos, Camera);
        Camera.offset = mouseScreenPos;
        Camera.target = mouseWorldPos;

        if (IsKeyPressed(KeyboardKey.KEY_F) && _selectedBody != null)
        {
            Camera.target = _selectedBody.State.Position.ToFloat();
            Camera.zoom = 3.0f;
        }
        
        if (IsMouseButtonDown(MouseButton.MOUSE_BUTTON_MIDDLE))
        {
            Camera.target += -GetMouseDelta() / Camera.zoom;
        }
        
        if (IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT) && IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL))
        {
            var mousePos = GetMousePosition();
            AddBody(GetScreenToWorld2D(mousePos, Camera), Vector2D.Zero, 10);
        }

        if (IsKeyPressed(KeyboardKey.KEY_DELETE) && _selectedBody != null && !_isSimulating)
        {
            if(_predictionPoints != null)
                Array.Clear(_predictionPoints);
            _currentPredictedIterations = 0;
            _predictionPoints = null;
            Bodies.Remove(_selectedBody);
            _selectedBody = null;
        }

        if (!IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) && !IsKeyDown(KeyboardKey.KEY_LEFT_ALT))
        {
            float mouseScroll = GetMouseWheelMove();
            Camera.zoom = MathF.Exp(MathF.Log(Camera.zoom) + mouseScroll * 0.1f);
            Camera.zoom = Math.Clamp(Camera.zoom, 0.5f, 3.0f);
        }

        if (IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_RIGHT))
        {
            _selectedBody?.Selected = false;
            _selectedBody?.CurrentColor = _selectedBody.InitialColor;
            _selectedBody = null;
        }
        
        foreach (var body in Bodies)
        {
            body.CheckSelectable(Camera);
        }
        
        if(_isSimulating)
            Simulate();
    }
    
    private void DrawGui()
    {
        if (_isSimulating)
        {
            // Simulation Speed
            GuiLabel(GuiHelper.NewRectangle(new Vector2(0.01f, 0.75f), new Vector2(0.15f, 0.77f)), "Simulation Speed");
            GuiSlider(GuiHelper.NewRectangle(new Vector2(0.01f, 0.77f), new Vector2(0.15f, 0.79f)), null, null,
                ref _simulationsSpeed, 0, 2000);
            
            // Toggle Prediction
            if (GuiButton(GuiHelper.NewRectangle(new Vector2(0.01f, 0.8f), new Vector2(0.15f, 0.89f)),
                    "Toggle Prediction") == 1)
                _showPrediction = !_showPrediction;
            
            // Stop Simulation
            if (GuiButton(GuiHelper.NewRectangle(new Vector2(0.01f, 0.9f), new Vector2(0.15f, 0.99f)), "Stop Simulation") == 1)
                StopSimulation();
        }
        else
        {
            // Back Button
            if (GuiButton(GuiHelper.NewRectangle(new Vector2(0.01f), new Vector2(0.09f, 0.05f)), "Back") == 1)
            {
                Program.LoadScene(new MainMenu());
                _cts?.Cancel();
            }
            
            // Save Button
            if (GuiButton(GuiHelper.NewRectangle(new Vector2(0.01f, 0.06f), new Vector2(0.09f, 0.1f)), "Save") == 1)
            {
                _saveModalShown = true;
                Array.Clear(_saveNameBuffer);
            }
            
            // Save Modal
            bool cache = _saveModalShown;
            if (_saveModalShown)
            {
                GuiEnable();
                int textInput;

                byte[] titleBuffer = "Save System"u8.ToArray();
                byte[] messageBuffer = "What would you like to call this solar system?"u8.ToArray();
                byte[] buttonBuffer = "Save"u8.ToArray();
                unsafe
                {
                    fixed(byte* titlePtr = titleBuffer)
                    fixed(byte* messagePtr = messageBuffer)
                    fixed(byte* buttonPtr = buttonBuffer)
                    fixed (byte* ptr = _saveNameBuffer)
                    {
                        textInput = GuiTextInputBox(GuiHelper.NewRectangle(new Vector2(0.35f), new Vector2(0.65f)),
                            (sbyte*)titlePtr,
                            (sbyte*)messagePtr,
                            (sbyte*)buttonPtr,
                            (sbyte*)ptr,
                            _saveNameBuffer.Length,
                            null
                        );

                    }
                }

                switch (textInput)
                {
                    case 0:
                        _saveModalShown = false;
                        break;
                    case 1:
                    {
                        string fileName = Encoding.UTF8.GetString(_saveNameBuffer).TrimEnd('\0').Trim();
                        if (!string.IsNullOrEmpty(Encoding.UTF8.GetString(_saveNameBuffer).TrimEnd('\0').Trim()))
                        {
                            _saveModalShown = false;
                            _ = SaveManager.SaveSolarSystem(Bodies.ToArray(), GravitationalConstant, $"{fileName}.json");
                        }

                        break;
                    }
                }

                GuiDisable();
            }
            if (cache != _saveModalShown)
                GuiEnable();
            
            // Gravitational Constant
            GuiLabel(GuiHelper.NewRectangle(new Vector2(0.85f, 0.01f), new Vector2(0.99f, 0.03f)),
                "Grav. Const.");
            unsafe
            {
                fixed (byte* ptr = _gravitationalConstantBuffer)
                {
                    if (GuiTextBox(GuiHelper.NewRectangle(new Vector2(0.85f, 0.03f), new Vector2(0.99f, 0.07f)),
                            (sbyte*)ptr, _gravitationalConstantBuffer.Length, _gravConstEditing) == 1)
                        _gravConstEditing = !_gravConstEditing;
                }
            }

            if (float.TryParse(Encoding.UTF8.GetString(_gravitationalConstantBuffer), out float resultGravConst))
            {
                GravitationalConstant = resultGravConst;
            }
            
            // Prediction Iterations
            GuiLabel(GuiHelper.NewRectangle(new Vector2(0.01f, 0.73f), new Vector2(0.15f, 0.75f)),
                "Prediction Iterations");
            
            if (GuiValueBox(GuiHelper.NewRectangle(new Vector2(0.01f, 0.75f), new Vector2(0.15f, 0.79f)), null,
                    ref _predictionIterations, 0, 100000, _iterationEditing) == 1)
                _iterationEditing = !_iterationEditing;
            
            // Predict Button
            if (GuiButton(GuiHelper.NewRectangle(new Vector2(0.01f, 0.8f), new Vector2(0.15f, 0.89f)), "Predict") == 1)
            {
                _cts?.Cancel();
                _ = Task.Run(() => Predict(_predictionIterations));
            }
            
            // Start Simulation
            if (GuiButton(GuiHelper.NewRectangle(new Vector2(0.01f, 0.9f), new Vector2(0.15f, 0.99f)), "Start Simulation") == 1)
                StartSimulation();
        }

        // Body Parameters
        if (_selectedBody == null) return;
        if (_isSimulating) return;
        var panelStart = new Vector2(0.75f, 0.7f);
        var panelEnd = new Vector2(0.99f, 0.99f);
        var panelRect = GuiHelper.NewRectangle(panelStart, panelEnd);
        
        float mouseWheelMove = GetMouseWheelMove();
        byte[] bytes = "Body Parameters"u8.ToArray();
        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                GuiPanel(panelRect, (sbyte*)ptr);
            }
        }

        int itemCount = 6;
        var offsetRect = panelRect with { Y = panelRect.Y + 20 };
        offsetRect = GuiHelper.Shrink(offsetRect, 5);
        
        // Labels
        var splitRectLabel = GuiHelper.SplitRectangleHor(offsetRect, 2, 0);
        GuiLabel(GuiHelper.SplitRectangleVert(splitRectLabel, itemCount, 0), "Body Velocity");
        GuiLabel(GuiHelper.SplitRectangleVert(splitRectLabel, itemCount, 1), "Body Position");
        GuiLabel(GuiHelper.SplitRectangleVert(splitRectLabel, itemCount, 2), "Body Mass");
        GuiLabel(GuiHelper.SplitRectangleVert(splitRectLabel, itemCount, 3), "Body Name");
        GuiLabel(GuiHelper.SplitRectangleVert(splitRectLabel, itemCount, 4), "Body Color");
        
        // Property
        var splitRectProperty = GuiHelper.SplitRectangleHor(offsetRect, 2, 1, -100);
        
        // Velocity
        unsafe
        {
            fixed (byte* ptr = _velocityXBuffer)
            {
                if (GuiTextBox(
                        GuiHelper.SplitRectangleHor(GuiHelper.SplitRectangleVert(splitRectProperty, itemCount, 0, 5), 2, 0),
                        (sbyte*)ptr, _velocityXBuffer.Length,
                        _velocityXEditing) == 1)
                {
                    _velocityXEditing = !_velocityXEditing;
                }
            }
        }

        if (float.TryParse(Encoding.UTF8.GetString(_velocityXBuffer), out float resultVelX))
        {
            _velocityX = resultVelX;
        }

        unsafe
        {
            fixed (byte* ptr = _velocityYBuffer)
            {
                if (GuiTextBox(
                        GuiHelper.SplitRectangleHor(GuiHelper.SplitRectangleVert(splitRectProperty, itemCount, 0, 5), 2, 1),
                        (sbyte*)ptr, _velocityYBuffer.Length,
                        _velocityYEditing) == 1)
                {
                    _velocityYEditing = !_velocityYEditing;
                }
            }
        }
        
        if (float.TryParse(Encoding.UTF8.GetString(_velocityYBuffer), out float resultVelY))
        {
            _velocityY = resultVelY;
        }

        if (mouseWheelMove != 0 && IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL))
        {
            if(IsKeyDown(KeyboardKey.KEY_LEFT_ALT))
                _velocityX += (int)mouseWheelMove;
            else
                _velocityY += (int)mouseWheelMove;
            
            Array.Clear(_velocityXBuffer);
            byte[] tempBytes = Encoding.UTF8.GetBytes(_velocityX.ToString(CultureInfo.InvariantCulture));
            Buffer.BlockCopy(tempBytes, 0, _velocityXBuffer, 0, Math.Min(tempBytes.Length, _velocityXBuffer.Length - 1));
        
            Array.Clear(_velocityYBuffer);
            tempBytes = Encoding.UTF8.GetBytes(_velocityY.ToString(CultureInfo.InvariantCulture));
            Buffer.BlockCopy(tempBytes, 0, _velocityYBuffer, 0, Math.Min(tempBytes.Length, _velocityYBuffer.Length - 1));
        }
        
        _selectedBody?.State.Velocity = new Vector2D(_velocityX, _velocityY);
        _selectedBody?.InitialState = _selectedBody.State;

        // Position
        unsafe
        {
            fixed (byte* ptr = _positionXBuffer)
            {
                if (GuiTextBox(
                        GuiHelper.SplitRectangleHor(GuiHelper.SplitRectangleVert(splitRectProperty, itemCount, 1, 5), 2, 0),
                        (sbyte*)ptr, _positionXBuffer.Length,
                        _positionXEditing) == 1)
                {
                    _positionXEditing = !_positionXEditing;
                }
            }
        }
        
        if (float.TryParse(Encoding.UTF8.GetString(_positionXBuffer), out float resultPosX))
        {
            _positionX = resultPosX;
        }

        unsafe
        {
            fixed (byte* ptr = _positionYBuffer)
            {
                if (GuiTextBox(
                        GuiHelper.SplitRectangleHor(GuiHelper.SplitRectangleVert(splitRectProperty, itemCount, 1, 5), 2, 1),
                        (sbyte*)ptr, _positionYBuffer.Length,
                        _positionYEditing) == 1)
                {
                    _positionYEditing = !_positionYEditing;
                }
            }
        }
        
        if (float.TryParse(Encoding.UTF8.GetString(_positionYBuffer), out float resultPosY))
        {
            _positionY = resultPosY;
        }
        
        if (IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) && IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) && !IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL))
        {
            var mousePos = GetScreenToWorld2D(GetMousePosition(), Camera);
            _positionX = mousePos.X;
            _positionY = mousePos.Y;
            
            Array.Clear(_positionXBuffer);
            byte[] tempBytes = Encoding.UTF8.GetBytes(_positionX.ToString(CultureInfo.InvariantCulture));
            Buffer.BlockCopy(tempBytes, 0, _positionXBuffer, 0, Math.Min(tempBytes.Length, _positionXBuffer.Length - 1));
        
            Array.Clear(_positionYBuffer);
            tempBytes = Encoding.UTF8.GetBytes(_positionY.ToString(CultureInfo.InvariantCulture));
            Buffer.BlockCopy(tempBytes, 0, _positionYBuffer, 0, Math.Min(tempBytes.Length, _positionYBuffer.Length - 1));
        }
        
        _selectedBody?.State.Position = new Vector2D(_positionX, _positionY);
        _selectedBody?.InitialState = _selectedBody.State;
        
        // Mass
        unsafe
        {
            fixed (byte* ptr = _massBuffer)
            {
                if(GuiTextBox(GuiHelper.SplitRectangleVert(splitRectProperty, itemCount, 2, 5), (sbyte*)ptr, _massBuffer.Length, _massEditing) == 1)
                {
                    _massEditing = !_massEditing;
                }
            }
        }
        
        if (float.TryParse(Encoding.UTF8.GetString(_massBuffer), out float resultMass))
        {
            _mass = resultMass;
        }
        
        
        if (mouseWheelMove != 0 && !IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) && IsKeyDown(KeyboardKey.KEY_LEFT_ALT))
        {
            _mass += mouseWheelMove;
            
            Array.Clear(_massBuffer);
            byte[] tempBytes = Encoding.UTF8.GetBytes(_mass.ToString(CultureInfo.InvariantCulture));
            Buffer.BlockCopy(tempBytes, 0, _massBuffer, 0, Math.Min(tempBytes.Length, _massBuffer.Length - 1));
        }
        
        _selectedBody?.State.Mass = _selectedBody.State.Mass = _mass;
        _selectedBody?.InitialState = _selectedBody.State;
        
        // Name
        unsafe
        {
            fixed (byte* ptr = _nameBuffer)
            {
                if(GuiTextBox(GuiHelper.SplitRectangleVert(splitRectProperty, itemCount, 3, 5), (sbyte*)ptr, _nameBuffer.Length, _nameEditing) == 1)
                {
                    _nameEditing = !_nameEditing;
                }
            }
        }

        _selectedBody?.Name = Encoding.UTF8.GetString(_nameBuffer);
    }

    public override void Draw()
    {
        BeginMode2D(Camera);
        foreach (var body in Bodies)
        {
            body.Draw();
        }
        
        if (_predictionPoints != null && _showPrediction)
        {
            for (var b = 0; b < _predictionPoints.Length; b++)
            {
                var color = Bodies[b].CurrentColor;
                var points = _predictionPoints[b];
                unsafe
                {
                    fixed (Vector2* ptr = points)
                        DrawLineStrip(ptr, _currentPredictedIterations + 1, color);
                }
            }
        }
        EndMode2D();

        DrawGui();
    }

    private void StartSimulation()
    {
        _cts?.Cancel();
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
        
        for (int step = 0; step < _simulationsSpeed; step++)
        {
            currentStates = Simulation.GetNextStepRk4(currentStates, GravitationalConstant, Program.FixedDelta);
        }

        for (int b = 0; b < Bodies.Count; b++)
        {
            Bodies[b].State = currentStates[b];
        }
    }
    
    private async Task Predict(int iterations, int substeps = 100)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        
        iterations = Math.Clamp(iterations, 0, 100000);
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

        var frameTimer = Stopwatch.StartNew();

        try
        {
            for (int i = 1; i <= iterations; i++)
            {
                if (token.IsCancellationRequested) return;

                for (int step = 0; step < substeps; step++)
                {
                    currentStates = Simulation.GetNextStepRk4(currentStates, GravitationalConstant, Program.FixedDelta);
                }
                
                for (int b = 0; b < currentStates.Length; b++)
                {
                    _predictionPoints[b][i] = (Vector2)currentStates[b].Position;
                }

                _currentPredictedIterations = i;
                
                if (frameTimer.Elapsed.TotalMilliseconds >= 16.67)
                {
                    await Task.Yield();
                    frameTimer.Restart();
                }
            }

            Console.WriteLine($"Predicted {Bodies.Count} Bodies with {iterations} iterations");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Prediction stopped {e.Message}");
        }
    }
}