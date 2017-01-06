using System.IO;
using System.Net;
using System.Text;
using WoodburyUtilities;

namespace SubtitleDownloaderPlugin.Engine.SubDB
{
    public class SubDBDownloader : IExternalSubtitleDownloader
    {        
        private FileInfo video;        
        private SubtitleLanguage language;        

        public SubtitleDownloadResult GetTVShowSubtitles(FileInfo videoFile, string seriesName, ushort seasonNumber, ushort episodeNumber, SubtitleLanguage language)
        {
            this.video = videoFile;            
            this.language = language;

            string subtitleFilename = this.DownloadSubtitles();

            return new SubtitleDownloadResult
                   {
                       Success = subtitleFilename != null,
                       SubtitleFilename = subtitleFilename
                   };
        }

        public SubtitleDownloadResult GetMovieSubtitles(FileInfo videoFile, string imdbID, SubtitleLanguage language)
        {
            this.video = videoFile;
            this.language = language;

            string subtitleFilename = this.DownloadSubtitles();

            return new SubtitleDownloadResult
                   {
                       Success = subtitleFilename != null,
                       SubtitleFilename = subtitleFilename
                   };
        }

        /// <summary>
        /// Download the subtitles for the video file
        /// </summary>
        /// <returns>True if the subtitles are found and downloaded, false if not</returns>
        private string DownloadSubtitles()
        {
            // Calculate hash value for video
            byte[] videoHashByteArray = SubDBHashGenerator.ComputeVideoHash(this.video.FullName);

            Conversion conversionUtilities = new Conversion();
            string videoHash = conversionUtilities.ToHexadecimal(videoHashByteArray);                        
         
            // Attempt to download the subtitles               
            string url = string.Format("http://api.thesubdb.com/?action=download&hash={0}&language={1}", videoHash, this.language.ISO639Code);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";            
            request.ContentType = "text/html; charset=ISO-8859-1";
            request.UserAgent = "SubDB/1.0 (JRMCSubtitleDownloader/0.3; http://yabb.jriver.com/interact/index.php?topic=75967.0)";

            WebResponse webResponse;
            string content;            

            try
            {
                webResponse = request.GetResponse();
            }
            catch (WebException)
            {
                // If an exception happens here then the subtitles can't be downloaded for some reason, probably that they don't exist
                return null;
            }

            // Read the response content
            using (HttpWebResponse httpWebResponse = (HttpWebResponse)webResponse)
            {
                if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (Stream responseStream = httpWebResponse.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding(1252));
                        content = reader.ReadToEnd();
                    }
                }
                else
                {
                    return null;
                }
            }

            // Write the response content to file            
            string subtitleFilename = SubtitleUtilities.GetSubtitleFilename(this.video.FullName, this.language, ".srt");
            File.WriteAllText(subtitleFilename, content, Encoding.GetEncoding(1252));

            return subtitleFilename;
        }               
    }
}
