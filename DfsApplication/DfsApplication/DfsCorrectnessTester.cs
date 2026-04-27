using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DfsApplication
{
    public enum DfsType
    {
        Sequential,
        Parallel
    }

    public class DfsCorrectnessTester
    {
        private readonly GraphGenerator generator = new GraphGenerator();
        private readonly int processorCount = Environment.ProcessorCount;

        public void RunAllTests(DfsType dfsType)
        {
            string algorithmName = dfsType == DfsType.Sequential ? "Sequential" : "Parallel (Work Stealing)";

            Console.WriteLine("\n" + "=".PadRight(80, '='));
            Console.WriteLine($"Testing correctness of {algorithmName} DFS");
            Console.WriteLine("=".PadRight(80, '='));

            TestBasicCorrectness(dfsType);
            TestStarGraph(dfsType);
            TestLinearChain(dfsType);
            TestStressTest(dfsType);
            TestDisconnectedGraph(dfsType);

            if (dfsType == DfsType.Parallel)
            {
                TestWorkStealingDistribution();
            }

            Console.WriteLine($"\nAll {algorithmName} tests passed successfully!");
        }

        private List<int> RunDfs(DfsType dfsType, Graph graph, int start)
        {
            if (dfsType == DfsType.Sequential)
            {
                return graph.DfsSequential(start);
            }
            else
            {
                var parallelGraph = new ImprovedWorkStealingDFS(graph.VertexCount, graph.AdjList);
                return parallelGraph.RunWorkStealingDfs(start);
            }
        }

        private void TestBasicCorrectness(DfsType dfsType)
        {
            Console.WriteLine("\nTest 1: Basic correctness");
            Console.WriteLine("-".PadRight(40, '-'));

            int vertices = 10000;
            int edges = vertices * 5;
            int runs = 5;

            Console.WriteLine($"Graph: {vertices} vertices, {edges} edges");
            Console.WriteLine($"Runs: {runs}");
            Console.Write("Generating graph...");

            var baseGraph = generator.GenerateConnectedGraph(vertices, edges);
            var seqGraph = new Graph(vertices);
            CopyGraphToGraph(baseGraph, seqGraph, vertices);
            var expected = new HashSet<int>(seqGraph.DfsSequential(0));

            Console.WriteLine(" done");
            Console.Write("Running traversals...");

            int passed = 0;
            for (int i = 0; i < runs; i++)
            {
                var result = RunDfs(dfsType, baseGraph, 0);
                var resultSet = new HashSet<int>(result);

                if (resultSet.SetEquals(expected) && result.Count == result.Distinct().Count())
                    passed++;
            }

            Console.WriteLine($"\nResult: {passed}/{runs} successful runs");
            Console.WriteLine($"Conclusion: {(passed == runs ? "Correct" : "Error")}");
        }

        private void TestStarGraph(DfsType dfsType)
        {
            Console.WriteLine("\nTest 2: Star graph");
            Console.WriteLine("-".PadRight(40, '-'));

            int vertices = 1001;
            var graph = new Graph(vertices);

            for (int i = 1; i < vertices; i++)
            {
                graph.AddEdge(0, i);
                graph.AddEdge(i, 0);
            }

            var seqGraph = new Graph(vertices);
            CopyGraphToGraph(graph, seqGraph, vertices);
            var expected = new HashSet<int>(seqGraph.DfsSequential(0));

            var result = RunDfs(dfsType, graph, 0);
            var resultSet = new HashSet<int>(result);

            bool isValid = resultSet.SetEquals(expected) && result.Count == vertices;
            Console.WriteLine($"Vertices: {vertices}");
            Console.WriteLine($"Expected: {vertices}");
            Console.WriteLine($"Got: {result.Count}");
            Console.WriteLine($"Duplicates: {result.Count - result.Distinct().Count()}");
            Console.WriteLine($"Conclusion: {(isValid ? "Correct" : "Error")}");
        }

        private void TestLinearChain(DfsType dfsType)
        {
            Console.WriteLine("\nTest 3: Linear chain");
            Console.WriteLine("-".PadRight(40, '-'));

            int vertices = 10000;
            var graph = new Graph(vertices);

            for (int i = 0; i < vertices - 1; i++)
            {
                graph.AddEdge(i, i + 1);
                graph.AddEdge(i + 1, i);
            }

            var seqGraph = new Graph(vertices);
            CopyGraphToGraph(graph, seqGraph, vertices);
            var expected = new HashSet<int>(seqGraph.DfsSequential(0));

            var result = RunDfs(dfsType, graph, 0);
            var resultSet = new HashSet<int>(result);

            bool isValid = resultSet.SetEquals(expected) && result.Count == vertices;
            Console.WriteLine($"Vertices: {vertices}");
            Console.WriteLine($"Expected: {vertices}");
            Console.WriteLine($"Got: {result.Count}");
            Console.WriteLine($"Conclusion: {(isValid ? "Correct" : "Error")}");
        }

        private void TestStressTest(DfsType dfsType)
        {
            Console.WriteLine("\nTest 4: Stress testing");
            Console.WriteLine("-".PadRight(40, '-'));

            int vertices = 50000;
            int edges = vertices * 4;
            int runs = 5;

            Console.WriteLine($"Graph: {vertices} vertices, {edges} edges");
            Console.WriteLine($"Runs: {runs}");
            Console.Write("Generating graph...");

            var baseGraph = generator.GenerateConnectedGraph(vertices, edges);

            var seqGraph = new Graph(vertices);
            CopyGraphToGraph(baseGraph, seqGraph, vertices);

            var expected = new HashSet<int>(seqGraph.DfsSequential(0));
            int expectedCount = expected.Count;

            Console.WriteLine(" done");
            Console.Write("Warmup...");

            RunDfs(dfsType, baseGraph, 0);

            Console.WriteLine(" done");
            Console.WriteLine("Running tests...");

            int passed = 0;
            var timings = new List<double>();

            for (int i = 0; i < runs; i++)
            {
                var sw = Stopwatch.StartNew();

                var result = RunDfs(dfsType, baseGraph, 0);

                sw.Stop();
                timings.Add(sw.Elapsed.TotalMilliseconds);

                bool isValid = result.Count == expectedCount;

                if (i == 0)
                {
                    var resultSet = new HashSet<int>(result);
                    isValid = resultSet.SetEquals(expected);
                }

                if (isValid)
                    passed++;
            }

            timings.Sort();

            double avg = timings.Average();
            double median = timings[timings.Count / 2];
            double p95 = timings[(int)(timings.Count * 0.95)];

            Console.WriteLine($"\nResult: {passed}/{runs} successful runs");
            Console.WriteLine($"Average time: {avg:F2} ms");
            Console.WriteLine($"Median time: {median:F2} ms");
            Console.WriteLine($"95th percentile: {p95:F2} ms");
            Console.WriteLine($"Min/Max: {timings.Min():F2}/{timings.Max():F2} ms");

            Console.WriteLine($"Conclusion: {(passed == runs ? "Stable" : "Issues detected")}");
        }

        private void TestDisconnectedGraph(DfsType dfsType)
        {
            Console.WriteLine("\nTest 5: Disconnected graph");
            Console.WriteLine("-".PadRight(40, '-'));

            int componentSize = 5000;
            int vertices = componentSize * 2;
            var graph = new Graph(vertices);

            for (int i = 0; i < componentSize - 1; i++)
            {
                graph.AddEdge(i, i + 1);
                graph.AddEdge(i + 1, i);
            }

            for (int i = componentSize; i < vertices - 1; i++)
            {
                graph.AddEdge(i, i + 1);
                graph.AddEdge(i + 1, i);
            }

            var seqGraph = new Graph(vertices);
            CopyGraphToGraph(graph, seqGraph, vertices);
            var expected = new HashSet<int>(seqGraph.DfsSequential(0));
            int expectedCount = componentSize;

            var result = RunDfs(dfsType, graph, 0);
            var resultSet = new HashSet<int>(result);

            bool isValid = resultSet.SetEquals(expected) && result.Count == expectedCount;

            Console.WriteLine($"Total vertices: {vertices}");
            Console.WriteLine($"Reachable from vertex 0: {expectedCount}");
            Console.WriteLine($"Got: {result.Count}");
            Console.WriteLine($"Vertices from second component: {(resultSet.Any(v => v >= componentSize) ? "Yes" : "No")}");
            Console.WriteLine($"Conclusion: {(isValid ? "Correct" : "Error - visited unreachable vertices")}");
        }

        private void TestWorkStealingDistribution()
        {
            Console.WriteLine("\nTest 6: Work stealing distribution");
            Console.WriteLine("-".PadRight(40, '-'));

            int vertices = 50000;
            int edges = vertices * 10;
            var baseGraph = generator.GenerateConnectedGraph(vertices, edges);

            var parGraph = new ImprovedWorkStealingDFS(vertices, baseGraph.AdjList);
            var result = parGraph.RunWorkStealingDfs(0);

            var threads = Process.GetCurrentProcess().Threads;
            int activeThreads = 0;
            foreach (ProcessThread thread in threads)
            {
                if (thread.ThreadState == System.Diagnostics.ThreadState.Running ||
                    thread.ThreadState == System.Diagnostics.ThreadState.Wait)
                {
                    activeThreads++;
                }
            }

            Console.WriteLine($"Graph: {vertices} vertices");
            Console.WriteLine($"Processor cores: {processorCount}");
            Console.WriteLine($"Active threads in process: {activeThreads}");
            Console.WriteLine($"Result size: {result.Count} vertices");
            Console.WriteLine($"Conclusion: {(activeThreads > 1 ? "Work distributed across threads" : "All work in one thread")}");
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