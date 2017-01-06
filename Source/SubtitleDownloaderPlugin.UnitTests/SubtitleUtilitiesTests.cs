using System.Collections.Generic;
using System.IO;
using Moq;
using NUnit.Framework;
using SubtitleDownloaderPlugin.Engine;
using WoodburyUtilities.Interfaces;

namespace SubtitleDownloaderPlugin.UnitTests
{
    [TestFixture]
    public class SubtitleUtilitiesTests
    {
        private const string PrimaryLanguageName = "English";
        private const string PrimaryLanguageCode = "eng";
        private const string PrimaryLanguageISO639Code = "en";
        private const string SecondaryLanguageName = "Finnish";
        private const string SecondaryLanguageCode = "fin";
        private const string SecondaryLanguageISO639Code = "fi";

        private SubtitleUtilities subtitleUtilities;
        private Mock<IFileSystem> fileSystem;
        private Mock<IMediaInfo> mediaInfo;
        private SubtitleLanguage primaryLanguage = new SubtitleLanguage(PrimaryLanguageName, PrimaryLanguageCode, PrimaryLanguageISO639Code);
        private SubtitleLanguage secondaryLanguage = new SubtitleLanguage(SecondaryLanguageName, SecondaryLanguageCode, SecondaryLanguageISO639Code);

        [SetUp]
        public void SetupTest()
        {
            this.fileSystem = new Mock<IFileSystem>();
            this.mediaInfo = new Mock<IMediaInfo>();
            this.subtitleUtilities = new SubtitleUtilities(this.fileSystem.Object, this.mediaInfo.Object);
            this.primaryLanguage = new SubtitleLanguage(PrimaryLanguageName, PrimaryLanguageCode, PrimaryLanguageISO639Code);
            this.secondaryLanguage = new SubtitleLanguage(SecondaryLanguageName, SecondaryLanguageCode, SecondaryLanguageISO639Code);
        }        

        [TestCase("srt")]
        [TestCase("sub")]
        [TestCase("idx")]
        [TestCase("smi")]
        [TestCase("sami")]
        [TestCase("ssa")]
        [TestCase("ass")]
        public void DoesVideoHaveSuitableSubtitles_should_return_true_if_an_external_subtitle_file_with_unknown_language_exists(string subtitleExtension)
        {
            // Arrange
            const string Filename = @"C:\Dexter.s01e01.mkv";
            string subtitleFilename = Path.ChangeExtension(Filename, subtitleExtension);
            this.fileSystem.Setup(x => x.EnumerateFiles(@"C:\")).Returns(new List<string> { subtitleFilename });

            // Act
            bool result = this.subtitleUtilities.DoesVideoHaveSuitableSubtitles(Filename, this.primaryLanguage, this.secondaryLanguage);

            // Assert
            Assert.That(result, Is.True);
        }

        [TestCase("srt")]
        [TestCase("sub")]
        [TestCase("idx")]
        [TestCase("smi")]
        [TestCase("sami")]
        [TestCase("ssa")]
        [TestCase("ass")]
        public void DoesVideoHaveSuitableSubtitles_should_return_true_if_an_external_subtitle_file_for_the_primary_language_exists(string subtitleExtension)
        {
            // Arrange
            const string Filename = @"C:\Dexter.s01e01.mkv";            
            string folderPath = Path.GetDirectoryName(Filename);
            string subtitleFilename = Path.GetFileNameWithoutExtension(Filename);
            subtitleFilename = string.Format("{0}.{1}.{2}", subtitleFilename, PrimaryLanguageCode, subtitleExtension);
            this.fileSystem.Setup(x => x.EnumerateFiles(folderPath)).Returns(new List<string> { subtitleFilename });            

            // Act
            bool result = this.subtitleUtilities.DoesVideoHaveSuitableSubtitles(Filename, this.primaryLanguage, this.secondaryLanguage);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void DoesVideoHaveSuitableSubtitles_should_return_true_if_embedded_subtitles_for_the_primary_language_exist()
        {
            // Arrange
            const string Filename = @"C:\Dexter.s01e01.mkv";            
            this.mediaInfo.Setup(x => x.GetEmbeddedSubtitleLanguages(Filename)).Returns(new List<string> { PrimaryLanguageCode });

            // Act
            bool result = this.subtitleUtilities.DoesVideoHaveSuitableSubtitles(Filename, this.primaryLanguage, this.secondaryLanguage);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void DoesVideoHaveSuitableSubtitles_should_return_true_if_embedded_subtitles_for_the_secondary_language_exist()
        {
            // Arrange
            const string Filename = @"C:\Dexter.s01e01.mkv";                        
            this.mediaInfo.Setup(x => x.GetEmbeddedSubtitleLanguages(Filename)).Returns(new List<string> { SecondaryLanguageISO639Code });

            // Act
            bool result = this.subtitleUtilities.DoesVideoHaveSuitableSubtitles(Filename, this.primaryLanguage, this.secondaryLanguage);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void DoesVideoHaveSuitableSubtitles_should_return_false_if_no_suitable_subtitles_exist()
        {
            // Arrange
            const string Filename = @"C:\Dexter.s01e01.mkv";            

            // Act
            bool result = this.subtitleUtilities.DoesVideoHaveSuitableSubtitles(Filename, this.primaryLanguage, this.secondaryLanguage);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsVideoDiskBased_should_return_true_for_a_ripped_blu_ray_disc()
        {
            // Arrange

            // Act
            bool result = SubtitleUtilities.IsVideoDiskBased(@"C:\2001 A Space Odyssey\BDMV\index.bluray;1");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsVideoDiskBased_should_return_true_for_a_ripped_DVD_disc()
        {
            // Arrange

            // Act
            bool result = SubtitleUtilities.IsVideoDiskBased(@"C:\2001 A Space Odyssey\VIDEO_TS.dvd");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsVideoDiskBased_should_return_true_for_an_ISO_image()
        {
            // Arrange

            // Act
            bool result = SubtitleUtilities.IsVideoDiskBased(@"C:\2001 A Space Odyssey.iso");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsVideoDiskBased_should_return_false_for_an_non_disk_file()
        {
            // Arrange

            // Act
            bool result = SubtitleUtilities.IsVideoDiskBased(@"C:\2001 A Space Odyssey.mkv");

            // Assert
            Assert.That(result, Is.False);
        }

        [TestCase(@"C:\Dexter.s01e01.mkv", @"C:\Dexter.s01e01.eng.srt")]
        [TestCase(@"Y:\TV Shows\How I Met Your Mother\Season 09\S09E09.avi", @"Y:\TV Shows\How I Met Your Mother\Season 09\S09E09.eng.srt")]
        public void GetSubtitleFilename_should_return_the_correct_filename(string videoFilename, string expectedSubtitleFilename)
        {
            // Arrange

            // Act
            string result = SubtitleUtilities.GetSubtitleFilename(videoFilename, this.primaryLanguage, ".srt");

            // Assert
            Assert.That(result, Is.EqualTo(expectedSubtitleFilename));
        }        
    }
}
