using Labb2_ThreadsAndAsync.Models;

namespace Labb2_ThreadsAndAsync.Handler
{
    internal static class Handler
    {
        static List<Car> cars = new List<Car>();
        static object statusLock = new object();
        static bool raceOver = false;
        static bool winnerFound = false;
        static int startedCount = 0; // Variabel för att skriva ut en tom rad efter alla bilar startat
        private static readonly Random rng = new Random(); // Random-genererare för olyckor
        private static int eventRow = 0; // Håller reda på aktuell rad för händelser
        private static readonly int statusColumn = 60; // Startkolumn för statusuppdateringar
        private static readonly int maxWidth = Console.WindowWidth; // Konsolens bredd
        private static readonly object consoleLock = new object(); // Lås för konsolutskrifter

        // Hjälpfunktion för att skriva händelser i vänster kolumn
        private static void WriteEvent(string message)
        {
            lock (consoleLock)
            {
                Console.SetCursorPosition(0, eventRow);
                Console.WriteLine(message.PadRight(55)); // Fyll ut till kolumn 55
                eventRow++;
                // Om vi når botten av konsolen, rulla eller rensa
                if (eventRow >= Console.WindowHeight - 1)
                {
                    Console.Clear();
                    eventRow = 0;
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("🏁 Tävlingen fortsätter!\n");
                }
            }
        }

        // Hjälpfunktion för att rensa höger kolumn
        private static void ClearStatusColumn()
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

        public static void Run()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Clear();
            eventRow = 0;

            // Initiera bilar
            cars.Add(new Car { Name = "Blixten" });
            cars.Add(new Car { Name = "Turbo" });
            cars.Add(new Car { Name = "Vroom" });
            cars.Add(new Car { Name = "Inferno" });

            List<Thread> threads = new List<Thread>();

            // Skapa trådar för varje bil
            foreach (var car in cars)
            {
                Thread t = new Thread(() => Drive(car));
                threads.Add(t);
            }

            // Starta användarinput i separat tråd
            Thread inputThread = new Thread(UserInput);
            inputThread.Start();

            WriteEvent("🏁 Tävlingen börjar!\n");

            // Starta alla biltrådar
            foreach (var t in threads)
                t.Start();

            // Kontrollera vinnare under racets gång
            while (!raceOver)
            {
                lock (statusLock)
                {
                    var winner = cars.Find(c => c.Finished && !c.Winner && !c.Exploded);
                    if (winner != null)
                    {
                        winner.Winner = true;
                        WriteEvent($"\n🏆 {winner.Name} vann tävlingen!");
                        winnerFound = true;
                        raceOver = true;
                    }

                    if (cars.All(c => c.Finished || c.Exploded))
                    {
                        if (!winnerFound)
                        {
                            WriteEvent("\n💥 Alla bilar har förstörts innan mållinjen. Ingen vinnare i detta lopp.");
                        }
                        raceOver = true;
                    }
                }

                Thread.Sleep(1000);
            }

            // Vänta på att alla trådar ska avslutas
            foreach (var t in threads)
                t.Join();

            WriteEvent("\n✅ Alla bilar har nått mållinjen eller förstörts.");
            WriteEvent("🏁 Tävlingen är avslutad!");
        }

        static void Drive(Car car)
        {
            double speedInMetersPerSecond = car.Speed * 1000 / 3600;
            int secondsPassed = 0;

            WriteEvent($"{car.Name} startar!");

            lock (statusLock)
            {
                startedCount++;
                if (startedCount == cars.Count)
                {
                    WriteEvent(""); // Tom rad efter sista bilen startat
                }
            }

            try
            {
                while (car.Distance < 5000)
                {
                    Thread.Sleep(1000);

                    lock (statusLock)
                    {
                        // Kontrollera om bilen är pausad
                        if (car.IsPaused)
                        {
                            if (DateTime.Now >= car.PauseUntil)
                            {
                                // Pausen är över
                                car.IsPaused = false;
                                WriteEvent($"{car.Name}: Fortsätter köra efter paus!");
                            }
                            // Hoppa över avståndsuppdatering om bilen är pausad
                            continue;
                        }

                        double currentSpeed = car.Speed * 1000 / 3600; // Uppdateras varje sekund
                        car.Distance += currentSpeed;
                    }

                    secondsPassed++;

                    if (secondsPassed % 10 == 0)
                        CheckForRandomEvent(car);

                    if (car.Distance >= 5000)
                    {
                        lock (statusLock)
                        {
                            car.Finished = true;
                            car.Speed = 0;
                            WriteEvent($"{car.Name} har nått mållinjen!");
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteEvent($"{car.Name}: {ex.Message}");
                lock (statusLock)
                {
                    if (ex.Message == "Bilen sprängdes.")
                    {
                        car.Exploded = true;
                        car.Finished = true;
                    }
                }
            }
        }

        static void CheckForRandomEvent(Car car)
        {
            lock (statusLock)
            {
                int mineChance = rng.Next(1, 1001); // 1 på 1 000 chans
                if (mineChance == 1)
                {
                    WriteEvent($"{car.Name}: Kör på en mina! 💣 Bilen sprängs.");
                    throw new Exception("Bilen sprängdes.");
                }

                int chance = rng.Next(1, 51); // 1 till 50

                if (chance == 1)
                {
                    WriteEvent($"{car.Name}: Slut på bensin! ⛽ Pausar i 15 sekunder.");
                    car.IsPaused = true;
                    car.PauseUntil = DateTime.Now.AddSeconds(15); // 15 sekunder från nu
                }
                else if (chance <= 3)
                {
                    WriteEvent($"{car.Name}: Punktering! 🛞 Pausar i 10 sekunder.");
                    car.IsPaused = true;
                    car.PauseUntil = DateTime.Now.AddSeconds(10);
                }
                else if (chance <= 8)
                {
                    WriteEvent($"{car.Name}: Fågel på vindrutan! 🐦 Pausar i 5 sekunder.");
                    car.IsPaused = true;
                    car.PauseUntil = DateTime.Now.AddSeconds(5);
                }
                else if (chance <= 18)
                {
                    car.Speed = Math.Max(1, car.Speed - 1);
                    WriteEvent($"{car.Name}: Motorproblem! 🔧 Ny hastighet: {car.Speed} km/h.");
                }
            }
        }

        static void UserInput()
        {
            while (!raceOver)
            {
                string input = Console.ReadLine();
                if (input == "" || input.ToLower() == "status")
                {
                    lock (statusLock)
                    {
                        lock (consoleLock)
                        {
                            ClearStatusColumn(); // Rensa höger kolumn
                            Console.SetCursorPosition(statusColumn, 0);
                            Console.WriteLine("📊 Statusuppdatering:");
                            int statusRow = 1;

                            if (cars.All(c => c.Finished || c.Exploded))
                            {
                                Console.SetCursorPosition(statusColumn, statusRow);
                                Console.WriteLine("🚫 Inga bilar kvar i racet.");
                            }
                            else
                            {
                                foreach (var car in cars)
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
        }
    }
}