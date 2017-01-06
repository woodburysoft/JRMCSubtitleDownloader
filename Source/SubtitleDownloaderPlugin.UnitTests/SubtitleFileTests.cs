using NUnit.Framework;
using SubtitleDownloaderPlugin.Engine;

namespace SubtitleDownloaderPlugin.UnitTests
{
    [TestFixture]
    public class SubtitleFileTests
    {
        private const string Filename = @"c:\Dexter.s01e01.srt";
        private const string LanguageName = "English";
        private const string LanguageCode = "eng";
        private const string LanguageISO639Code = "en";

        private SubtitleFile subtitleFile;

        [SetUp]
        public void TestSetup()
        {
            SubtitleLanguage subtitleLanguage = new SubtitleLanguage(LanguageName, LanguageCode, LanguageISO639Code);
            this.subtitleFile = new SubtitleFile(Filename, subtitleLanguage);
        }

        [Test]
        public void ToString_should_return_a_correctly_formatted_string()
        {
            // Arrange

            // Act
            string result = this.subtitleFile.ToString();

            // Assert
            Assert.That(result, Is.EqualTo(string.Format("{0} ({1})", LanguageName, Filename)));
        }

        [Test]
        public void ToString_should_return_a_correctly_formatted_string_if_the_language_is_null()
        {
            // Arrange
            this.subtitleFile = new SubtitleFile(Filename, null);

            // Act
            string result = this.subtitleFile.ToString();

            // Assert
            Assert.That(result, Is.EqualTo(string.Format("Unknown ({0})", Filename)));
        }
    }
}