using System;
using System.Collections.Generic;

namespace DfsApplication
{
    public class GraphGenerator
    {
        private readonly Random random = new Random(42);

        public Graph GenerateConnectedGraph(int vertices, int edges)
        {
            long minEdges = vertices - 1;
            long maxEdges = (long)vertices * (vertices - 1) / 2;

            if (edges < minEdges)
                throw new ArgumentException($"Кількість ребер ({edges}) менша за мінімум для зв'язного графу ({minEdges})");

            if (edges > maxEdges)
            {
                Console.WriteLine($"  ПОПЕРЕДЖЕННЯ: Запит {edges} ребер, але максимум {maxEdges:N0}");
                Console.WriteLine($"  Використовується максимум: {maxEdges:N0} ребер");
                edges = (int)maxEdges;
            }

            var graph = new Graph(vertices);
            var connectedList = new List<int> { 0 };

            for (int i = 1; i < vertices; i++)
            {
                int parent = connectedList[random.Next(connectedList.Count)];
                graph.AddEdge(parent, i);
                graph.AddEdge(i, parent);
                connectedList.Add(i);
            }

            long additionalEdges = edges - (vertices - 1);

            int maxAdditional = Math.Min(10000000, (int)additionalEdges); 

            for (int i = 0; i < maxAdditional; i++)
            {
                int src = random.Next(vertices);
                int dst = random.Next(vertices);
                if (src != dst)
                {
                    graph.AddEdge(src, dst);
                    graph.AddEdge(dst, src);
                }
            }

            return graph;
        }
    }
}
