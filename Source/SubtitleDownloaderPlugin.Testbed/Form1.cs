using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using SubtitleDownloaderPlugin.Engine;
using WoodburyUtilities;
using WoodburyUtilities.Enumerations;
using WoodburyUtilities.Interfaces;

namespace SubtitleDownloaderPlugin.Testbed
{
    public partial class Form1 : Form
    {
        private readonly ILogger logger;

        public Form1()
        {
            this.logger = new Logger(@"C:\Subtitle Downloader Log.txt", new HashSet<LogLevel> { LogLevel.Error, LogLevel.Info });
            this.InitializeComponent();            
        }

        private static IEnumerable<SubtitleSource> GetSubtitleSources()
        {
            var test = new List<SubtitleSource> { SubtitleSource.SubDB, SubtitleSource.OpenSubtitles };
            return test;
        }

        private void Button1Click(object sender, System.EventArgs e)
        {
            FileInfo videoFile = new FileInfo(@"\\nas1\media\TV Shows\Seinfeld\Season 1\Seinfeld S01E01.mkv");
            SubtitleDownloader subtitleDownloader = new SubtitleDownloader(new SubtitleUtilities(new FileSystem(), new MediaInfo()), new SubtitleDownloaderFactory());
            subtitleDownloader.GetTVShowSubtitles(GetSubtitleSources(), videoFile, "Seinfeld", 1, 1, new SubtitleLanguage("English", "eng", "en"), null, false);
        }        

        private void Button2Click(object sender, System.EventArgs e)
        {
            ////MessageBox.Show(SubtitleUtilities.ToHexadecimal(SubDBHashGenerator.ComputeVideoHash(@"c:\dexter.mp4")));
        }

        private void Button3Click(object sender, System.EventArgs e)
        {
            FileInfo videoFile = new FileInfo(@"D:\Video\Kids Movies\Aliens In The Attic\Aliens In The Attic.mkv");
            SubtitleDownloader subtitleDownloader = new SubtitleDownloader(new SubtitleUtilities(new FileSystem(), new MediaInfo()), new SubtitleDownloaderFactory());
            subtitleDownloader.GetMovieSubtitles(GetSubtitleSources(), videoFile, "0272338", new SubtitleLanguage("English", "eng", "en"), null, false);            
        }
    }
}
