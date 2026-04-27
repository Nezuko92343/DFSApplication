using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DfsApplication
{
    public class ExperimentProgram
    {
        static void Main(string[] args)
        {
            Console.Title = "Паралельний DFS - Експериментальне дослідження";

            Console.WriteLine("     Експериментальне дослідження ефективності паралельного DFS  ");

            try
            {
                var currentProcess = Process.GetCurrentProcess();
                currentProcess.PriorityClass = ProcessPriorityClass.High;
                Console.WriteLine($"Пріоритет процесу: High");

                int coreCount = Environment.ProcessorCount;
                int coreMask = (1 << coreCount) - 1;

                if (coreCount <= 32)
                {
                    currentProcess.ProcessorAffinity = (IntPtr)coreMask;
                    Console.WriteLine($"Affinity процесу: 0x{coreMask:X2} (всі {coreCount} ядер)");
                }
                else
                {
                    Console.WriteLine($"Affinity не встановлено (більше 32 ядер)");
                }
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine($"Affinity не підтримується на цій платформі");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка налаштування affinity: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine(new string('-', 80));
            Console.WriteLine();

            try
            {
                var experiments = new CompleteExperiments();
                experiments.RunAllExperiments();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nКРИТИЧНА ПОМИЛКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nНатисніть будь-яку клавішу для завершення...");
            Console.ResetColor();
            Console.ReadKey();
        }
    }
}