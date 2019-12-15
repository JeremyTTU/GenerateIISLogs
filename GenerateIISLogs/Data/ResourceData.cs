using GenerateIISLogs.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GenerateIISLogs.Data
{
    public static class ResourceData
    {
        private static Random _Random = new Random(RandomSeeds.GetSeed());
        private static Dictionary<string, string[]> _Data = new Dictionary<string, string[]>();

        public static void Init()
        {
            var resourceStreams = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(o => o.Contains(".Data.")).Select(o => o.Replace("GenerateIISLogs.Data.", "").Replace(".txt", ""));
            foreach (var resourceStream in resourceStreams)
                _Data.Add(resourceStream, GetStringsFromResource(resourceStream + ".txt").ToArray());
        }

        public static string GetRandomData(string resourceName)
        {
            return _Data[resourceName][_Random.Next(_Data[resourceName].Length)];
        }

        private static IEnumerable<string> GetStringsFromResource(string resourceName)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream($"GenerateIISLogs.Data.{resourceName}"))
            using (var streamReader = new StreamReader(resource))
                while (!streamReader.EndOfStream)
                    yield return streamReader.ReadLine();
        }
    }
}
