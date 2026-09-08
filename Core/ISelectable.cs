using SolarSystemSim.Celestial;
using ZeroElectric.Vinculum;

namespace SolarSystemSim.Core;

public interface ISelectable
{
    public event Action<CelestialBody>? MouseEntered;
    public event Action<CelestialBody>? MouseExited;
    public event Action<CelestialBody>? Clicked;
    void CheckSelectable(Camera2D camera);
}