using SkiaSharp;

namespace ModerBox.CableRouting;

/// <summary>
/// 电缆走向绘制器
/// </summary>
public class CableRenderer : IDisposable
{
    // 颜色定义
    private static readonly Dictionary<PointType, SKColor> PointColors = new()
    {
        { PointType.Observation, new SKColor(255, 255, 255) },  // 白色
        { PointType.Pass, new SKColor(255, 255, 0) },           // 黄色
        { PointType.Start, new SKColor(0, 255, 0) },            // 绿色
        { PointType.End, new SKColor(0, 100, 255) }             // 蓝色
    };
    
    private static readonly SKColor PathColor = new(255, 0, 0);  // 红色路径
    private const float PathWidth = 3f;
    private const float DefaultPointRadius = 8f;
    private const float DefaultFontSize = 14f;
    
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly List<BoundingRect> _occupiedRects = new();
    private readonly SKTypeface _typeface;
    
    public int Width => _bitmap.Width;
    public int Height => _bitmap.Height;

    public float PointRadius { get; set; } = DefaultPointRadius;
    public float FontSize { get; set; } = DefaultFontSize;
    
    /// <summary>
    /// 从图片文件创建渲染器
    /// </summary>
    public CableRenderer(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"底图文件不存在: {imagePath}");
        }
        
        using var stream = File.OpenRead(imagePath);
        _bitmap = SKBitmap.Decode(stream);
        if (_bitmap == null)
        {
            throw new InvalidOperationException($"无法解码图片: {imagePath}");
        }
        
        _canvas = new SKCanvas(_bitmap);
        _typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal) 
                    ?? SKTypeface.Default;
    }
    
    /// <summary>
    /// 从空白画布创建渲染器（用于演示）
    /// </summary>
    public CableRenderer(int width, int height)
    {
        _bitmap = new SKBitmap(width, height);
        _canvas = new SKCanvas(_bitmap);
        _canvas.Clear(new SKColor(50, 50, 50));  // 深灰色背景
        _typeface = SKTypeface.FromFamilyName("Microsoft YaHei", SKFontStyle.Normal) 
                    ?? SKTypeface.Default;
    }
    
    /// <summary>
    /// 绘制路径
    /// </summary>
    public void DrawPath(List<RoutePoint> route, IEnumerable<RoutePoint> observations)
    {
        var corrector = new OrthogonalCorrector(observations);
        
        using var pathPaint = new SKPaint
        {
            Color = PathColor,
            StrokeWidth = PathWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };
        
        for (int i = 0; i < route.Count - 1; i++)
        {
            var p1 = route[i];
            var p2 = route[i + 1];
            
            if (IsDirectConnection(p1, p2))
            {
                // 直线连接（观测点之间、同 pair 穿管之间）
                _canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, pathPaint);
            }
            else
            {
                // L型或Z型连接
                var corners = corrector.FindCornerPoints(p1, p2);
                
                if (corners.Count == 0)
                {
                    // 无拐点，直线
                    _canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, pathPaint);
                }
                else if (corners.Count == 1)
                {
                    // L型连接
                    _canvas.DrawLine(p1.X, p1.Y, corners[0].X, corners[0].Y, pathPaint);
                    _canvas.DrawLine(corners[0].X, corners[0].Y, p2.X, p2.Y, pathPaint);
                }
                else
                {
                    // Z型连接（2个拐点）
                    _canvas.DrawLine(p1.X, p1.Y, corners[0].X, corners[0].Y, pathPaint);
                    _canvas.DrawLine(corners[0].X, corners[0].Y, corners[1].X, corners[1].Y, pathPaint);
                    _canvas.DrawLine(corners[1].X, corners[1].Y, p2.X, p2.Y, pathPaint);
                }
            }
        }
    }
    
    /// <summary>
    /// 判断是否使用直线连接
    /// </summary>
    private static bool IsDirectConnection(RoutePoint p1, RoutePoint p2)
    {
        // observation ↔ observation
        if (p1.Type == PointType.Observation && p2.Type == PointType.Observation)
            return true;
        
        // 同 pair 的 pass 点
        if (p1.Type == PointType.Pass && p2.Type == PointType.Pass &&
            !string.IsNullOrEmpty(p1.Pair) && p1.Pair == p2.Pair)
            return true;
        
        // 垂足点（路由中自动生成的中间点）与任何点直线连接
        if (p1.Id == "_foot_" || p2.Id == "_foot_")
            return true;
        
        return false;
    }
    
    /// <summary>
    /// 绘制所有点位
    /// </summary>
    public void DrawPoints(IEnumerable<RoutePoint> points)
    {
        using var font = new SKFont(_typeface, FontSize);
        var labelRects = new List<BoundingRect>();  // 已绘制的标签区域
        
        foreach (var p in points)
        {
            // 不绘制路由中间生成的垂足点
            if (p.Id == "_foot_") continue;
            
            var color = PointColors.GetValueOrDefault(p.Type, new SKColor(128, 128, 128));
            
            // 绘制圆点填充
            using var fillPaint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            _canvas.DrawCircle(p.X, p.Y, PointRadius, fillPaint);
            
            // 绘制圆点边框
            using var strokePaint = new SKPaint
            {
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true
            };
            _canvas.DrawCircle(p.X, p.Y, PointRadius, strokePaint);
            
            // 测量文本宽度
            using var measurePaint = new SKPaint { IsAntialias = true };
            float textWidth = font.MeasureText(p.Id, out _, measurePaint);
            float textHeight = FontSize;
            float margin = PointRadius + 3;
            
            // 尝试多个位置放置文本（右、左、上、下、右上、右下、左上、左下）
            var candidatePositions = new (float x, float y)[]
            {
                (p.X + margin, p.Y + textHeight / 3),                          // 右
                (p.X - margin - textWidth, p.Y + textHeight / 3),              // 左
                (p.X - textWidth / 2, p.Y - margin),                          // 上
                (p.X - textWidth / 2, p.Y + margin + textHeight),             // 下
                (p.X + margin, p.Y - margin),                                  // 右上
                (p.X + margin, p.Y + margin + textHeight),                     // 右下
                (p.X - margin - textWidth, p.Y - margin),                      // 左上
                (p.X - margin - textWidth, p.Y + margin + textHeight),         // 左下
            };
            
            float textX = candidatePositions[0].x;
            float textY = candidatePositions[0].y;
            
            foreach (var (cx, cy) in candidatePositions)
            {
                var candidateRect = new BoundingRect(
                    (int)cx - 2, (int)(cy - textHeight) - 2,
                    (int)textWidth + 4, (int)textHeight + 4);
                
                bool overlaps = labelRects.Any(r => candidateRect.Intersects(r));
                if (!overlaps)
                {
                    textX = cx;
                    textY = cy;
                    break;
                }
            }
            
            // 记录标签占用区域
            labelRects.Add(new BoundingRect(
                (int)textX - 2, (int)(textY - textHeight) - 2,
                (int)textWidth + 4, (int)textHeight + 4));
            
            // 文字描边效果（黑色阴影）
            using var shadowPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };
            foreach (var (dx, dy) in new[] { (-1, -1), (-1, 1), (1, -1), (1, 1) })
            {
                _canvas.DrawText(p.Id, textX + dx, textY + dy, font, shadowPaint);
            }
            
            // 文字本体（白色）
            using var textPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };
            _canvas.DrawText(p.Id, textX, textY, font, textPaint);
            
            // 记录点位占用区域（用于表格避让）
            _occupiedRects.Add(new BoundingRect(
                (int)(p.X - PointRadius - 5),
                (int)(p.Y - PointRadius - 5),
                (int)(PointRadius * 2 + textWidth + margin + 10),
                (int)(PointRadius * 2 + 10)
            ));
        }
    }
    
    /// <summary>
    /// 在终点附近绘制业务统计表格（二维数组格式）
    /// </summary>
    public void DrawEndTable(RoutePoint endPoint, EndTableData? tableData)
    {
        if (tableData == null || tableData.Data.Count == 0)
            return;
        
        using var font = new SKFont(_typeface, FontSize);
        
        // 计算表格尺寸（自适应字体大小）
        int padding = Math.Max(10, (int)(FontSize * 0.7));
        int rowHeight = Math.Max(25, (int)(FontSize * 1.8));
        int colCount = tableData.Data.Max(row => row.Count);  // 最多列数
        int[] colWidths = new int[colCount];
        
        // 按实际文本宽度计算每列宽度
        using var measurePaint = new SKPaint { IsAntialias = true };
        for (int j = 0; j < colCount; j++)
        {
            float maxWidth = 0;
            foreach (var row in tableData.Data)
            {
                if (j < row.Count)
                {
                    float w = font.MeasureText(row[j], out _, measurePaint);
                    if (w > maxWidth) maxWidth = w;
                }
            }
            colWidths[j] = (int)maxWidth + padding;
        }
        
        // 确保表格至少能容纳标题
        float titleWidth = font.MeasureText(tableData.Title, out _, measurePaint);
        int tableWidth = Math.Max(colWidths.Sum() + padding * 2, (int)titleWidth + padding * 2);
        int tableHeight = tableData.Data.Count * rowHeight + padding * 2 + (int)(FontSize * 2);  // 包含标题空间
        
        // 尝试位置顺序：右上 → 右下 → 左上 → 左下 → 上 → 下
        var positions = new[]
        {
            (endPoint.X + 30, endPoint.Y - tableHeight - 10),   // 右上
            (endPoint.X + 30, endPoint.Y + 30),                  // 右下
            (endPoint.X - tableWidth - 30, endPoint.Y - tableHeight - 10),  // 左上
            (endPoint.X - tableWidth - 30, endPoint.Y + 30),    // 左下
            (endPoint.X - tableWidth / 2, endPoint.Y - tableHeight - 40),   // 上
            (endPoint.X - tableWidth / 2, endPoint.Y + 40)      // 下
        };
        
        // 找到第一个无冲突位置
        int tableX = positions[0].Item1;
        int tableY = positions[0].Item2;
        
        foreach (var (px, py) in positions)
        {
            // 边界检查
            if (px < 0 || py < 0) continue;
            if (px + tableWidth > Width || py + tableHeight > Height) continue;
            
            // 碰撞检测
            var tableRect = new BoundingRect(px, py, tableWidth, tableHeight);
            bool conflict = _occupiedRects.Any(r => tableRect.Intersects(r));
            
            if (!conflict)
            {
                tableX = px;
                tableY = py;
                break;
            }
        }
        
        // 绘制表格背景
        using var bgPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, 230),
            Style = SKPaintStyle.Fill
        };
        _canvas.DrawRect(tableX, tableY, tableWidth, tableHeight, bgPaint);
        
        // 绘制表格边框
        using var borderPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        _canvas.DrawRect(tableX, tableY, tableWidth, tableHeight, borderPaint);
        
        // 绘制标题
        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };
        int titleY = tableY + padding + (int)(FontSize * 0.8);
        _canvas.DrawText(tableData.Title, tableX + padding, titleY, font, textPaint);
        
        // 绘制分隔线（在标题下方）
        int headerLineY = tableY + padding + (int)(FontSize * 1.5);
        using var linePaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 1
        };
        _canvas.DrawLine(tableX, headerLineY, tableX + tableWidth, headerLineY, linePaint);
        
        // 绘制数据行（二维数组）
        int dataStartY = headerLineY + 5;
        for (int i = 0; i < tableData.Data.Count; i++)
        {
            var row = tableData.Data[i];
            int rowY = dataStartY + i * rowHeight + (int)(FontSize * 0.8);
            
            int currentX = tableX + padding;
            for (int j = 0; j < row.Count; j++)
            {
                _canvas.DrawText(row[j], currentX, rowY, font, textPaint);
                currentX += colWidths[j];
            }
        }
        
        // 记录表格占用区域
        _occupiedRects.Add(new BoundingRect(tableX, tableY, tableWidth, tableHeight));
    }
    
    /// <summary>
    /// 保存结果图片
    /// </summary>
    public void Save(string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        using var image = SKImage.FromBitmap(_bitmap);
        using var data = image.Encode(GetFormat(outputPath), 95);
        using var stream = File.Create(outputPath);
        data.SaveTo(stream);
    }
    
    private static SKEncodedImageFormat GetFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".bmp" => SKEncodedImageFormat.Bmp,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
    }
    
    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
        _typeface.Dispose();
    }
}
