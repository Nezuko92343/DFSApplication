using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DfsApplication
{
    public class CompleteExperiments
    {
        private readonly GraphGenerator generator = new GraphGenerator();
        private readonly int processorCount = Environment.ProcessorCount;

        public void RunAllExperiments()
        {
            Console.WriteLine($"\nСистемна інформація:");
            Console.WriteLine($"Процесор: {processorCount} логічних ядер");
            Console.WriteLine($"Операційна система: {Environment.OSVersion}");
            Console.WriteLine($".NET: {Environment.Version}");
            Console.WriteLine($"Дата: {DateTime.Now:yyyy-MM-dd HH:mm}");
            Console.WriteLine();

            ExperimentScalabilityWorkStealing();
            ExperimentThreadScalingWorkStealing();
            ExperimentWeakScalingCustomSizes();
            ExperimentGraphDensityWorkStealing();
            ExperimentFinalBenchmarkWorkStealing();
            ExperimentCorrectnessStressTestWorkStealing();
            ExperimentFindOptimalSizeWorkStealing();
        }

        private void ExperimentScalabilityWorkStealing()
        {
            Console.WriteLine("\nЕксперимент 1: Масштабованість");

            int[] sizes = { 500000, 400000, 300000, 200000, 100000, 50000, 20000, 10000 };
            int edgesPerVertex = 20;
            int runs = 20;

            Console.WriteLine($"{"Вершини",10} {"Ребра",12} {"Послід.(мс)",12} {"Парал.(мс)",12} {"Прискорення",10}");

            foreach (int v in sizes)
            {
                long totalEdgesLong = (long)v * edgesPerVertex;
                if (totalEdgesLong > int.MaxValue) continue;

                int totalEdges = (int)totalEdgesLong;

                try
                {
                    var baseGraph = generator.GenerateConnectedGraph(v, totalEdges);

                    var seqGraph = new Graph(v);
                    CopyGraphToGraph(baseGraph, seqGraph, v);

                    seqGraph.DfsSequential(0);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < runs; i++)
                        seqGraph.DfsSequential(0);
                    sw.Stop();
                    double seqAvg = sw.Elapsed.TotalMilliseconds / runs;

                    var parGraph = new ImprovedWorkStealingDFS(v, baseGraph.AdjList);

                    parGraph.RunWorkStealingDfs(0);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    sw.Restart();

                    for (int i = 0; i < runs; i++)
                        parGraph.RunWorkStealingDfs(0);
                    sw.Stop();
                    double parAvg = sw.Elapsed.TotalMilliseconds / runs;

                    double speedup = seqAvg / parAvg;

                    var seqResult = seqGraph.DfsSequential(0);
                    var parResult = parGraph.RunWorkStealingDfs(0);
                    bool isValid = seqResult.Count == parResult.Count &&
                                  new HashSet<int>(seqResult).SetEquals(new HashSet<int>(parResult));

                    Console.WriteLine($"{v,10:N0} {totalEdges,12:N0} {seqAvg,12:F2} {parAvg,12:F2} {speedup,10:F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка для розміру {v}: {ex.Message}");
                }
            }
        }

        private void ExperimentThreadScalingWorkStealing()
        {
            Console.WriteLine("\nЕксперимент 2: Масштабування по потоках (Work-Stealing DFS)");

            int vertices = 400000;
            int edges = vertices * 20;
            int runs = 20;

            Console.WriteLine($"Кількість вершин у графі: {vertices}");

            var baseGraph = generator.GenerateConnectedGraph(vertices, edges);

            var seqGraph = new Graph(vertices);
            CopyGraphToGraph(baseGraph, seqGraph, vertices);

            double seqAvg = MeasureTimeWorkStealing(() => seqGraph.DfsSequential(0), runs);

            Console.WriteLine($"\nЕталон (1 потік): {seqAvg:F2} мс\n");
            Console.WriteLine($"{"Потоки",8} {"Час (мс)",10} {"Прискорення",12}");

            int[] threadCounts = { 1, 2, 4, 6, 8, 12, 16 };

            foreach (int threads in threadCounts)
            {
                var parGraph = new ImprovedWorkStealingDFS(vertices, baseGraph.AdjList);

                double parAvg = MeasureTimeWorkStealing(
                    () => parGraph.RunWorkStealingDfs(0, threads),
                    runs);

                double speedup = seqAvg / parAvg;

                Console.WriteLine($"{threads,8} {parAvg,10:F2} {speedup,12:F2}");
            }
        }

        private void ExperimentWeakScalingCustomSizes()
        {
            Console.WriteLine("\nЕксперимент 3: Weak Scaling (кастомні розміри)");

            int runs = 20;

            int[] threadCounts = { 1, 2, 4, 6, 8, 12, 16 };
            int[] vertexSizes = { 25000, 50000, 100000, 200000, 400000, 500000 };

            Console.WriteLine($"\n{"Вершини",10} {"Потоки",8} {"Час seq",12} {"Час par",12} {"Speedup",10}");

            foreach (int vertices in vertexSizes)
            {
                int edges = vertices * 20;

                var graph = generator.GenerateConnectedGraph(vertices, edges);

                double seqTime = 0;

                for (int i = 0; i < runs; i++)
                {
                    var seqGraph = new Graph(vertices);
                    CopyGraphToGraph(graph, seqGraph, vertices);

                    var sw = Stopwatch.StartNew();
                    seqGraph.DfsSequential(0);
                    sw.Stop();

                    seqTime += sw.Elapsed.TotalMilliseconds;
                }

                seqTime /= runs;

                foreach (int threads in threadCounts)
                {
                    double parTime = 0;

                    for (int i = 0; i < runs; i++)
                    {
                        var dfs = new ImprovedWorkStealingDFS(vertices, graph.AdjList);

                        var sw = Stopwatch.StartNew();
                        dfs.RunWorkStealingDfs(0, threads);
                        sw.Stop();

                        parTime += sw.Elapsed.TotalMilliseconds;
                    }

                    parTime /= runs;

                    double speedup = seqTime / parTime;

                    Console.WriteLine($"{vertices,10} {threads,8} {seqTime,12:F2} {parTime,12:F2} {speedup,10:F2}");
                }
                Console.WriteLine();
            }
        }

        private void ExperimentGraphDensityWorkStealing()
        {
            Console.WriteLine("\nЕксперимент 3: Оптимальна густина графу");

            int vertices = 50000;
            int[] densities = { 5, 10, 20, 30, 50, 75, 100 };
            int runs = 20;

            Console.WriteLine($"\n{"Ребер/верш",8} {"Всього ребер",12} {"Послід.(мс)",12} {"Парал.(мс)",12} {"Прискорення",10}");

            double bestSpeedup = 0;
            int bestDensity = 0;

            foreach (int deg in densities)
            {
                int totalEdges = vertices * deg;

                try
                {
                    var baseGraph = generator.GenerateConnectedGraph(vertices, totalEdges);

                    var seqGraph = new Graph(vertices);
                    CopyGraphToGraph(baseGraph, seqGraph, vertices);
                    double seqAvg = MeasureTimeWorkStealing(() => seqGraph.DfsSequential(0), runs);

                    var parGraph = new ImprovedWorkStealingDFS(vertices, baseGraph.AdjList);
                    double parAvg = MeasureTimeWorkStealing(() => parGraph.RunWorkStealingDfs(0), runs);

                    double speedup = seqAvg / parAvg;

                    if (speedup > bestSpeedup)
                    {
                        bestSpeedup = speedup;
                        bestDensity = deg;
                    }

                    Console.WriteLine($"{deg,8} {totalEdges,12:N0} {seqAvg,12:F2} {parAvg,12:F2} {speedup,10:F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка: {ex.Message}");
                }
            }
            Console.WriteLine($"\nОптимальна густина = {bestDensity} ребер/вершину (прискорення: {bestSpeedup:F2}x)");
        }

        private void ExperimentFinalBenchmarkWorkStealing()
        {
            Console.WriteLine("\nЕксперимент 4: Фінальний бенчмарк");

            int[] sizes = { 500000, 400000, 300000, 200000, 100000, 50000 };
            int runs = 20;
            int edgesPerVertex = 25;

            Console.WriteLine($"\n{"Розмір",10} {"Послід.(мс)",12} {"Парал.(мс)",12} {"Прискорення",10}");

            double totalSpeedup = 0;
            int validTests = 0;
            double maxSpeedup = 0;
            int bestSize = 0;

            foreach (int v in sizes)
            {
                var baseGraph = generator.GenerateConnectedGraph(v, v * edgesPerVertex);

                var seqGraph = new Graph(v);
                CopyGraphToGraph(baseGraph, seqGraph, v);
                var seqResult = seqGraph.DfsSequential(0);
                double seqAvg = MeasureTimeWorkStealing(() => seqGraph.DfsSequential(0), runs);

                var parGraph = new ImprovedWorkStealingDFS(v, baseGraph.AdjList);
                var parResult = parGraph.RunWorkStealingDfs(0);
                double parAvg = MeasureTimeWorkStealing(() => parGraph.RunWorkStealingDfs(0), runs);

                double speedup = seqAvg / parAvg;

                bool isValid = seqResult.Count == parResult.Count &&
                              new HashSet<int>(seqResult).SetEquals(new HashSet<int>(parResult));

                totalSpeedup += speedup;
                validTests++;

                if (isValid && speedup > maxSpeedup)
                {
                    maxSpeedup = speedup;
                    bestSize = v;
                }

                Console.WriteLine($"{v,10:N0} {seqAvg,12:F2} {parAvg,12:F2} {speedup,10:F2}");
            }

            double avgSpeedup = validTests > 0 ? totalSpeedup / validTests : 0;
            Console.WriteLine($"\nСереднє прискорення: {avgSpeedup:F2}x");
            Console.WriteLine($"Максимальне прискорення: {maxSpeedup:F2}x (на {bestSize:N0} вершинах)");
        }

        private void ExperimentCorrectnessStressTestWorkStealing()
        {
            Console.WriteLine("\nЕксперимент 5: Стрес-тест коректності");

            int[] sizes = { 1000, 5000, 10000, 20000, 50000, 100000 };
            int runs = 20;

            Console.WriteLine($"\n{"Розмір",8} {"Успішно",10} {"Час (мс)",10}");

            foreach (int vertices in sizes)
            {
                int edges = vertices * 20;
                var baseGraph = generator.GenerateConnectedGraph(vertices, edges);

                var seqGraph = new Graph(vertices);
                CopyGraphToGraph(baseGraph, seqGraph, vertices);
                var etalon = seqGraph.DfsSequential(0);
                var etalonSet = new HashSet<int>(etalon);

                int passed = 0;
                var timings = new List<double>();
                int totalMissing = 0;
                int totalExtra = 0;

                for (int r = 0; r < runs; r++)
                {
                    var parGraph = new ImprovedWorkStealingDFS(vertices, baseGraph.AdjList);

                    var sw = Stopwatch.StartNew();
                    var result = parGraph.RunWorkStealingDfs(0);
                    sw.Stop();
                    timings.Add(sw.Elapsed.TotalMilliseconds);

                    var resultSet = new HashSet<int>(result);
                    bool valid = result.Count == etalon.Count && resultSet.SetEquals(etalonSet);

                    if (valid)
                        passed++;
                    else
                    {
                        totalMissing += etalonSet.Except(resultSet).Count();
                        totalExtra += resultSet.Except(etalonSet).Count();
                    }
                }

                double avgTime = timings.Average();
                double avgMissing = totalMissing / (double)(runs - passed);
                double avgExtra = totalExtra / (double)(runs - passed);

                Console.WriteLine($"{vertices,8:N0} {passed,6}/{runs,2} {avgTime,10:F2} {avgMissing,10:F1} {avgExtra,10:F1}");
            }
        }
        private void ExperimentFindOptimalSizeWorkStealing()
        {
            Console.WriteLine("\nЕксперимент 6: Пошук оптимального розміру");

            int[] sizes = { 350000, 375000, 400000, 425000, 450000, 475000, 500000 };
            int edgesPerVertex = 10;
            int runs = 20;

            Console.WriteLine($"\n{"Розмір",10} {"Ребра",12} {"Послід.(мс)",14} {"Парал.(мс)",14} {"Прискорення",10}");

            double bestSpeedup = 0;
            int bestSize = 0;

            foreach (int v in sizes)
            {
                int totalEdges = v * edgesPerVertex;

                try
                {
                    var baseGraph = generator.GenerateConnectedGraph(v, totalEdges);

                    var seqGraph = new Graph(v);
                    CopyGraphToGraph(baseGraph, seqGraph, v);

                    seqGraph.DfsSequential(0);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    var seqTimes = new List<double>();
                    for (int i = 0; i < runs; i++)
                    {
                        var sw = Stopwatch.StartNew();
                        seqGraph.DfsSequential(0);
                        sw.Stop();
                        seqTimes.Add(sw.Elapsed.TotalMilliseconds);
                    }
                    double seqAvg = seqTimes.Average();

                    var parGraph = new ImprovedWorkStealingDFS(v, baseGraph.AdjList);

                    parGraph.RunWorkStealingDfs(0);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    var parTimes = new List<double>();
                    for (int i = 0; i < runs; i++)
                    {
                        var sw = Stopwatch.StartNew();
                        parGraph.RunWorkStealingDfs(0);
                        sw.Stop();
                        parTimes.Add(sw.Elapsed.TotalMilliseconds);
                    }
                    double parAvg = parTimes.Average();

                    double speedup = seqAvg / parAvg;

                    var seqResult = seqGraph.DfsSequential(0);
                    var parResult = parGraph.RunWorkStealingDfs(0);
                    bool isValid = seqResult.Count == parResult.Count &&
                                  new HashSet<int>(seqResult).SetEquals(new HashSet<int>(parResult));

                    if (isValid && speedup > bestSpeedup)
                    {
                        bestSpeedup = speedup;
                        bestSize = v;
                    }

                    Console.WriteLine($"{v,10:N0} {totalEdges,12:N0} {seqAvg,14:F2} {parAvg,14:F2} {speedup,10:F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка для розміру {v}: {ex.Message}");
                }
            }
            Console.WriteLine($"\nОптимальний розмір: {bestSize:N0} вершин (прискорення: {bestSpeedup:F2}x)");
        }

        private double MeasureTimeWorkStealing(Func<List<int>> action, int runs)
        {
            action();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var times = new List<double>();
            for (int i = 0; i < runs; i++)
            {
                var sw = Stopwatch.StartNew();
                action();
                sw.Stop();
                times.Add(sw.Elapsed.TotalMilliseconds);
            }

            times.Sort();
            if (times.Count > 2)
            {
                times.RemoveAt(0);
                times.RemoveAt(times.Count - 1);
            }

            return times.Average();
        }

        private void CopyGraphToGraph(Graph source, Graph target, int vertices)
        {
            for (int i = 0; i < vertices; i++)
            {
                foreach (int neighbor in source.AdjList[i])
                {
                    target.AddEdge(i, neighbor);
                }
            }
        }
    }
}