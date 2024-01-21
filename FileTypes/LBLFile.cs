using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEarthLauncher.FileTypes
{
    public class LBLFile : IFile
    {
        public readonly Dictionary<string, string> Values;

        public readonly char Separator;

        public LBLFile(string _path, char _separator) : base(_path)
        {
            Separator = _separator;

            string[] lines = File.ReadAllLines(Path);

            Values = new Dictionary<string, string>();

            string[] split;
            for (int i = 0; i < lines.Length; i++)
            {
                split = lines[i].Split(Separator);
                if (split.Length > 1 && split[0] != string.Empty)
                    Values[split[0]] = split[1];
            }
        }

        public override void Save()
        {
            List<string> lines = new List<string>();

            foreach (KeyValuePair<string, string> item in Values) 
                    lines.Add(item.Key + Separator + item.Value);

            File.WriteAllLines(Path, lines.ToArray());
        }

        public string this[string name]
        {
            get => Values[name];
            set => Values[name] = value;
        }
    }
}
