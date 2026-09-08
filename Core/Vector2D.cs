using System.Numerics;

namespace SolarSystemSim.Core;

public record struct Vector2D(double X, double Y)
{
    public static Vector2D Zero => new Vector2D(0, 0);

    public static Vector2D operator +(Vector2D a, Vector2D b) => new Vector2D(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new Vector2D(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator *(Vector2D a, double scalar) => new Vector2D(a.X * scalar, a.Y * scalar);
    public static Vector2D operator /(Vector2D a, double scalar) => new Vector2D(a.X / scalar, a.Y / scalar);

    public static explicit operator Vector2(Vector2D v)
    {
        return new Vector2((float)v.X, (float)v.Y);
    }
    public static implicit operator Vector2D(Vector2 v)
    {
        return new Vector2D(v.X, v.Y);
    }
    public double LengthSquared() => X * X + Y * Y;
    
    public static Vector2D Normalize(Vector2D v)
    {
        double lenSq = v.LengthSquared();
        if (lenSq < 1e-9) return Zero;
        return v / Math.Sqrt(lenSq);
    }
    
    public Vector2 ToFloat() => new System.Numerics.Vector2((float)X, (float)Y);
}
