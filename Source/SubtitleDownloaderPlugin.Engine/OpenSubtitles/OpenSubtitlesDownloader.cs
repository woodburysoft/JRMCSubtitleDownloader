using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CookComputing.XmlRpc;
using WoodburyUtilities;

namespace SubtitleDownloaderPlugin.Engine.OpenSubtitles
{
    public class OpenSubtitlesDownloader : IExternalSubtitleDownloader
    {
        private const string UserName = "";
        private const string Password = "";
        private const string APILanguage = "en";
        private const string UserAgentId = "OS Test User Agent";

        private readonly IOpenSubtitles openSubtitles;

        private string token;
        private OpenSubtitlesRecord selectedSubtitles;
        private FileInfo video;
        private string series;
        private ushort season;
        private ushort episode;
        private string imdb;
        private SubtitleLanguage language;        

        public OpenSubtitlesDownloader()
        {
            this.openSubtitles = XmlRpcProxyGen.Create<IOpenSubtitles>();
        }

        public SubtitleDownloadResult GetTVShowSubtitles(FileInfo videoFile, string seriesName, ushort seasonNumber, ushort episodeNumber, SubtitleLanguage language)
        {
            this.video = videoFile;
            this.series = seriesName;
            this.season = seasonNumber;
            this.episode = episodeNumber;
            this.language = language;

            this.token = this.LogIn();

            try
            {
                this.FindTVShowSubtitles();

                if (this.selectedSubtitles != null)
                {
                    string subtitleFilename = this.DownloadSubtitles();
                    return new SubtitleDownloadResult { Success = true, SubtitleFilename = subtitleFilename };
                }

                return new SubtitleDownloadResult { Success = false };
            }
            finally
            {
                this.LogOut();
            }
        }

        public SubtitleDownloadResult GetMovieSubtitles(FileInfo videoFile, string imdbID, SubtitleLanguage language)
        {
            this.video = videoFile;            
            this.imdb = imdbID;
            this.language = language;

            this.token = this.LogIn();

            try
            {
                this.FindMovieSubtitles();

                if (this.selectedSubtitles != null)
                {
                    string subtitleFilename = this.DownloadSubtitles();
                    return new SubtitleDownloadResult { Success = true, SubtitleFilename = subtitleFilename };
                }

                return new SubtitleDownloadResult { Success = false };
            }
            finally
            {
                this.LogOut();
            }
        }

        /// <summary>
        /// Find the best matching subtitles
        /// </summary>
        /// <param name="recordHeader">The list of subtitles</param>
        /// <returns></returns>
        private static OpenSubtitlesRecord FindBestMatchSubtitles(OpenSubtitlesRecordHeader recordHeader)
        {
            // Check that some subtitles have been found
            if (recordHeader.data == null || !recordHeader.data.Any())
            {
                return null;
            }

            // Exclude subtitles that have been marked as bad
            IList<OpenSubtitlesRecord> subtitles = recordHeader.data.Where(x => x.SubBad == null || x.SubBad.ToString().Equals("0", StringComparison.InvariantCultureIgnoreCase)).ToList();

            // Check that some non-bad subtitles have been found
            if (!subtitles.Any())
            {
                return null;
            }

            // Return the most downloaded subtitle
            OpenSubtitlesRecord result = subtitles.OrderByDescending(x => x.SubDownloadsCnt != null ? int.Parse(x.SubDownloadsCnt.ToString()) : 0).First();

            return result;
        }

        /// <summary>
        /// Checks the return status from an OpenSubtitles API call
        /// </summary>
        /// <param name="openSubtitlesResultValue">The OpenSubtitles API return object</param>
        private static void CheckReturnStatus(IOpenSubtitlesResult openSubtitlesResultValue)
        {
            if (!openSubtitlesResultValue.status.Equals("200 OK"))
            {
                throw new ArgumentException(string.Format("An error of '{0}' occurred was returned from the OpenSubtitles API", openSubtitlesResultValue.status));
            }
        }

        /// <summary>
        /// Find the best subtitles for the selected movie
        /// </summary>
        private void FindMovieSubtitles()
        {
            this.FindSubtitlesByHash();

            if (this.selectedSubtitles != null)
            {
                return;
            }

            this.FindSubtitlesByImdbID();
        }

        /// <summary>
        /// Find the subtitles for the selected video file using the IMDB ID
        /// </summary>
        private void FindSubtitlesByImdbID()
        {
            OpenSubtitlesImdbParameters[] searchParameters = new[]
                {
                    new OpenSubtitlesImdbParameters()
                        {
                            imdbid = this.imdb,
                            sublanguageid = this.language.Code
                        }
                };

            OpenSubtitlesRecordHeader recordHeader;

            try
            {
                recordHeader = this.openSubtitles.SearchSubtitles(this.token, searchParameters);
            }
            catch (XmlRpcTypeMismatchException)
            {
                // If no matching subtitles are found then "false" is returned in the data property of the OpenSubtitlesRecordHeader object which causes a cast mismatch exception
                return;
            }

            CheckReturnStatus(recordHeader);

            this.selectedSubtitles = FindBestMatchSubtitles(recordHeader);                       
        }

        /// <summary>
        /// Log in to the OpenSubtitles API
        /// </summary>
        /// <returns>The token</returns>
        private string LogIn()
        {
            OpenSubtitlesToken tokenHeader = this.openSubtitles.LogIn(UserName, Password, APILanguage, UserAgentId);
            CheckReturnStatus(tokenHeader);
            return tokenHeader.token;
        }

