using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace SubtitleDownloaderPlugin
{
    internal static class Message
    {
        private static readonly string AssemblyTitle;

        static Message()
        {
            AssemblyTitle = GetAssemblyTitle();
        }

        /// <summary>
        /// Displays a message box styled for an error
        /// </summary>
        /// <param name="errorMessageText">The text of the error message</param>
        internal static void ShowError(string errorMessageText)
        {
            MessageBox.Show(errorMessageText, AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Displays a message box styled for information
        /// </summary>
        /// <param name="informationMessageText">The text of the information message</param>
        internal static void ShowInformation(string informationMessageText)
        {
            MessageBox.Show(informationMessageText, AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Displays a message box styled for a warning
        /// </summary>
        /// <param name="warningMessageText">The text of the warning message</param>
        internal static void ShowWarning(string warningMessageText)
        {
            MessageBox.Show(warningMessageText, AssemblyTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Gets the assembly title
        /// </summary>        
        private static string GetAssemblyTitle()
        {
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);

            if (attributes.Length > 0)
            {
                AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];

                if (!string.IsNullOrWhiteSpace(titleAttribute.Title))
                {
                    return titleAttribute.Title;
                }
            }

            return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
        }        
    }
}
