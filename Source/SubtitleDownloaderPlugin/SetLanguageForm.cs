using System.IO;
using System.Windows.Forms;
using SubtitleDownloaderPlugin.Engine;
using WoodburyUtilities;

namespace SubtitleDownloaderPlugin
{
    public partial class SetLanguageForm : Form
    {
        private readonly string subtitleFilename;

        public SetLanguageForm(string subtitleFilename)
        {
            this.subtitleFilename = subtitleFilename;
            this.InitializeComponent();            

            // Add each available language to the combo box
            foreach (SubtitleLanguage subtitleLanguage in SubtitleUtilities.AvailableLanguages)
            {
                this.languageComboBox.Items.Add(subtitleLanguage);                
            }
        }

        private void CancelButtonClick(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void OkButtonClick(object sender, System.EventArgs e)
        {
            if (this.languageComboBox.SelectedItem == null)
            {
                Message.ShowWarning(Strings.NoLanguageSelected);
                return;
            }

            SubtitleLanguage selectedLanguage = (SubtitleLanguage)this.languageComboBox.SelectedItem;

            // Rename the subtitle file to indicate it contains subtitles in the selected language
            string folder = Path.GetDirectoryName(this.subtitleFilename);
            string extension = Path.GetExtension(this.subtitleFilename);
            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(this.subtitleFilename);
            string newFilename = string.Format(@"{0}{1}.{2}{3}", folder, filenameWithoutExtension, selectedLanguage.Code, extension);            
            FileSystem fileSystem = new FileSystem();
            fileSystem.RenameFile(this.subtitleFilename, newFilename);

            this.Close();
        }
    }
}
