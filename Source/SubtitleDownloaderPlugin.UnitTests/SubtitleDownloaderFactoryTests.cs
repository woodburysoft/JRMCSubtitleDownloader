using System;
using NUnit.Framework;
using SubtitleDownloaderPlugin.Engine;
using SubtitleDownloaderPlugin.Engine.OpenSubtitles;
using SubtitleDownloaderPlugin.Engine.SubDB;

namespace SubtitleDownloaderPlugin.UnitTests
{
    [TestFixture]
    public class SubtitleDownloaderFactoryTests
    {
        private SubtitleDownloaderFactory subtitleDownloaderFactory;

        [SetUp]
        public void TestSetup()
        {
            this.subtitleDownloaderFactory = new SubtitleDownloaderFactory();
        }

        [TestCase(SubtitleSource.OpenSubtitles, typeof(OpenSubtitlesDownloader))]
        [TestCase(SubtitleSource.SubDB, typeof(SubDBDownloader))]        
        public void CreateSubtitleDownloader_should_return_the_correct_subtitle_downloader(SubtitleSource subtitleSource, Type expectedReturnType)
        {
            // Arrange

            // Act
            IExternalSubtitleDownloader result = this.subtitleDownloaderFactory.CreateSubtitleDownloader(subtitleSource);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.GetType(), Is.EqualTo(expectedReturnType));
        }

        [Test]
        public void CreateSubtitleDownloader_should_throw_an_exception_if_an_invalid_subtitle_source_is_supplied()
        {
            // Arrange

            // Act + Assert
            Assert.That(() => this.subtitleDownloaderFactory.CreateSubtitleDownloader((SubtitleSource)99999), Throws.ArgumentException.With.Message.EqualTo("Unrecognised subtitle source"));
        }
    }
}