using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookComputing.XmlRpc;
using SubtitleDownloaderPlugin.Engine.OpenSubtitles;
using WoodburyUtilities.Interfaces;

namespace SubtitleDownloaderPlugin.Engine
{
    public class SubtitleUtilities
    {        
        private static readonly IList<string> ValidSubtitleFileExtensions;
        private static IDictionary<string, IList<string>> cachedFolderFiles;

        private readonly IFileSystem fileSystem;
        private readonly IMediaInfo mediaInfo;

        static SubtitleUtilities()
        {            
            ValidSubtitleFileExtensions = new List<string> { ".srt", ".sub", ".idx", ".smi", ".sami", ".ssa", ".ass" };

            IOpenSubtitles openSubtitles = XmlRpcProxyGen.Create<IOpenSubtitles>();
            OpenSubtitlesLanguageHeader languageHeader = openSubtitles.GetSubLanguages();
            OpenSubtitlesLanguage[] languages = languageHeader.data ?? new OpenSubtitlesLanguage[0];
            AvailableLanguages = languages.Select(language => new SubtitleLanguage(language.LanguageName, language.SubLanguageID, language.ISO639)).ToList();
        }        

        public SubtitleUtilities(IFileSystem fileSystem, IMediaInfo mediaInfo)
        {
            this.fileSystem = fileSystem;
            this.mediaInfo = mediaInfo;
        }

        /// <summary>
        /// The list of available subtitle languages
        /// </summary>
        public static IEnumerable<SubtitleLanguage> AvailableLanguages { get; private set; }        

        /// <summary>
        /// Initialise folder file caching
        /// </summary>
        public static void StartFolderFileCaching()
        {
            if (cachedFolderFiles == null)
            {
                cachedFolderFiles = new Dictionary<string, IList<string>>();
            }
            else
            {
                cachedFolderFiles.Clear();
            }
        }        

        /// <summary>
        /// Checks if the video is disk based
        /// </summary>
        /// <param name="filename">The filename of the video</param>
        /// <returns>True if the video is disk based, false if not</returns>
        public static bool IsVideoDiskBased(string filename)
        {
            // Blu-ray disc
            if (filename.IndexOf("index.bluray", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                return true;
            }

            // DVD disc
            if (filename.IndexOf("VIDEO_TS.dvd", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                return true;
            }

            // ISO image
            string fileExtension = Path.GetExtension(filename);

            if (!string.IsNullOrWhiteSpace(fileExtension) && fileExtension.Equals(".iso", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the expected filename for a subtitle file
        /// </summary>
        /// <param name="videoFilename">The filename of the video</param>
        /// <param name="language">The language code</param>
        /// <param name="extension">The subtitle file extension including the dot</param>
        /// <returns>The filename</returns>
        public static string GetSubtitleFilename(string videoFilename, SubtitleLanguage language, string extension)
        {
            string parentDirectory = Path.GetDirectoryName(videoFilename);

            if (string.IsNullOrWhiteSpace(parentDirectory))
            {
                throw new ArgumentException("Parent directory not found");
            }

            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(videoFilename);            
            string fullFilename = string.Format(@"{0}.{1}{2}", filenameWithoutExtension, language.Code, extension);
            return Path.Combine(parentDirectory, fullFilename);
        }

        /// <summary>
        /// Checks if the video already has suitable subtitles
        /// </summary>
        /// <returns>True if the video already has suitable subtitles, false if not</returns>
        public virtual bool DoesVideoHaveSuitableSubtitles(string filename, SubtitleLanguage primaryLanguage, SubtitleLanguage secondaryLanguage)
        {                                           
            IEnumerable<SubtitleFile> subtitles = this.GetSubtitlesForVideoFile(filename);
            return subtitles.Any(x => x.Language == null || x.Language.Code.Equals(primaryLanguage.Code) || (secondaryLanguage != null && (x.Language == null || x.Language.Code.Equals(secondaryLanguage.Code))));                       
        }                

        /// <summary>
        /// Gets the list of existing subtitle files for a video file
        /// </summary>
        /// <param name="videoFilename">The video filename</param>
        /// <returns>The list of languages</returns>
        public IEnumerable<SubtitleFile> GetSubtitlesForVideoFile(string videoFilename)
        {
            IList<SubtitleFile> subtitleFiles = new List<SubtitleFile>();

            string videoFileParentFolder = Path.GetDirectoryName(videoFilename);

            if (videoFileParentFolder == null)
            {
                return subtitleFiles;
            }
            
            IEnumerable<string> files = this.GetFilesInFolder(videoFileParentFolder);

            string videoFilenameWithoutExtension = Path.GetFileNameWithoutExtension(videoFilename);                        

            foreach (string file in files)
            {
                string potentialSubtitleFileWithoutExtension = Path.GetFileNameWithoutExtension(file);

                if (potentialSubtitleFileWithoutExtension == null || !potentialSubtitleFileWithoutExtension.StartsWith(videoFilenameWithoutExtension))
                {
                    continue;
                }                

                string potentialSubtitleFileExtension = Path.GetExtension(file);

                if (!ValidSubtitleFileExtensions.Contains(potentialSubtitleFileExtension))
                {
                    continue;
                }                               
                
                string code = potentialSubtitleFileWithoutExtension.Split('.').Last();
                SubtitleLanguage language = AvailableLanguages.FirstOrDefault(x => x.Code.Equals(code, StringComparison.InvariantCultureIgnoreCase));
                subtitleFiles.Add(new SubtitleFile(file, language));
            }      
                  
            // Search for embedded subtitles            
            IEnumerable<string> subtitleLanguageCodes = this.mediaInfo.GetEmbeddedSubtitleLanguages(videoFilename);
            
            foreach (string subtitleLanguageCode in subtitleLanguageCodes)
            {                
                SubtitleLanguage subtitleLanguage = AvailableLanguages.FirstOrDefault(x => x.ISO639Code.Equals(subtitleLanguageCode, StringComparison.InvariantCultureIgnoreCase));
                subtitleFiles.Add(new SubtitleFile("Embedded", subtitleLanguage));
            }

            return subtitleFiles;
        }

        /// <summary>
        /// Update the folder files cache with a new subtitle filename
        /// </summary>
        /// <param name="subtitleFilename">The subtitle filename</param>
        internal static void UpdateCache(string subtitleFilename)
        {
            if (cachedFolderFiles != null)
            {
                string parentFolderPath = Path.GetDirectoryName(subtitleFilename);
                cachedFolderFiles[parentFolderPath].Add(subtitleFilename);
            }
        }

        /// <summary>
        /// Get the list of files in a folder
        /// </summary>
        /// <param name="folderPath">The folder path</param>
        /// <returns>The list of files</returns>
        private IEnumerable<string> GetFilesInFolder(string folderPath)
        {            
            IList<string> files;

            if (cachedFolderFiles != null && cachedFolderFiles.ContainsKey(folderPath))
            {
                files = cachedFolderFiles[folderPath];
            }
            else
            {
                files = this.fileSystem.EnumerateFiles(folderPath).ToList();

                if (cachedFolderFiles != null)
                {
                    cachedFolderFiles.Add(folderPath, files);
                }
            }

            return files;
        }
    }
}
