namespace ModerBox.CableRouting;

/// <summary>
/// 矩形区域，用于碰撞检测
/// </summary>
public readonly struct BoundingRect
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    
    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;
    
    public BoundingRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    /// <summary>
    /// 检测两个矩形是否相交
    /// </summary>
    public bool Intersects(BoundingRect other)
    {
        return !(Right < other.Left ||
                 other.Right < Left ||
                 Bottom < other.Top ||
                 other.Bottom < Top);
    }
    
    /// <summary>
    /// 检测点是否在矩形内
    /// </summary>
    public bool Contains(int x, int y)
    {
        return x >= Left && x <= Right && y >= Top && y <= Bottom;
    }
}
