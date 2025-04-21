using Labb2_ThreadsAndAsync.Interface;
using Labb2_ThreadsAndAsync.Models;

namespace Labb2_ThreadsAndAsync.Handler
{
    internal static class Handler
    {
        static List<Car> cars = new List<Car>();
        static object statusLock = new object();
        static bool raceOver = false;
        static bool winnerFound = false;
        static int startedCount = 0;
        static int nextFinishPosition = 1;
        private static readonly Random rng = new Random();

        public static async Task Run()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Clear();

            // Skapa bilar
            cars.Add(new Car { Name = "Blixten" });
            cars.Add(new Car { Name = "Turbo" });
            cars.Add(new Car { Name = "Vroom" });
            cars.Add(new Car { Name = "Inferno" });

            List<Task> tasks = new List<Task>();

            // Användarinput i en separat thread
            Task inputTask = Task.Run(() => UserInput());
            tasks.Add(inputTask);

            ConsoleInterface.WriteEvent("🏁 Tävlingen börjar!");
            ConsoleInterface.WriteEvent("");

            // Starta en task för varje bil
            foreach (var car in cars)
            {
                Task t = Task.Run(() => Drive(car));
                tasks.Add(t);
            }

            // Leta efter vinnare
            while (!raceOver)
            {
                lock (statusLock)
                {
                    var winner = cars.Find(c => c.Finished && !c.Winner && !c.Exploded);
                    if (winner != null)
                    {
                        winner.Winner = true;
                        ConsoleInterface.WriteEvent($"\n🏆 {winner.Name} vann tävlingen!");
                        winnerFound = true;
                        raceOver = true;
                    }

                    if (cars.All(c => c.Finished || c.Exploded))
                    {
                        if (!winnerFound)
                        {
                            ConsoleInterface.WriteEvent("\n💥 Alla bilar har förstörts innan mållinjen. Ingen vinnare i detta lopp.");
                        }
                        raceOver = true;
                    }
                }

                await Task.Delay(1000);
            }

            // Invänta samtliga tasks
            await Task.WhenAll(tasks);

            ConsoleInterface.WriteEvent("\n✅ Alla bilar har nått mållinjen eller förstörts.");
            ConsoleInterface.WriteEvent("🏁 Tävlingen är avslutad!");
        }

        static async Task Drive(Car car)
        {
            double speedInMetersPerSecond = car.Speed * 1000 / 3600;
            int secondsPassed = 0;

            ConsoleInterface.WriteEvent($"{car.Name} startar!");

            lock (statusLock)
            {
                startedCount++;
                if (startedCount == cars.Count)
                {
                    ConsoleInterface.WriteEvent("");
                }
            }

            try
            {
                // Medans bilen fortfarande inte kört i mål
                while (car.Distance < 5000)
                {
                    // Varje sekund
                    await Task.Delay(1000);

                    // Endast en tråd kommer åt detta åt gången för att undvika fel
                    lock (statusLock)
                    {
                        // Pausad bil hamnar i detta block
                        if (car.IsPaused)
                        {
                            // Pausad bil släpps lös när Nuvarande tid når PausadTill-tiden
                            // som sattes vid olyckan
                            if (DateTime.Now >= car.PauseUntil)
                            {
                                car.IsPaused = false;
                                ConsoleInterface.WriteEvent($"{car.Name}: Fortsätter köra efter paus!");
                            }
                            // Om bilen fortfarande är pausad så hoppar vi över koden nedan
                            // där avklarad sträcka ökar
                            continue;
                        }

                        double currentSpeed = car.Speed * 1000 / 3600;
                        car.Distance += currentSpeed;
                    }

                    // +1 sekund varje sekund
                    secondsPassed++;

                    // Leta efter ett random event var 10e sekund
                    if (secondsPassed % 10 == 0)
                        CheckForRandomEvent(car);

                    // Ge statusuppdatering varje sekund
                    if (secondsPassed % 1 == 0)
                        ConsoleInterface.WriteStatus(cars);

                    // Bilen kör i mål vid 5000 m
                    if (car.Distance >= 5000)
                    {
                        lock (statusLock)
                        {
                            car.Finished = true;
                            car.Speed = 0;

                            if (car.FinishPosition == null)
                            {
                                car.FinishPosition = nextFinishPosition++;
                            }

                            ConsoleInterface.WriteEvent($"{car.Name} har nått mållinjen!");
                        }
                        // Avsluta racet (lämna loopen)
                        break;
                    }
                }
            }
            // Block för sprängd bil
            catch (Exception ex)
            {
                ConsoleInterface.WriteEvent($"{car.Name}: {ex.Message}");
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
                // 1 på 25 att bilen sprängs
                int mineChance = rng.Next(1, 26);
                if (mineChance == 1)
                {
                    ConsoleInterface.WriteEvent($"{car.Name}: Kör på en mina! 💣 Bilen sprängs.");
                    throw new Exception("Bilen sprängdes.");
                }

                int chance = rng.Next(1, 51);

                if (chance == 1)
                {
                    ConsoleInterface.WriteEvent($"{car.Name}: Slut på bensin! ⛽ Pausar i 15 sekunder.");
                    car.IsPaused = true;
                    car.PauseUntil = DateTime.Now.AddSeconds(15);
                }
                else if (chance <= 3)
                {
                    ConsoleInterface.WriteEvent($"{car.Name}: Punktering! 🛞 Pausar i 10 sekunder.");
                    car.IsPaused = true;
                    car.PauseUntil = DateTime.Now.AddSeconds(10);
                }
                else if (chance <= 8)
                {
                    ConsoleInterface.WriteEvent($"{car.Name}: Fågel på vindrutan! 🐦 Pausar i 5 sekunder.");
                    car.IsPaused = true;
                    car.PauseUntil = DateTime.Now.AddSeconds(5);
                }
                else if (chance <= 18)
                {
                    car.Speed = Math.Max(1, car.Speed - 1);
                    ConsoleInterface.WriteEvent($"{car.Name}: Motorproblem! 🔧 Ny hastighet: {car.Speed} km/h.");
                }
            }
        }

        static async Task UserInput()
        {
            while (!raceOver)
            {
                string input = await Console.In.ReadLineAsync();
                if (string.IsNullOrEmpty(input) || input.ToLower() == "status")
                {
                    ConsoleInterface.WriteStatus(cars);
                }
            }
        }

    }
}
