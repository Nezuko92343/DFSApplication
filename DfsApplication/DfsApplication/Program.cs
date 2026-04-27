using System;
using System.Diagnostics;

namespace DfsApplication
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "DFS - Аналіз графів";
            Console.WriteLine("DFS Аналіз графів");
            Console.WriteLine("=".PadRight(50, '='));

            while (true)
            {
                Console.WriteLine("\nМЕНЮ:");
                Console.WriteLine("1. Завантажити граф з файлу");
                Console.WriteLine("2. Створити тестовий граф (генератор)");
                Console.WriteLine("3. Виконати DFS обхід");
                Console.WriteLine("4. Показати граф");
                Console.WriteLine("5. Зберегти граф у файл");
                Console.WriteLine("0. Вихід");
                Console.Write("\nВаш вибір: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        LoadGraphFromFile();
                        break;
                    case "2":
                        GenerateTestGraph();
                        break;
                    case "3":
                        RunDfs();
                        break;
                    case "4":
                        if (currentGraph != null)
                            currentGraph.PrintGraph();
                        else
                            Console.WriteLine("Граф не завантажено");
                        break;
                    case "5":
                        SaveGraphToFile();
                        break;
                    case "0":
                        Console.WriteLine("Вихід...");
                        return;
                    default:
                        Console.WriteLine("Невірний вибір");
                        break;
                }
            }
        }

        private static Graph currentGraph;
        private static string currentFilePath;

        private static void LoadGraphFromFile()
        {
            Console.Write("Введіть шлях до файлу (наприклад, graph.txt): ");
            string path = Console.ReadLine();

            try
            {
                currentGraph = Graph.LoadFromFile(path);
                currentFilePath = path;
                Console.WriteLine($"Граф успішно завантажено: {currentGraph.VertexCount} вершин");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка: {ex.Message}");
            }
        }

        private static void GenerateTestGraph()
        {
            Console.Write("Кількість вершин: ");
            int v = int.Parse(Console.ReadLine());
            Console.Write("Кількість ребер: ");
            int e = int.Parse(Console.ReadLine());

            var generator = new GraphGenerator();
            currentGraph = generator.GenerateConnectedGraph(v, e);
            currentFilePath = null;
            Console.WriteLine($"Тестовий граф створено: {v} вершин, {e} ребер");
        }

        private static void RunDfs()
        {
            if (currentGraph == null)
            {
                Console.WriteLine("Спочатку завантажте або створіть граф");
                return;
            }

            Console.Write("Стартова вершина (0 за замовчуванням): ");
            string input = Console.ReadLine();
            int start = string.IsNullOrEmpty(input) ? 0 : int.Parse(input);

            if (start < 0 || start >= currentGraph.VertexCount)
            {
                Console.WriteLine($"Вершина {start} поза межами (0-{currentGraph.VertexCount - 1})");
                return;
            }

            Console.WriteLine("\nDFS обхід:");
            var sw = Stopwatch.StartNew();
            var result = currentGraph.DfsSequential(start);
            sw.Stop();

            Console.WriteLine($"Результат: {string.Join(" -> ", result)}");
            Console.WriteLine($"Час: {sw.Elapsed.TotalMilliseconds:F2} мс");
            Console.WriteLine($"Відвідано вершин: {result.Count}");
        }

        private static void SaveGraphToFile()
        {
            if (currentGraph == null)
            {
                Console.WriteLine("Немає графу для збереження");
                return;
            }

            Console.Write("Введіть шлях для збереження (enter для перезапису): ");
            string path = Console.ReadLine();

            if (string.IsNullOrEmpty(path))
                path = currentFilePath ?? "graph_saved.txt";

            currentGraph.SaveToFile(path);
            currentFilePath = path;
        }
    }
}