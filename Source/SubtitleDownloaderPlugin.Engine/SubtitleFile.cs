namespace SubtitleDownloaderPlugin.Engine
{
    public class SubtitleFile
    {
        public SubtitleFile(string filename, SubtitleLanguage language)
        {
            this.Filename = filename;
            this.Language = language;
        }

        public string Filename { get; private set; }

        public SubtitleLanguage Language { get; private set; }

        public override string ToString()
        {            
            return this.Language != null ? string.Format("{0} ({1})", this.Language.Name, this.Filename) : string.Format("Unknown ({0})", this.Filename);
        }
    }
}