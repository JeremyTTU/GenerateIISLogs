using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenerateIISLogs.Utility
{
    public static class RandomSeeds
    {
        private static Random _Random = new Random(Environment.TickCount);
        private static ConcurrentQueue<int> _SeedSak = new ConcurrentQueue<int>();
        private static Timer _Checker = null;
        public static int MAX_SEED = 1000000;

        public static void Init()
        {
            C.History($"Generating {MAX_SEED} of Random Seeds...");
            for (var x = 0; x < MAX_SEED; x++)
                _SeedSak.Enqueue(_Random.Next(int.MaxValue));

            _Checker = new Timer(AddSeeds, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        private static void AddSeeds(object o)
        {
            if (_SeedSak.Count > 10000) return;

            for (var x = 0; x < MAX_SEED; x++)
                _SeedSak.Enqueue(_Random.Next(int.MaxValue));
        }

        public static int GetSeed()
        {
            if (_SeedSak.TryDequeue(out int seed))
                return seed;
            else
                throw new Exception("Could not retrieve random seed");
        }
    }
}
