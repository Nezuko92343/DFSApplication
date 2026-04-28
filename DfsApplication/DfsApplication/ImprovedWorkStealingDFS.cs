using System;
using System.Collections.Concurrent;
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
                new Random(Guid.NewGuid().GetHashCode()));

        private readonly ConcurrentDictionary<int, byte> usedThreadIds = new();

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
            private readonly List<int> deque = new();
            private readonly object locker = new();

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
        }

        public List<int> RunWorkStealingDfs(int start, int maxDegreeOfParallelism = -1)
        {
            usedThreadIds.Clear();
            usedThreadIds.TryAdd(Thread.CurrentThread.ManagedThreadId, 0);

            if (vertexCount < ParallelThreshold)
                return RunSequentialDfs(start);

            Array.Clear(visited, 0, vertexCount);

            int workers = maxDegreeOfParallelism > 0
                ? maxDegreeOfParallelism
                : processorCount;

            var deques = new WorkStealingDeque[workers];
            var results = new List<int>[workers];

            for (int i = 0; i < workers; i++)
            {
                deques[i] = new WorkStealingDeque();
                results[i] = new List<int>();
            }

            visited[start] = 1;

            deques[0].Push(start);
            Interlocked.Increment(ref activeTasks);

            Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers }, workerId =>
            {
                usedThreadIds.TryAdd(Thread.CurrentThread.ManagedThreadId, 0);

                var localResult = results[workerId];
                var rand = threadRandom.Value!;
                var spinner = new SpinWait();

                while (true)
                {
                    int current;

                    bool gotWork =
                        deques[workerId].TryPop(out current) ||
                        Steal(workers, workerId, rand, deques, out current);

                    if (!gotWork)
                    {
                        if (Volatile.Read(ref activeTasks) == 0)
                            break;

                        spinner.SpinOnce();
                        continue;
                    }

                    Interlocked.Decrement(ref activeTasks);

                    localResult.Add(current);

                    var neighbors = flatAdjacency[current];

                    for (int i = neighbors.Length - 1; i >= 0; i--)
                    {
                        int neighbor = neighbors[i];

                        if (Interlocked.CompareExchange(ref visited[neighbor], 1, 0) == 0)
                        {
                            deques[workerId].Push(neighbor);
                            Interlocked.Increment(ref activeTasks);
                        }
                    }
                }
            });

            return results.SelectMany(x => x).ToList();
        }

        private bool Steal(int workers, int workerId, Random rand,
            WorkStealingDeque[] deques, out int item)
        {
            int startOffset = rand.Next(workers);

            for (int i = 0; i < workers; i++)
            {
                int victim = (startOffset + i) % workers;

                if (victim == workerId)
                    continue;

                if (deques[victim].TrySteal(out item))
                    return true;
            }

            item = -1;
            return false;
        }

    
        public int GetUsedThreadCount() => usedThreadIds.Count;

        public string GetUsedThreadDetails() =>
            string.Join(", ", usedThreadIds.Keys.OrderBy(x => x));
    }
}
