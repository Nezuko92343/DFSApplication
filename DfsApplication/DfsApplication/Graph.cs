using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DfsApplication
{
    public class Graph
    {
        private readonly int vertexCount;
        private readonly List<int>[] adjList;

        public int VertexCount => vertexCount;
        public List<int>[] AdjList => adjList;

        public Graph(int vertices)
        {
            vertexCount = vertices;
            adjList = new List<int>[vertices];
            for (int i = 0; i < vertices; i++)
                adjList[i] = new List<int>();
        }

        /// <summary>
        /// Зчитування графу з файлу
        /// </summary>
        public static Graph LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл {filePath} не знайдено");

            var lines = File.ReadAllLines(filePath);

            if (lines.Length == 0)
                throw new InvalidDataException("Файл порожній");

            if (!int.TryParse(lines[0].Trim(), out int vertices))
                throw new InvalidDataException("Перший рядок має містити кількість вершин");

            if (vertices <= 0)
                throw new InvalidDataException("Кількість вершин має бути додатною");

            var graph = new Graph(vertices);

            var vertexSet = new HashSet<int>();

            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                {
                    Console.WriteLine($"Попередження: рядок {i + 1} пропущено (недостатньо даних)");
                    continue;
                }

                if (!int.TryParse(parts[0], out int src))
                {
                    Console.WriteLine($"Попередження: рядок {i + 1} пропущено (некоректна вершина джерело)");
                    continue;
                }

                if (!int.TryParse(parts[1], out int dst))
                {
                    Console.WriteLine($"Попередження: рядок {i + 1} пропущено (некоректна вершина призначення)");
                    continue;
                }

                if (src < 0 || src >= vertices)
                {
                    Console.WriteLine($"Попередження: вершина {src} поза межами (0-{vertices - 1})");
                    continue;
                }

                if (dst < 0 || dst >= vertices)
                {
                    Console.WriteLine($"Попередження: вершина {dst} поза межами (0-{vertices - 1})");
                    continue;
                }

                graph.AddEdge(src, dst);
                vertexSet.Add(src);
                vertexSet.Add(dst);
            }

            for (int i = 0; i < vertices; i++)
            {
                if (!vertexSet.Contains(i) && i != 0)
                {
                    Console.WriteLine($"Попередження: вершина {i} ізольована (немає ребер)");
                }
            }

            return graph;
        }

        /// <summary>
        /// Збереження графу у файл
        /// </summary>
        public void SaveToFile(string filePath)
        {
            var lines = new List<string> { vertexCount.ToString() };

            for (int i = 0; i < vertexCount; i++)
            {
                foreach (int neighbor in adjList[i])
                {
                    lines.Add($"{i} {neighbor}");
                }
            }

            File.WriteAllLines(filePath, lines);
            Console.WriteLine($"Граф збережено у {filePath}");
        }

        public void AddEdge(int source, int destination)
        {
            if (source >= 0 && source < vertexCount && destination >= 0 && destination < vertexCount)
            {
                adjList[source].Add(destination);
            }
        }

        public List<int> DfsSequential(int startVertex)
        {
            if (startVertex < 0 || startVertex >= vertexCount)
                throw new ArgumentOutOfRangeException(nameof(startVertex));

            var visited = new bool[vertexCount];
            var result = new List<int>(vertexCount);
            var stack = new Stack<int>();
            stack.Push(startVertex);

            while (stack.Count > 0)
            {
                int current = stack.Pop();
                if (visited[current]) continue;

                visited[current] = true;
                result.Add(current);

                var neighbors = adjList[current];
                for (int i = neighbors.Count - 1; i >= 0; i--)
                {
                    int neighbor = neighbors[i];
                    if (!visited[neighbor])
                        stack.Push(neighbor);
                }
            }

            return result;
        }

        public bool ValidateDfsResult(List<int> result, int startVertex)
        {
            if (result == null || result.Count != vertexCount)
                return false;

            if (result[0] != startVertex)
                return false;

            var visited = new bool[vertexCount];
            foreach (var vertex in result)
            {
                if (vertex < 0 || vertex >= vertexCount)
                    return false;
                if (visited[vertex])
                    return false;
                visited[vertex] = true;
            }

            return true;
        }

        public void PrintGraph()
        {
            Console.WriteLine("\nСписок суміжності:");
            for (int i = 0; i < vertexCount; i++)
            {
                Console.Write($"{i} -> ");
                foreach (int neighbor in adjList[i])
                    Console.Write($"{neighbor} ");
                Console.WriteLine();
            }
        }
    }
}