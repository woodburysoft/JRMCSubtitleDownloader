using System;
using System.IO;

namespace SubtitleDownloaderPlugin.Engine.OpenSubtitles
{
    internal static class OpenSubtitlesHashGenerator
    {
        internal static byte[] ComputeVideoHash(string filename)
        {
            byte[] result;

            using (Stream input = File.OpenRead(filename))
            {
                result = ComputeVideoHash(input);                
            }

            return result;
        }

        private static byte[] ComputeVideoHash(Stream input)
        {
            long streamsize = input.Length;
            long lhash = streamsize;

            long i = 0;
            byte[] buffer = new byte[sizeof(long)];

            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }

            input.Position = Math.Max(0, streamsize - 65536);            
            i = 0;
            
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }

            input.Close();
            byte[] result = BitConverter.GetBytes(lhash);
            Array.Reverse(result);

            return result;
        }
    }
}
