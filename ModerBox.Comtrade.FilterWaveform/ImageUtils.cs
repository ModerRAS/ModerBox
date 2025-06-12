using SkiaSharp;
using System;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 提供图像处理相关的实用工具方法。
    /// </summary>
    public static class ImageUtils {
        /// <summary>
        /// 使用 SkiaSharp 将两个 PNG 格式的字节数组拼接为一张图片，并返回拼接后的字节数组。
        /// </summary>
        /// <param name="pngBytes1">第一张图片（置于顶部）的PNG格式字节数组。</param>
        /// <param name="pngBytes2">第二张图片（置于底部）的PNG格式字节数组。</param>
        /// <returns>拼接后新图片的PNG格式字节数组。</returns>
        public static byte[] CombineImages(byte[] pngBytes1, byte[] pngBytes2) {
            // 使用 SkiaSharp 将字节数组转换为 SKBitmap 对象
            using (var bmp1 = SKBitmap.Decode(pngBytes1))
            using (var bmp2 = SKBitmap.Decode(pngBytes2)) {
                // 创建一个新的 SKBitmap，用于存储拼接后的图像
                int width = Math.Max(bmp1.Width, bmp2.Width);
                int height = bmp1.Height + bmp2.Height;
                using (var finalImage = new SKBitmap(width, height)) {
                    using (var canvas = new SKCanvas(finalImage)) {
                        // 将第一张图片绘制到上半部分
                        canvas.DrawBitmap(bmp1, new SKPoint(0, 0));
                        // 将第二张图片绘制到下半部分
                        canvas.DrawBitmap(bmp2, new SKPoint(0, bmp1.Height));
                    }

                    // 将拼接后的图像转换为字节数组
                    using (var image = SKImage.FromBitmap(finalImage))
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) {
                        return data.ToArray();
                    }
                }
            }
        }
    }
}