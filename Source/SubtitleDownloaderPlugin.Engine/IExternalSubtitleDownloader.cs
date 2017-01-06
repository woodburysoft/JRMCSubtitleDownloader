using System.IO;

namespace SubtitleDownloaderPlugin.Engine
{
    public interface IExternalSubtitleDownloader
    {
        SubtitleDownloadResult GetTVShowSubtitles(FileInfo videoFile, string seriesName, ushort seasonNumber, ushort episodeNumber, SubtitleLanguage language);

        SubtitleDownloadResult GetMovieSubtitles(FileInfo videoFile, string imdbID, SubtitleLanguage language);
    }
}
