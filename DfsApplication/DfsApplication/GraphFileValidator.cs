using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DfsApplication
{
    public static class GraphFileValidator
    {
        public static bool Validate(string filePath, out string error)
        {
            error = null;

            if (!File.Exists(filePath))
            {
                error = $"Файл {filePath} не існує";
                return false;
            }

            var lines = File.ReadAllLines(filePath);

            if (lines.Length == 0)
            {
                error = "Файл порожній";
                return false;
            }

            if (!int.TryParse(lines[0].Trim(), out int vertices))
            {
                error = "Перший рядок має містити кількість вершин";
                return false;
            }

            if (vertices <= 0 || vertices > 1000000)
            {
                error = $"Кількість вершин {vertices} поза допустимим діапазоном (1-1000000)";
                return false;
            }

            var edges = new HashSet<(int, int)>();

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    error = $"Рядок {i + 1}: недостатньо даних";
                    return false;
                }

                if (!int.TryParse(parts[0], out int src))
                {
                    error = $"Рядок {i + 1}: некоректна вершина джерело";
                    return false;
                }

                if (!int.TryParse(parts[1], out int dst))
                {
                    error = $"Рядок {i + 1}: некоректна вершина призначення";
                    return false;
                }

                if (src < 0 || src >= vertices)
                {
                    error = $"Рядок {i + 1}: вершина {src} поза межами (0-{vertices - 1})";
                    return false;
                }

                if (dst < 0 || dst >= vertices)
                {
                    error = $"Рядок {i + 1}: вершина {dst} поза межами (0-{vertices - 1})";
                    return false;
                }

                if (edges.Contains((src, dst)))
                {
                    Console.WriteLine($"Попередження: дублікат ребра ({src}, {dst}) у рядку {i + 1}");
                }
                else
                {
                    edges.Add((src, dst));
                }
            }

            return true;
        }

        public static void PrintStatistics(string filePath)
        {
            if (!Validate(filePath, out string error))
            {
                Console.WriteLine($"Помилка: {error}");
                return;
            }

            var lines = File.ReadAllLines(filePath);
            int vertices = int.Parse(lines[0].Trim());
            int edges = lines.Length - 1;

            Console.WriteLine($"Статистика графу:");
            Console.WriteLine($"  Вершин: {vertices}");
            Console.WriteLine($"  Ребер: {edges}");
            Console.WriteLine($"  Щільність: {(double)edges / (vertices * (vertices - 1) / 2) * 100:F2}%");
        }
    }
}