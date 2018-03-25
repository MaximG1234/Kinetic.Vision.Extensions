using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Reads a stream fully and returns a byte array containing the stream contents
        /// </summary>
        /// <param name="input">Any stream</param>
        /// <returns>A byte array containing the stream contents</returns>
        public static byte[] ReadFully(this Stream input)
        {
            input.Position = 0;
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
