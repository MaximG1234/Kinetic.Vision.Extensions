using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Utilities
{
    public static class PathUtilities
    {
        public static string GetLocalPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

    }
}
