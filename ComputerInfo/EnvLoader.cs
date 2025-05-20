using System.Collections.Generic;
using System.IO;

namespace ComputerInfo
{
    public static class EnvLoader
    {
        public static Dictionary<string, string> Load(string path)
        {
            var dict = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                    dict[parts[0].Trim()] = parts[1].Trim();
            }
            return dict;
        }
    }
}
