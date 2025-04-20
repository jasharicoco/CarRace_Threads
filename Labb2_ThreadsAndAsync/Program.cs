namespace Labb2_ThreadsAndAsync
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Starta tävlingen
            await Handler.Handler.Run();
        }
    }
}
