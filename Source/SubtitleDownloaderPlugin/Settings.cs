using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using SubtitleDownloaderPlugin.Engine;

namespace SubtitleDownloaderPlugin
{
    internal static class Settings
    {
        /// <summary>
        /// The default language to use when one has not been selected
        /// </summary>
        private const string DefaultLanguage = "eng";

        private static readonly string SettingsFilename;

        static Settings()
        {
            string applicationDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            SettingsFilename = Path.Combine(applicationDataFolderPath, @"Woodbury Software\Subtitle Downloader\settings.xml");
        }        

        /// <summary>
        /// Save a new secondary language setting
        /// </summary>
        /// <param name="languageCode">The newly selected secondary language</param>
        internal static void SaveSecondaryLanguage(string languageCode)
        {
            XmlDocument settings = new XmlDocument();

            if (!File.Exists(SettingsFilename))
            {
                CreateDefaultFile();
            }

            settings.Load(SettingsFilename);

            XmlNode languageNode = settings.SelectSingleNode("/Settings/SecondaryLanguage");

            if (languageNode == null)
            {
                XmlNode settingsNode = GetSettingsNode(settings);
                languageNode = settings.CreateElement("SecondaryLanguage");
                settingsNode.AppendChild(languageNode);
            }

            languageNode.InnerText = languageCode;
            settings.Save(SettingsFilename);
        }

