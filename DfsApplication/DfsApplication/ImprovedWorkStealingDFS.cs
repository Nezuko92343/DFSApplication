using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DfsApplication
{
    public class ImprovedWorkStealingDFS
    {
        private readonly int vertexCount;
        private readonly int[][] flatAdjacency;
        private readonly int[] visited;
        private readonly int processorCount;

        private const int ParallelThreshold = 10000;

        private int activeTasks = 0;

        private static readonly ThreadLocal<Random> threadRandom =
            new ThreadLocal<Random>(() =>
                new Random(Environment.TickCount * Thread.CurrentThread.ManagedThreadId));

        public ImprovedWorkStealingDFS(int vertices, List<int>[] adjList)
        {
            vertexCount = vertices;
            processorCount = Environment.ProcessorCount;
            visited = new int[vertices];

            var flatList = new List<int>();
            var offsets = new int[vertices + 1];

            for (int i = 0; i < vertices; i++)
            {
                offsets[i] = flatList.Count;
                flatList.AddRange(adjList[i]);
            }
            offsets[vertices] = flatList.Count;

            flatAdjacency = new int[vertices][];

            for (int i = 0; i < vertices; i++)
            {
                int start = offsets[i];
                int end = offsets[i + 1];

                flatAdjacency[i] = new int[end - start];

                for (int j = 0; j < end - start; j++)
                    flatAdjacency[i][j] = flatList[start + j];
            }
        }

        private class WorkStealingDeque
        {
            private readonly List<int> deque = new List<int>();
            private readonly object locker = new object();

            public void Push(int item)
            {
                lock (locker)
                    deque.Add(item);
            }

            public bool TryPop(out int item)
            {
                lock (locker)
                {
                    if (deque.Count > 0)
                    {
                        item = deque[^1];
                        deque.RemoveAt(deque.Count - 1);
                        return true;
                    }
                }

                item = -1;
                return false;
            }

            public bool TrySteal(out int item)
            {
                lock (locker)
                {
                    if (deque.Count > 0)
                    {
                        item = deque[0];
                        deque.RemoveAt(0);
                        return true;
                    }
                }

                item = -1;
                return false;
            }

            public bool IsEmpty()
            {
                lock (locker)
                    return deque.Count == 0;
            }
        }

        public List<int> RunWorkStealingDfs(int start, int maxDegreeOfParallelism = -1)
        {
            if (vertexCount < ParallelThreshold)
                return RunSequentialDfs(start);

            Array.Clear(visited, 0, vertexCount);
            activeTasks = 0;

            int workers = maxDegreeOfParallelism > 0
                ? maxDegreeOfParallelism
                : processorCount;

            var deques = new WorkStealingDeque[workers];
            var results = new List<int>[workers];

            for (int i = 0; i < workers; i++)
                deques[i] = new WorkStealingDeque();

            visited[start] = 1;
            deques[0].Push(start);
            Interlocked.Increment(ref activeTasks);

            Parallel.For(0, workers,
                new ParallelOptions { MaxDegreeOfParallelism = workers },
                workerId =>
                {
                    var localResult = new List<int>();
                    var rand = threadRandom.Value;
                    var spinner = new SpinWait();

                    while (true)
                    {
                        int current;

                        if (!deques[workerId].TryPop(out current))
                        {
                            bool stolen = false;
                            int startOffset = rand.Next(workers);

                            for (int i = 0; i < workers; i++)
                            {
                                int victim = (startOffset + i) % workers;

                                if (victim == workerId) continue;

                                if (deques[victim].TrySteal(out current))
                                {
                                    stolen = true;
                                    break;
                                }
                            }

                            if (!stolen)
                            {
                                bool allEmpty = true;

                                for (int i = 0; i < workers && allEmpty; i++)
                                    allEmpty = deques[i].IsEmpty();

                                if (Volatile.Read(ref activeTasks) == 0 && allEmpty)
                                    break;

                                spinner.SpinOnce();
                                continue;
                            }
                        }

                        localResult.Add(current);

                        var neighbors = flatAdjacency[current];

                        for (int i = neighbors.Length - 1; i >= 0; i--)
                        {
                            int neighbor = neighbors[i];

                            if (Interlocked.CompareExchange(ref visited[neighbor], 1, 0) == 0)
                            {
                                Interlocked.Increment(ref activeTasks);
                                deques[workerId].Push(neighbor);
                            }
                        }

                        Interlocked.Decrement(ref activeTasks);
                    }

                    results[workerId] = localResult;
                });

            return results.SelectMany(x => x).ToList();
        }

        private List<int> RunSequentialDfs(int start)
        {
            var visitedLocal = new bool[vertexCount];
            var stack = new Stack<int>();
            var result = new List<int>();

            stack.Push(start);

            while (stack.Count > 0)
            {
                int current = stack.Pop();

                if (visitedLocal[current]) continue;

                visitedLocal[current] = true;
                result.Add(current);

                var neighbors = flatAdjacency[current];

                for (int i = neighbors.Length - 1; i >= 0; i--)
                    stack.Push(neighbors[i]);
            }

            return result;
        }
    }
}