using NUnit.Framework;
using SubtitleDownloaderPlugin.Engine;

namespace SubtitleDownloaderPlugin.UnitTests
{
    [TestFixture]
    public class SubtitleLanguageTests
    {
        [Test]
        public void ToString_should_return_the_language_name()
        {
            // Arrange
            const string LanguageName = "English";
            SubtitleLanguage subtitleLanguage = new SubtitleLanguage(LanguageName, string.Empty, string.Empty);

            // Act
            string result = subtitleLanguage.ToString();

            // Assert
            Assert.That(result, Is.EqualTo(LanguageName));
        }        
    }
}
