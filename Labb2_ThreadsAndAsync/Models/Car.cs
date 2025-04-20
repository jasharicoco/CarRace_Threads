namespace Labb2_ThreadsAndAsync.Models
{
    public class Car
    {
        public string Name { get; set; }
        public double Distance { get; set; } = 0; // Körd sträcka
        public double Speed { get; set; } = 120; // km/h
        public bool Finished { get; set; } = false; // Färdig
        public bool Exploded { get; set; } = false; // Exploderad
        public bool Winner { get; set; } = false; // Vinnare
        public bool IsPaused { get; set; } // Indikerar om bilen är pausad
        public DateTime PauseUntil { get; set; } // Tidpunkt när pausen slutar

    }
}
