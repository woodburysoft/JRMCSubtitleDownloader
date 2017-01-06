using System.Collections.Generic;
using System.IO;

namespace SubtitleDownloaderPlugin.Engine
{
    public class SubtitleDownloader
    {                
        private readonly SubtitleUtilities subtitleUtilities;
        private readonly SubtitleDownloaderFactory subtitleDownloaderFactory;

        public SubtitleDownloader(SubtitleUtilities subtitleUtilities, SubtitleDownloaderFactory subtitleDownloaderFactory)
        {                        
            this.subtitleUtilities = subtitleUtilities;
            this.subtitleDownloaderFactory = subtitleDownloaderFactory;
        }

        /// <summary>
        /// Gets subtitles for a TV show
        /// </summary>
        /// <param name="sources">The list of subtitle sources</param>
        /// <param name="video">The video file</param>
        /// <param name="seriesName">The name of the TV series</param>
        /// <param name="season">The season number</param>
        /// <param name="episode">The episode number</param>
        /// <param name="primaryLanguage">The language for which to retrieve subtitles</param>
        /// <param name="secondaryLanguage">The language for which to retrieve subtitles if none were found for the primary language</param>
        /// <param name="ignoreExistingSubtitles">Get subtitles even if the TV show has existing subtitles</param>
        public bool GetTVShowSubtitles(IEnumerable<SubtitleSource> sources, FileInfo video, string seriesName, ushort season, ushort episode, SubtitleLanguage primaryLanguage, SubtitleLanguage secondaryLanguage, bool ignoreExistingSubtitles)
        {
            if (SubtitleUtilities.IsVideoDiskBased(video.FullName))
            {
                return false;
            }

            // Do nothing if there are existing subtitles and we are NOT ignoring existing subtitles
            if (!ignoreExistingSubtitles && this.subtitleUtilities.DoesVideoHaveSuitableSubtitles(video.FullName, primaryLanguage, secondaryLanguage))
            {
                return false;
            }           

            // Check each source for subtitles in turn
            foreach (SubtitleSource source in sources)
            {                
                IExternalSubtitleDownloader externalSubtitleDownloader = this.subtitleDownloaderFactory.CreateSubtitleDownloader(source);

                // If subtitles are found in the primary language then don't look any further
                SubtitleDownloadResult result = externalSubtitleDownloader.GetTVShowSubtitles(video, seriesName, season, episode, primaryLanguage);

                if (result.Success)
                {
                    SubtitleUtilities.UpdateCache(result.SubtitleFilename);
                    return true;
                }
                                
                if (secondaryLanguage != null)
                {
                    // If subtitles are found in the secondary language then don't look any further
                    result = externalSubtitleDownloader.GetTVShowSubtitles(video, seriesName, season, episode, secondaryLanguage);

                    if (result.Success)
                    {
                        SubtitleUtilities.UpdateCache(result.SubtitleFilename);
                        return true;
                    }       
                }
            }

            return false;
        }

        /// <summary>
        /// Get subtitles for a movie
        /// </summary>
        /// <param name="sources">The list of subtitle sources</param>
        /// <param name="video">The video file</param>
        /// <param name="imdbID">The IMDB ID</param>
        /// <param name="primaryLanguage">The language for which to retrieve subtitles</param>
        /// <param name="secondaryLanguage">The language for which to retrieve subtitles if none were found for the primary language</param>
        /// <param name="ignoreExistingSubtitles">Get subtitles even if the movie has existing subtitles</param>
        public bool GetMovieSubtitles(IEnumerable<SubtitleSource> sources, FileInfo video, string imdbID, SubtitleLanguage primaryLanguage, SubtitleLanguage secondaryLanguage, bool ignoreExistingSubtitles)
        {
            if (SubtitleUtilities.IsVideoDiskBased(video.FullName))
            {
                return false;
            }

            // Do nothing if there are existing subtitles and we are NOT ignoring existing subtitles
            if (!ignoreExistingSubtitles && this.subtitleUtilities.DoesVideoHaveSuitableSubtitles(video.FullName, primaryLanguage, secondaryLanguage))
            {
                return false;
            }                                   

            foreach (SubtitleSource source in sources)
            {
                IExternalSubtitleDownloader externalSubtitleDownloader = this.subtitleDownloaderFactory.CreateSubtitleDownloader(source);

                // If subtitles are found in the primary language then don't look any further
                SubtitleDownloadResult result = externalSubtitleDownloader.GetMovieSubtitles(video, imdbID, primaryLanguage);

                if (result.Success)
                {
                    SubtitleUtilities.UpdateCache(result.SubtitleFilename);
                    return true;
                }

                if (secondaryLanguage != null)
                {
                    // If subtitles are found in the secondary language then don't look any further
                    result = externalSubtitleDownloader.GetMovieSubtitles(video, imdbID, secondaryLanguage);

                    if (result.Success)
                    {
                        SubtitleUtilities.UpdateCache(result.SubtitleFilename);
                        return true;
                    }
                }
            }

            return false;
        }        
    }    
}