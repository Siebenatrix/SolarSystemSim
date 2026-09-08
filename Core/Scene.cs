namespace SolarSystemSim.Core;

public abstract class Scene
{
    public virtual void Load(){}
    
    public virtual void Update(){}
    public virtual void Draw(){}
    
    public virtual void Unload(){}
}