using System.IO;
using System.Security.Cryptography;

namespace SubtitleDownloaderPlugin.Engine.SubDB
{
    internal static class SubDBHashGenerator
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
            byte[] startBuffer = new byte[65536];
            byte[] endBuffer = new byte[65536];

            input.Read(startBuffer, 0, 65536);
            long offset = input.Length - 65536;
            input.Position = offset;
            input.Read(endBuffer, 0, 65536);

            byte[] resultBuffer = Combine(startBuffer, endBuffer);

            MD5 md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(resultBuffer);
        }

        private static byte[] Combine(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            System.Buffer.BlockCopy(a, 0, c, 0, a.Length);
            System.Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }
    }
}