        /// <summary>
        /// Get the currently selected secondary language
        /// </summary>
        /// <returns>The selected secondary language</returns>
        internal static SubtitleLanguage GetSecondaryLanguage()
        {
            if (!File.Exists(SettingsFilename))
            {
                return SubtitleUtilities.AvailableLanguages.First(x => x.Code.Equals(DefaultLanguage, StringComparison.InvariantCultureIgnoreCase));
            }

            XmlDocument settings = new XmlDocument();
            settings.Load(SettingsFilename);
            XmlNode languageNode = settings.SelectSingleNode("/Settings/SecondaryLanguage");

            if (languageNode == null || string.IsNullOrWhiteSpace(languageNode.InnerText))
            {
                return null;
            }

            return SubtitleUtilities.AvailableLanguages.First(x => x.Code.Equals(languageNode.InnerText, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Get the currently selected primary language
        /// </summary>
        /// <returns>The selected primary language</returns>
        internal static SubtitleLanguage GetPrimaryLanguage()
        {
            if (!File.Exists(SettingsFilename))
            {
                return SubtitleUtilities.AvailableLanguages.First(x => x.Code.Equals(DefaultLanguage, StringComparison.InvariantCultureIgnoreCase));
            }
            
            XmlDocument settings = new XmlDocument();
            settings.Load(SettingsFilename);
            XmlNode languageNode = settings.SelectSingleNode("/Settings/Language");
            
            return languageNode == null ? SubtitleUtilities.AvailableLanguages.First(x => x.Code.Equals(DefaultLanguage, StringComparison.InvariantCultureIgnoreCase)) : SubtitleUtilities.AvailableLanguages.First(x => x.Code.Equals(languageNode.InnerText, StringComparison.InvariantCultureIgnoreCase));
        }        
      
        /// <summary>
        /// Get the state of the sources
        /// </summary>
        /// <returns>The state of the sources</returns>
        internal static IList<SubtitleSourceItem> GetSources()
        {
            List<SubtitleSourceItem> result = new List<SubtitleSourceItem>();

            if (!File.Exists(SettingsFilename))
            {                
                result.AddRange(GetDefaultSources());
            }
            else
            {
                XmlDocument settings = new XmlDocument();
                settings.Load(SettingsFilename);

                XmlNode sourcesNode = settings.SelectSingleNode("/Settings/Sources");

                if (sourcesNode == null || sourcesNode.ChildNodes.Count == 0)
                {
                    result.AddRange(GetDefaultSources());    
                }
                else
                {
                    foreach (XmlElement sourceNode in sourcesNode.ChildNodes)
                    {
                        SubtitleSource source;

                        if (Enum.TryParse(sourceNode.InnerText, out source))
                        {
                            if (sourceNode.Attributes["Position"] == null)
                            {
                                continue;        
                            }

                            int position;

                            if (!int.TryParse(sourceNode.Attributes["Position"].InnerText, out position))
                            {
                                continue;                                
                            }

                            if (sourceNode.Attributes["Selected"] == null)
                            {
                                continue;        
                            }

                            bool selected;

                            if (!bool.TryParse(sourceNode.Attributes["Selected"].InnerText, out selected))
                            {
                                continue;
                            }

                            result.Add(new SubtitleSourceItem(source, position, selected));
                        }                        
                    }

                    if (!result.Any())
                    {
                        result.AddRange(GetDefaultSources());
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Save a new primary language setting
        /// </summary>
        /// <param name="languageCode">The newly selected primary language</param>
        internal static void SavePrimaryLanguage(string languageCode)
        {
            XmlDocument settings = new XmlDocument();

            if (!File.Exists(SettingsFilename))
            {
                CreateDefaultFile();
            }

            settings.Load(SettingsFilename);

            XmlNode languageNode = settings.SelectSingleNode("/Settings/Language");

            if (languageNode == null)
            {
                XmlNode settingsNode = GetSettingsNode(settings);
                languageNode = settings.CreateElement("Language");
                settingsNode.AppendChild(languageNode);
            }

            languageNode.InnerText = languageCode;
            settings.Save(SettingsFilename);
        }        

        internal static void SaveSources(IEnumerable<SubtitleSourceItem> sources)
        {
            XmlDocument settings = new XmlDocument();

            if (!File.Exists(SettingsFilename))
            {
                CreateDefaultFile();
            }

            settings.Load(SettingsFilename);

            XmlNode sourcesNode = settings.SelectSingleNode("/Settings/Sources");
            
            if (sourcesNode == null)
            {
                XmlNode settingsNode = GetSettingsNode(settings);
                sourcesNode = settings.CreateElement("Sources");
                settingsNode.AppendChild(sourcesNode);
            }

            sourcesNode.RemoveAll();

            foreach (SubtitleSourceItem source in sources)
            {
                CreateSourceNode(sourcesNode, source.Source, source.Position, source.Selected);
            }
            
            settings.Save(SettingsFilename);
        }

        private static XmlNode GetSettingsNode(XmlDocument settings)
        {
            XmlNode settingsNode = settings.SelectSingleNode("/Settings");

            if (settingsNode == null)
            {
                settingsNode = settings.CreateElement("Settings");
                settings.AppendChild(settingsNode);
            }

            return settingsNode;
        }

        /// <summary>
        /// Creates the settings file with default values
        /// </summary>
        private static void CreateDefaultFile()
        {
            XmlDocument settings = new XmlDocument();

            XmlElement rootNode = settings.CreateElement("Settings");
            settings.AppendChild(rootNode);

            XmlNode languageNode = settings.CreateElement("Language");
            languageNode.InnerText = DefaultLanguage;
            rootNode.AppendChild(languageNode);

            XmlNode sourcesNode = settings.CreateElement("Sources");
            rootNode.AppendChild(sourcesNode);

            CreateSourceNode(sourcesNode, SubtitleSource.OpenSubtitles, 1, true);
            CreateSourceNode(sourcesNode, SubtitleSource.SubDB, 2, true);

            string settingsFileFolderPath = Path.GetDirectoryName(SettingsFilename);

            if (settingsFileFolderPath == null)
            {                
                Message.ShowError(Strings.UnableToDetermineApplicationDataFolderError);
                return;
            }

            Directory.CreateDirectory(settingsFileFolderPath);
            settings.Save(SettingsFilename);            
        }

        /// <summary>
        /// Creates a source node and attributes
        /// </summary>
        /// <param name="sourcesNode">The sources node</param>
        /// <param name="source">The source</param>
        /// <param name="position">The position</param>
        /// <param name="selected">Is the source selected?</param>
        private static void CreateSourceNode(XmlNode sourcesNode, SubtitleSource source, int position, bool selected)
        {
            if (sourcesNode.OwnerDocument == null)
            {
                throw new ArgumentNullException("sourcesNode");
            }            

            XmlNode sourceNode = sourcesNode.OwnerDocument.CreateElement("Source");

            if (sourceNode.Attributes == null)
            {
                throw new ArgumentNullException("sourcesNode");
            }

            sourceNode.InnerText = ((int)source).ToString(CultureInfo.InvariantCulture);
            XmlAttribute positionAttribute = sourcesNode.OwnerDocument.CreateAttribute("Position");
            positionAttribute.InnerText = position.ToString(CultureInfo.InvariantCulture);
            sourceNode.Attributes.Append(positionAttribute);
            XmlAttribute selectedAttribute = sourcesNode.OwnerDocument.CreateAttribute("Selected");
            selectedAttribute.InnerText = selected.ToString();
            sourceNode.Attributes.Append(selectedAttribute);
            sourcesNode.AppendChild(sourceNode);
        }

        private static IEnumerable<SubtitleSourceItem> GetDefaultSources()
        {
            IList<SubtitleSourceItem> result = new List<SubtitleSourceItem>();
            result.Add(new SubtitleSourceItem(SubtitleSource.OpenSubtitles, 0, true));
            result.Add(new SubtitleSourceItem(SubtitleSource.SubDB, 1, true));
            return result;
        }
    }
}
