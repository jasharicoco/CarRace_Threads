using Labb2_ThreadsAndAsync.Models;

namespace Labb2_ThreadsAndAsync.Interface
{
    public static class ConsoleInterface
    {
        private static int eventRow = 0;
        private static readonly int statusColumn = 60;
        private static readonly int maxWidth = Console.WindowWidth;
        private static readonly object consoleLock = new object();

        // Händelser på vänster sida
        public static void WriteEvent(string message)
        {
            lock (consoleLock)
            {
                // Börja på 0,x
                Console.SetCursorPosition(0, eventRow);
                // Ta 55 "kolumner"
                Console.WriteLine(message.PadRight(55));
                // Ny rad varje gång
                eventRow++;
                // Rensa "event-konsolen" när alla rader är tagna
                if (eventRow >= Console.WindowHeight - 1)
                {
                    // Rensa konsolen
                    Console.Clear();
                    // Börja utskrifterna på rad nr 2
                    eventRow = 2;
                    // Skriv följande på rad nr 0
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("🏁 Tävlingen fortsätter!\n");
                }
            }
        }

        // Rensa statuskolumnen (körs mellan varje statusuppdatering)
        public static void ClearStatusColumn()
        {
            lock (consoleLock)
            {
                for (int row = 0; row < Console.WindowHeight; row++)
                {
                    Console.SetCursorPosition(statusColumn, row);
                    Console.Write(new string(' ', maxWidth - statusColumn));
                }
            }
        }

        // Statusuppdateringar på höger sida
        public static void WriteStatus(List<Car> cars)
        {
            lock (consoleLock)
            {
                ClearStatusColumn();
                Console.SetCursorPosition(statusColumn, 0);
                Console.WriteLine("📊 Statusuppdatering:");
                int statusRow = 2;

                if (cars.All(c => c.Finished || c.Exploded))
                {
                    Console.SetCursorPosition(statusColumn, statusRow);
                    Console.WriteLine("🚫 Inga bilar kvar i racet.");
                }
                else
                {
                    // Sortera bilarna baserat på Distance i fallande ordning
                    var sortedCars = cars.OrderByDescending(c => c.Distance).ToList();

                    foreach (var car in sortedCars)
                    {
                        Console.SetCursorPosition(statusColumn, statusRow);
                        if (car.Exploded)
                            Console.WriteLine($"{car.Name}: ❌ Bilen är sprängd.");
                        else if (car.Finished)
                            Console.WriteLine($"{car.Name}: ✅ Har nått mållinjen.");
                        else if (car.IsPaused)
                            Console.WriteLine($"{car.Name}: ⏸ Pausad - 📍 {car.Distance:F1} m - 🚗 {car.Speed:F1} km/h");
                        else
                            Console.WriteLine($"{car.Name}: 📍 {car.Distance:F1} m - 🚗 {car.Speed:F1} km/h");
                        statusRow++;
                    }
                }
            }
        }

    }
}
