using System;
using System.Diagnostics;

namespace DfsApplication
{
    public class ProgramSimple
    {
        static void Main(string[] args)
        {
            Console.Title = "DFS - Correctness Testing";

            Console.WriteLine("DFS Correctness Testing");
            Console.WriteLine($"Processor: {Environment.ProcessorCount} logical cores");
            Console.WriteLine($"OS: {Environment.OSVersion}");
            Console.WriteLine($".NET: {Environment.Version}");
            Console.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            Console.WriteLine();

            // Налаштування продуктивності
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                currentProcess.PriorityClass = ProcessPriorityClass.High;

                int coreCount = Environment.ProcessorCount;
                int coreMask = (1 << coreCount) - 1;

                if (coreCount <= 32)
                {
                    currentProcess.ProcessorAffinity = (IntPtr)coreMask;
                    Console.WriteLine($"Process affinity: 0x{coreMask:X2} (all {coreCount} cores)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Affinity setup error: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();

            var tester = new DfsCorrectnessTester();

            // Меню вибору
            while (true)
            {
                Console.WriteLine("\nSelect algorithm to test:");
                Console.WriteLine("1. Sequential DFS");
                Console.WriteLine("2. Parallel DFS (Work Stealing)");
                Console.WriteLine("3. Test both");
                Console.WriteLine("0. Exit");
                Console.Write("\nYour choice: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        Console.Clear();
                        tester.RunAllTests(DfsType.Sequential);
                        break;
                    case "2":
                        Console.Clear();
                        tester.RunAllTests(DfsType.Parallel);
                        break;
                    case "3":
                        Console.Clear();
                        Console.WriteLine("Testing Sequential DFS first...");
                        tester.RunAllTests(DfsType.Sequential);
                        Console.WriteLine("\n" + new string('=', 80));
                        Console.WriteLine("Testing Parallel DFS...");
                        tester.RunAllTests(DfsType.Parallel);
                        break;
                    case "0":
                        Console.WriteLine("Exiting...");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }
    }
}