using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEarthLauncher.FileTypes
{
    public abstract class IFile
    {
        public readonly string Path;

        public IFile(string _path)
        {
            Path = _path;
        }

        public abstract void Save();
    }
}
