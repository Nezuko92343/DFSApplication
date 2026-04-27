using DfsApplication;
using System;
using System.Collections.Generic;
using System.Diagnostics;

class Program
{
    static void Main()
    {
        int vertices = 500_000;
        int edgesPerVertex = 5;

        var graph = GenerateGraph(vertices, edgesPerVertex);
        var dfs = new ImprovedWorkStealingDFS(vertices, graph);

        dfs.SetDepthThreshold(32);         
        dfs.RunWorkStealingDfs(0);         

        int[] thresholds = { 4, 8, 16, 32, 64, 128 };

        foreach (var t in thresholds)
        {
            dfs.SetDepthThreshold(t);

            int runs = 20;
            var times = new List<long>();

            for (int i = 0; i < runs; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var sw = Stopwatch.StartNew();

                dfs.RunWorkStealingDfs(0);

                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }

            times.Sort();

            long median = times[times.Count / 2];
            long min = times[0];
            long max = times[times.Count - 1];

            Console.WriteLine($"Threshold = {t}, Median = {median} ms, Min = {min}, Max = {max}");
        }
    }

    static List<int>[] GenerateGraph(int vertices, int edgesPerVertex)
    {
        var rand = new Random();
        var graph = new List<int>[vertices];

        for (int i = 0; i < vertices; i++)
        {
            graph[i] = new List<int>();

            for (int j = 0; j < edgesPerVertex; j++)
            {
                int neighbor = rand.Next(vertices);

                if (neighbor != i)
                    graph[i].Add(neighbor);
            }
        }

        return graph;
    }
}