        /// <summary>
        /// Find the best subtitles for the selected TV show
        /// </summary>
        private void FindTVShowSubtitles()
        {
            this.FindSubtitlesByHash();

            if (this.selectedSubtitles != null)
            {
                return;
            }

            this.FindSubtitlesBySeriesSeasonEpisode();
        }

        /// <summary>
        /// Find the subtitles for the selected video file using the video hash
        /// </summary>        
        private void FindSubtitlesByHash()
        {
            // Calculate the size of the video file in bytes
            long videoSize = this.GetVideoSize();

            // Calculate the hash value for the video file
            byte[] videoHashByteArray = OpenSubtitlesHashGenerator.ComputeVideoHash(this.video.FullName);

            Conversion conversionUtilities = new Conversion();
            string videoHash = conversionUtilities.ToHexadecimal(videoHashByteArray);

            OpenSubtitlesHashSearchParameters[] searchParameters = new[]
                {
                    new OpenSubtitlesHashSearchParameters()
                        {
                            moviehash = videoHash,
                            moviebytesize = videoSize.ToString(CultureInfo.InvariantCulture),
                            sublanguageid = this.language.Code
                        }
                };

            OpenSubtitlesRecordHeader recordHeader;

            try
            {
                recordHeader = this.openSubtitles.SearchSubtitles(this.token, searchParameters);
            }
            catch (XmlRpcTypeMismatchException)
            {
                // If no matching subtitles are found then "false" is returned in the data property of the OpenSubtitlesRecordHeader object which causes a cast mismatch exception
                return;
            }

            CheckReturnStatus(recordHeader);

            // Select the first subtitles if any have been found
            if (recordHeader.data != null && recordHeader.data.Any())
            {
                this.selectedSubtitles = recordHeader.data.First();                
            }
        }

        /// <summary>
        /// Get the size of the current video file
        /// </summary>
        /// <returns>The size of the video file in bytes</returns>
        private long GetVideoSize()
        {
            using (FileStream videoFileStream = new FileStream(this.video.FullName, FileMode.Open, FileAccess.Read))
            {
                return videoFileStream.Length;
            }
        }

        /// <summary>
        /// Find the subtitles for the selected video file using the series name, season number and episode number
        /// </summary>
        private void FindSubtitlesBySeriesSeasonEpisode()
        {
            OpenSubtitlesSeriesSeasonEpisodeSearchParameters[] searchParameters = new[]
                {
                    new OpenSubtitlesSeriesSeasonEpisodeSearchParameters()
                        {
                            query = this.series,
                            season = this.season,
                            episode = this.episode,
                            sublanguageid = this.language.Code
                        }
                };

            OpenSubtitlesRecordHeader recordHeader;

            try
            {
                recordHeader = this.openSubtitles.SearchSubtitles(this.token, searchParameters);
            }
            catch (XmlRpcTypeMismatchException)
            {
                // If no matching subtitles are found then "false" is returned in the data property of the OpenSubtitlesRecordHeader object which causes a cast mismatch exception
                return;
            }

            CheckReturnStatus(recordHeader);

            this.selectedSubtitles = FindBestMatchSubtitles(recordHeader);
        }        

        /// <summary>
        /// Download the subtitles from the OpenSubtitles web site
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "http://stackoverflow.com/questions/3831676/ca2202-how-to-solve-this-case/3839419#3839419")]
        private string DownloadSubtitles()
        {
            // Get the path to the folder in which the video is stored
            string videoFileFolder = Path.GetDirectoryName(this.video.FullName);

            if (videoFileFolder == null)
            {
                throw new ArgumentException(Strings.UnableToDetermineVideoFileParentFolderError);
            }

            // Generate the name of the subtitle zip file
            string subtitleZipFilename = string.Format("{0}.gz", this.selectedSubtitles.SubFileName);
            subtitleZipFilename = Path.Combine(videoFileFolder, subtitleZipFilename);

            // Download the zipped subtitle file
            OpenSubtitlesSubtitleHeader subtitles = this.openSubtitles.DownloadSubtitles(this.token, new object[] { this.selectedSubtitles.IDSubtitleFile });
            byte[] result = Convert.FromBase64String(subtitles.data.First().data);
            File.WriteAllBytes(subtitleZipFilename, result);

            FileInfo subtitleZipFile = new FileInfo(subtitleZipFilename);
            string subtitleFilename;

            using (FileStream subtitleZipFileStream = subtitleZipFile.OpenRead())
            {
                // Generate the name of the subtitle file as it must match the name of the video file
                string originalFilename = Path.GetFileNameWithoutExtension(subtitleZipFilename);
                string subtitleExtension = Path.GetExtension(originalFilename);                                
                subtitleFilename = SubtitleUtilities.GetSubtitleFilename(this.video.FullName, this.language, subtitleExtension);

                // Unzip the subtitle file
                using (FileStream decompressedFileStream = File.Create(subtitleFilename))
                {
                    using (GZipStream decompressionStream = new GZipStream(subtitleZipFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }

                File.Delete(subtitleZipFilename);
            }

            return subtitleFilename;
        }

        /// <summary>
        /// Log out of the OpenSubtitles API
        /// </summary>
        private void LogOut()
        {
            OpenSubtitlesLogOutResult result = this.openSubtitles.LogOut(this.token);
            CheckReturnStatus(result);
        }        
    }
}
