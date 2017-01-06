using System.Collections.Generic;
using System.IO;
using Moq;
using NUnit.Framework;
using SubtitleDownloaderPlugin.Engine;
using WoodburyUtilities.Interfaces;

namespace SubtitleDownloaderPlugin.UnitTests
{
    [TestFixture]
    public class SubtitleDownloaderTests
    {        
        private SubtitleDownloader subtitleDownloader;                
        private Mock<SubtitleUtilities> subtitleUtilities;
        private Mock<IFileSystem> fileSystem;
        private Mock<IMediaInfo> mediaInfo;
        private Mock<SubtitleDownloaderFactory> subtitleDownloaderFactory;

        [SetUp]
        public void TestSetup()
        {                                    
            this.fileSystem = new Mock<IFileSystem>();            
            this.mediaInfo = new Mock<IMediaInfo>();
            this.subtitleUtilities = new Mock<SubtitleUtilities>(MockBehavior.Strict, this.fileSystem.Object, this.mediaInfo.Object);
            this.subtitleDownloaderFactory = new Mock<SubtitleDownloaderFactory>(MockBehavior.Strict);            
            this.subtitleDownloader = new SubtitleDownloader(this.subtitleUtilities.Object, this.subtitleDownloaderFactory.Object);
        }                                       
       
        [Test]
        public void GetTVShowSubtitles_should_return_false_if_the_video_already_has_suitable_subtitles()
        {
            // Arrange                           
            SubtitleLanguage language = new SubtitleLanguage("English", "eng", "en");
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");            
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, language, null)).Returns(true);

