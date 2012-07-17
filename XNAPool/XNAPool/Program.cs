using System;

namespace Pool
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Pool game = new Pool())
            {
                game.Run();
            }
        }
    }
#endif
}

