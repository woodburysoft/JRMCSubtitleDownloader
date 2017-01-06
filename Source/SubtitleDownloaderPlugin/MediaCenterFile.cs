using System.IO;
using MediaCenter;

namespace SubtitleDownloaderPlugin
{
    internal class MediaCenterFile
    {
        internal MediaCenterFile(string filename, bool hasSuitableSubtitles, IMJFileAutomation fileAutomation)
        {
            this.Filename = filename;
            this.HasSuitableSubtitles = hasSuitableSubtitles;
            this.FileAutomation = fileAutomation;
        }

        internal IMJFileAutomation FileAutomation { get; set; }

        internal string Filename { get; private set; }

        internal bool HasSuitableSubtitles { get; set; }

        public override string ToString()
        {
            if (this.Filename == null)
            {
                return string.Empty;
            }

            return Path.GetFileName(this.Filename);
        }
    }
}