            // Act
            bool result = this.subtitleDownloader.GetTVShowSubtitles(subtitleSources, fileInfo, "Dexter", 0, 0, language, null, false);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetTVShowSubtitles_should_return_true_if_the_video_already_has_suitable_subtitles_but_ignore_existing_subtitles_is_selected()
        {
            // Arrange
            const string SeriesName = "Dexter";
            SubtitleLanguage language = new SubtitleLanguage("English", "eng", "en");         
            const ushort SeasonNumber = 1;
            const ushort EpisodeNumber = 1;
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");            
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, language, null)).Returns(true);
            Mock<IExternalSubtitleDownloader> externalSubtitleDownloader = new Mock<IExternalSubtitleDownloader>();
            externalSubtitleDownloader.Setup(x => x.GetTVShowSubtitles(fileInfo, SeriesName, SeasonNumber, EpisodeNumber, language)).Returns(new SubtitleDownloadResult { Success = true });
            this.subtitleDownloaderFactory.Setup(x => x.CreateSubtitleDownloader(It.IsAny<SubtitleSource>())).Returns(externalSubtitleDownloader.Object);            

            // Act
            bool result = this.subtitleDownloader.GetTVShowSubtitles(subtitleSources, fileInfo, SeriesName, SeasonNumber, EpisodeNumber, language, null, true);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void GetTVShowSubtitles_should_return_true_if_subtitles_are_found_in_the_primary_language()
        {
            // Arrange
            const string SeriesName = "Dexter";
            SubtitleLanguage language = new SubtitleLanguage("English", "eng", "en");
            const ushort SeasonNumber = 1;
            const ushort EpisodeNumber = 1;
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, language, null)).Returns(false);
            Mock<IExternalSubtitleDownloader> externalSubtitleDownloader = new Mock<IExternalSubtitleDownloader>();
            externalSubtitleDownloader.Setup(x => x.GetTVShowSubtitles(fileInfo, SeriesName, SeasonNumber, EpisodeNumber, language)).Returns(new SubtitleDownloadResult { Success = true });
            this.subtitleDownloaderFactory.Setup(x => x.CreateSubtitleDownloader(It.IsAny<SubtitleSource>())).Returns(externalSubtitleDownloader.Object);            

            // Act
            bool result = this.subtitleDownloader.GetTVShowSubtitles(subtitleSources, fileInfo, SeriesName, SeasonNumber, EpisodeNumber, language, null, false);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void GetTVShowSubtitles_should_return_true_if_subtitles_are_found_in_the_secondary_language()
        {
            // Arrange
            const string SeriesName = "Dexter";
            SubtitleLanguage primaryLanguage = new SubtitleLanguage("English", "eng", "en");
            SubtitleLanguage secondaryLanguage = new SubtitleLanguage("French", "fra", "fr");
            const ushort SeasonNumber = 1;
            const ushort EpisodeNumber = 1;
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, primaryLanguage, secondaryLanguage)).Returns(false);
            Mock<IExternalSubtitleDownloader> externalSubtitleDownloader = new Mock<IExternalSubtitleDownloader>();
            externalSubtitleDownloader.Setup(x => x.GetTVShowSubtitles(fileInfo, SeriesName, SeasonNumber, EpisodeNumber, primaryLanguage)).Returns(new SubtitleDownloadResult { Success = false });
            externalSubtitleDownloader.Setup(x => x.GetTVShowSubtitles(fileInfo, SeriesName, SeasonNumber, EpisodeNumber, secondaryLanguage)).Returns(new SubtitleDownloadResult { Success = true });
            this.subtitleDownloaderFactory.Setup(x => x.CreateSubtitleDownloader(It.IsAny<SubtitleSource>())).Returns(externalSubtitleDownloader.Object);

            // Act
            bool result = this.subtitleDownloader.GetTVShowSubtitles(subtitleSources, fileInfo, SeriesName, SeasonNumber, EpisodeNumber, primaryLanguage, secondaryLanguage, false);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void GetTVShowSubtitles_should_return_false_if_subtitles_are_not_found()
        {
            // Arrange
            const string SeriesName = "Dexter";
            SubtitleLanguage language = new SubtitleLanguage("English", "eng", "en");
            const ushort SeasonNumber = 1;
            const ushort EpisodeNumber = 1;
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, language, null)).Returns(false);
            Mock<IExternalSubtitleDownloader> externalSubtitleDownloader = new Mock<IExternalSubtitleDownloader>();
            externalSubtitleDownloader.Setup(x => x.GetTVShowSubtitles(fileInfo, SeriesName, SeasonNumber, EpisodeNumber, language)).Returns(new SubtitleDownloadResult { Success = false });
            this.subtitleDownloaderFactory.Setup(x => x.CreateSubtitleDownloader(It.IsAny<SubtitleSource>())).Returns(externalSubtitleDownloader.Object);  

            // Act
            bool result = this.subtitleDownloader.GetTVShowSubtitles(subtitleSources, fileInfo, SeriesName, SeasonNumber, EpisodeNumber, language, null, false);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetTVShowSubtitles_should_return_false_if_the_video_is_disk_based()
        {
            // Arrange                           
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.iso");                                    

            // Act
            bool result = this.subtitleDownloader.GetTVShowSubtitles(null, fileInfo, "Dexter", 1, 1, null, null, false);

            // Assert
            Assert.IsFalse(result);
        }                                               

        [Test]
        public void GetMovieSubtitles_should_return_false_if_the_video_already_has_suitable_subtitles()
        {
            // Arrange                           
            SubtitleLanguage language = new SubtitleLanguage("English", "eng", "en");
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, language, null)).Returns(true);

            // Act
            bool result = this.subtitleDownloader.GetMovieSubtitles(subtitleSources, fileInfo, "12345678", language, null, false);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetMovieSubtitles_should_return_true_if_the_video_already_has_suitable_subtitles_but_ignore_existing_subtitles_is_selected()
        {
            // Arrange
            const string ImdbID = "12345678";
            SubtitleLanguage language = new SubtitleLanguage("English", "eng", "en");            
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, language, null)).Returns(true);
            Mock<IExternalSubtitleDownloader> externalSubtitleDownloader = new Mock<IExternalSubtitleDownloader>();
            externalSubtitleDownloader.Setup(x => x.GetMovieSubtitles(fileInfo, ImdbID, language)).Returns(new SubtitleDownloadResult { Success = true });
            this.subtitleDownloaderFactory.Setup(x => x.CreateSubtitleDownloader(It.IsAny<SubtitleSource>())).Returns(externalSubtitleDownloader.Object);

            // Act
            bool result = this.subtitleDownloader.GetMovieSubtitles(subtitleSources, fileInfo, ImdbID, language, null, true);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void GetMovieSubtitles_should_return_true_if_subtitles_are_found_in_the_primary_language()
        {
            // Arrange
            const string ImdbID = "12345678";
            SubtitleLanguage language = new SubtitleLanguage("English", "eng", "en");            
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, language, null)).Returns(false);
            Mock<IExternalSubtitleDownloader> externalSubtitleDownloader = new Mock<IExternalSubtitleDownloader>();            
            externalSubtitleDownloader.Setup(x => x.GetMovieSubtitles(fileInfo, ImdbID, language)).Returns(new SubtitleDownloadResult { Success = true });
            this.subtitleDownloaderFactory.Setup(x => x.CreateSubtitleDownloader(It.IsAny<SubtitleSource>())).Returns(externalSubtitleDownloader.Object);

            // Act
            bool result = this.subtitleDownloader.GetMovieSubtitles(subtitleSources, fileInfo, ImdbID, language, null, false);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void GetMovieSubtitles_should_return_true_if_subtitles_are_found_in_the_secondary_language()
        {
            // Arrange
            const string ImdbID = "12345678";
            SubtitleLanguage primaryLanguage = new SubtitleLanguage("English", "eng", "en");
            SubtitleLanguage secondaryLanguage = new SubtitleLanguage("French", "fra", "fr");            
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, primaryLanguage, secondaryLanguage)).Returns(false);
            Mock<IExternalSubtitleDownloader> externalSubtitleDownloader = new Mock<IExternalSubtitleDownloader>();
            externalSubtitleDownloader.Setup(x => x.GetMovieSubtitles(fileInfo, ImdbID, primaryLanguage)).Returns(new SubtitleDownloadResult { Success = false });
            externalSubtitleDownloader.Setup(x => x.GetMovieSubtitles(fileInfo, ImdbID, secondaryLanguage)).Returns(new SubtitleDownloadResult { Success = true });
            this.subtitleDownloaderFactory.Setup(x => x.CreateSubtitleDownloader(It.IsAny<SubtitleSource>())).Returns(externalSubtitleDownloader.Object);

            // Act
            bool result = this.subtitleDownloader.GetMovieSubtitles(subtitleSources, fileInfo, ImdbID, primaryLanguage, secondaryLanguage, false);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void GetMovieSubtitles_should_return_false_if_subtitles_are_not_found()
        {
            // Arrange
            const string ImdbID = "12345678";
            SubtitleLanguage language = new SubtitleLanguage("English", "eng", "en");            
            List<SubtitleSource> subtitleSources = new List<SubtitleSource> { SubtitleSource.OpenSubtitles, SubtitleSource.SubDB };
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.exe");
            this.subtitleUtilities.Setup(x => x.DoesVideoHaveSuitableSubtitles(fileInfo.FullName, language, null)).Returns(false);
            Mock<IExternalSubtitleDownloader> externalSubtitleDownloader = new Mock<IExternalSubtitleDownloader>();
            externalSubtitleDownloader.Setup(x => x.GetMovieSubtitles(fileInfo, ImdbID, language)).Returns(new SubtitleDownloadResult { Success = false });
            this.subtitleDownloaderFactory.Setup(x => x.CreateSubtitleDownloader(It.IsAny<SubtitleSource>())).Returns(externalSubtitleDownloader.Object);

            // Act
            bool result = this.subtitleDownloader.GetMovieSubtitles(subtitleSources, fileInfo, ImdbID, language, null, false);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetMovieSubtitles_should_return_false_if_the_video_is_disk_based()
        {
            // Arrange                           
            FileInfo fileInfo = new FileInfo(@"C:\Windows\explorer.iso");

            // Act
            bool result = this.subtitleDownloader.GetMovieSubtitles(null, fileInfo, "12345678", null, null, false);

            // Assert
            Assert.IsFalse(result);
        }                                               
    }
}