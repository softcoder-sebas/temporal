using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MyMarket_ERP
{
    internal static class SignatureQrGenerator
    {
        public static string? TryGenerate(string? payload, string? signatureHash)
        {
            var matrix = TryGenerateMatrix(payload, signatureHash);
            if (matrix is null)
                return null;

            return Render(matrix, 8, 4);
        }

        internal static bool[,]? TryGenerateMatrix(string? payload, string? signatureHash)
        {
            string data = string.IsNullOrWhiteSpace(payload) ? signatureHash ?? string.Empty : payload;
            if (string.IsNullOrWhiteSpace(data))
                return null;

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
            const int modules = 29;
            bool[,] matrix = new bool[modules, modules];
            bool[,] reserved = new bool[modules, modules];

            AddFinder(matrix, reserved, 0, 0);
            AddFinder(matrix, reserved, modules - 7, 0);
            AddFinder(matrix, reserved, 0, modules - 7);

            int bitIndex = 0;
            int totalBits = hash.Length * 8;
            for (int y = 0; y < modules; y++)
            {
                for (int x = 0; x < modules; x++)
                {
                    if (reserved[y, x])
                        continue;
                    int byteIndex = bitIndex / 8;
                    int bitOffset = bitIndex % 8;
                    bool value = ((hash[byteIndex] >> bitOffset) & 1) != 0;
                    matrix[y, x] = value;
                    bitIndex = (bitIndex + 1) % totalBits;
                }
            }

            return matrix;
        }

        internal static string RenderToBase64(bool[,] matrix, int pixelsPerModule = 8, int border = 4)
            => Render(matrix, pixelsPerModule, border);

        private static void AddFinder(bool[,] matrix, bool[,] reserved, int startX, int startY)
        {
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    int xx = startX + x;
                    int yy = startY + y;
                    if (xx < 0 || xx >= matrix.GetLength(1) || yy < 0 || yy >= matrix.GetLength(0))
                        continue;
                    bool dark = x == 0 || x == 6 || y == 0 || y == 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4);
                    matrix[yy, xx] = dark;
                    reserved[yy, xx] = true;
                }
            }

            for (int y = -1; y <= 7; y++)
            {
                for (int x = -1; x <= 7; x++)
                {
                    int xx = startX + x;
                    int yy = startY + y;
                    if (xx < 0 || xx >= matrix.GetLength(1) || yy < 0 || yy >= matrix.GetLength(0))
                        continue;
                    if (!reserved[yy, xx])
                        reserved[yy, xx] = x >= -1 && x <= 7 && y >= -1 && y <= 7;
                }
            }
        }

        private static string Render(bool[,] matrix, int pixelsPerModule, int border)
        {
            int modules = matrix.GetLength(0);
            int size = (modules + border * 2) * pixelsPerModule;
            using var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                for (int y = 0; y < modules; y++)
                {
                    for (int x = 0; x < modules; x++)
                    {
                        if (!matrix[y, x])
                            continue;
                        int xx = (x + border) * pixelsPerModule;
                        int yy = (y + border) * pixelsPerModule;
                        g.FillRectangle(Brushes.Black, xx, yy, pixelsPerModule, pixelsPerModule);
                    }
                }
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
