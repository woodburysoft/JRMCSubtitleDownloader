using System;
using SubtitleDownloaderPlugin.Engine;

namespace SubtitleDownloaderPlugin
{
    internal class SubtitleSourceItem
    {
        internal SubtitleSourceItem(SubtitleSource source, int position, bool selected)
        {
            this.Source = source;
            this.Position = position;
            this.Selected = selected;
        }

        internal SubtitleSourceItem(SubtitleSource source, bool selected)
        {
            this.Source = source;
            this.Selected = selected;
        }

        internal SubtitleSource Source { get; private set; }

        internal int Position { get; set; }

        internal bool Selected { get; set; }

        public override string ToString()
        {
            switch (this.Source)
            {
                case SubtitleSource.OpenSubtitles:
                    return "OpenSubtitles.org";
                case SubtitleSource.SubDB:
                    return "SubDB";
                default:
                    throw new ArgumentException("Unknown subtitle source");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SubtitleSourceItem)
            {
                return this.Source.Equals(((SubtitleSourceItem)obj).Source);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.Source.GetHashCode();
        }
    }
}
