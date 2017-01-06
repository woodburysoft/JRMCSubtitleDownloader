namespace SubtitleDownloaderPlugin.Engine
{
    public class SubtitleLanguage
    {
        public SubtitleLanguage(string name, string code, string iso639Code)
        {
            this.Name = name;
            this.Code = code;
            this.ISO639Code = iso639Code;
        }

        /// <summary>
        /// The name of the language
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The code of the language
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// The ISO639 code of the language
        /// </summary>
        public string ISO639Code { get; private set; }                        

        public override string ToString()
        {
            return this.Name;
        }                        
    }
}
