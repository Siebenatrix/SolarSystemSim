using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace SolarSystemSim.Core;

public static class GuiHelper
{
    public static Rectangle ToScreenSpace(Rectangle rect) => new Rectangle
    (
        rect.X * GetScreenWidth(),
        rect.Y * GetScreenHeight(),
        rect.Width * GetScreenWidth(),
        rect.Height * GetScreenHeight()
    );

    public static Vector2 ToScreenSpace(Vector2 vec) => vec * new Vector2(GetScreenWidth(), GetScreenHeight());

    public static Rectangle NewRectangle(Vector2 anchorStart, Vector2 anchorEnd)
    {
        var screenStart = ToScreenSpace(anchorStart);
        var screenEnd = ToScreenSpace(anchorEnd);
        var rectSize = screenEnd - screenStart;

        return new Rectangle(screenStart.X, screenStart.Y, rectSize.X, rectSize.Y);
    }

    public static Rectangle SplitRectangleHor(Rectangle rect, int splitAmount, int splitIdx, int margin = 0)
    {
        float totalMargin = margin * (splitAmount - 1);
        float availableWidth = MathF.Max(0, rect.Width - totalMargin);
        
        float width = availableWidth / splitAmount;
        float remainder = availableWidth % splitAmount;

        float currentWidth = width + (splitIdx < remainder ? 1 : 0);
        float currentX = rect.X + width * splitIdx + MathF.Min(splitIdx, remainder) + margin * splitIdx;

        return new Rectangle(currentX, rect.Y, currentWidth, rect.Height);
    }
    
    public static Rectangle SplitRectangleVert(Rectangle rect, int splitAmount, int splitIdx, int margin = 0)
    {
        float totalMargin = margin * (splitAmount - 1);
        float availableHeight = MathF.Max(0, rect.Height - totalMargin);
        
        float height = availableHeight / splitAmount;
        float remainder = availableHeight % splitAmount;

        float currentHeight = height + (splitIdx < remainder ? 1 : 0);
        float currentY = rect.Y + currentHeight * splitIdx + MathF.Min(splitIdx, remainder) + margin * splitIdx;

        return new Rectangle(rect.X, currentY, rect.Width, currentHeight);
    }

    public static Rectangle Shrink(Rectangle rect, float amount)
    {
        return new Rectangle(
            rect.X + amount,
            rect.Y + amount,
            rect.Width - amount * 2,
            rect.Height - amount * 2
        );
    }
}