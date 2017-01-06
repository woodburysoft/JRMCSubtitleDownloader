using System;
using SubtitleDownloaderPlugin.Engine.OpenSubtitles;
using SubtitleDownloaderPlugin.Engine.SubDB;

namespace SubtitleDownloaderPlugin.Engine
{
    public class SubtitleDownloaderFactory
    {
        public virtual IExternalSubtitleDownloader CreateSubtitleDownloader(SubtitleSource subtitleSource)
        {
            switch (subtitleSource)
            {
                case SubtitleSource.OpenSubtitles:
                    return new OpenSubtitlesDownloader();
                case SubtitleSource.SubDB:
                    return new SubDBDownloader();
                default:
                    throw new ArgumentException("Unrecognised subtitle source");
            }
        }
    }
}
