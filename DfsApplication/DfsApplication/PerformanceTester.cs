using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DfsApplication
{
    /// <summary>
    /// Клас для вимірювання швидкодії послідовного алгоритму.
    /// </summary>
    public class PerformanceTester
    {
        private readonly GraphGenerator generator = new GraphGenerator();

        public void RunBenchmark(int vertices, int edgesPerVertex, int runs)
        {
            int totalEdges = vertices * edgesPerVertex;
            var graph = generator.GenerateConnectedGraph(vertices, totalEdges);

            var times = new List<double>();
            var sw = new Stopwatch();

            Console.WriteLine($"\nБенчмарк: {vertices} вершин, {totalEdges} ребер, {runs} запусків");

            for (int r = 0; r < runs; r++)
            {
                sw.Restart();
                graph.DfsSequential(0);
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            double avgTime = 0;
            foreach (var t in times) avgTime += t;
            avgTime /= times.Count;

            double stdDev = 0;
            foreach (var t in times)
                stdDev += (t - avgTime) * (t - avgTime);
            stdDev = Math.Sqrt(stdDev / times.Count);

            Console.WriteLine($"  Середній час: {avgTime:F2} мс");
            Console.WriteLine($"  Стандартне відхилення: {stdDev:F2} мс");
            Console.WriteLine($"  Пропускна здатність: {vertices / avgTime:F0} вершин/мс");
        }
    }
}