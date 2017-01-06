using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MediaCenter;
using SubtitleDownloaderPlugin.Engine;
using WoodburyUtilities;
using WoodburyUtilities.Enumerations;
using WoodburyUtilities.Interfaces;

namespace SubtitleDownloaderPlugin
{
    [System.Runtime.InteropServices.ProgId("SubtitleDownloader")]    
    public partial class MainInterface : UserControl
    {        
        private static readonly IList<string> VideoFilesBeingCheckedForSubtitles = new List<string>();
        private static bool populatingVideoList = false;

        private readonly ILogger logger;
        private readonly SubtitleUtilities subtitleUtilities;
        private readonly bool interfaceInitialised;
        private readonly string logFilename;

        private MCAutomation mediaCenter;
        private IList<MediaCenterFile> cachedVideoFileList;        

        /// <summary>
        /// Constructor
        /// </summary>
        public MainInterface()
        {       
            Application.ThreadException += this.ApplicationOnThreadException;
            string applicationDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            this.logFilename = Path.Combine(applicationDataFolderPath, @"Woodbury Software\Subtitle Downloader\log.txt");
            this.logger = new Logger(this.logFilename, new HashSet<LogLevel> { LogLevel.Error, LogLevel.Info });

            this.InitializeComponent();            
            this.subtitleUtilities = new SubtitleUtilities(new FileSystem(), new MediaInfo());            
            this.InitialiseLanguageComponents();            
            this.InitialiseSourcesComponents();                        
            this.interfaceInitialised = true;                                   
        }        

        /// <summary>
        /// Initialise the user control
        /// </summary>
        /// <param name="mediaCenterReference">The Media Center automation class</param>
        public void Init(MCAutomation mediaCenterReference)
        {
            try
            {
                this.mediaCenter = mediaCenterReference;
                this.mediaCenter.FireMJEvent += this.MediaCenterFireMjEvent;                
            }
            catch (Exception)
            {
                this.Enabled = false;
                throw;
            }
        }

        /// <summary>
        /// Get the selected language variables by reading the settings
        /// </summary>
        private static SelectedLanguages GetSelectedLanguages()
        {
            return new SelectedLanguages()
                   {
                       Primary = Settings.GetPrimaryLanguage(),
                       Secondary = Settings.GetSecondaryLanguage()
                   };
        }        
        
        /// <summary>
        /// Initialise the UI controls that deal with subtitle sources
        /// </summary>
        private void InitialiseSourcesComponents()
        {                        
            IList<SubtitleSourceItem> sources = Settings.GetSources();
            IList<SubtitleSource> subtitleSources = new List<SubtitleSource>();

            // Add each source that is stored in settings to the list box
            foreach (SubtitleSourceItem sourceItem in sources.OrderBy(x => x.Position))
            {
                int index = this.sourcesCheckedListBox.Items.Add(sourceItem);
                this.sourcesCheckedListBox.SetItemChecked(index, sourceItem.Selected);  
                subtitleSources.Add(sourceItem.Source);                                                                  
            }

            // Add any sources that were not in the settings to the end of the list. This should only be doing something the first time after a new subtitle source has been added.
            foreach (SubtitleSource subtitleSource in Enum.GetValues(typeof(SubtitleSource)))
            {
                if (!subtitleSources.Contains(subtitleSource))
                {
                    SubtitleSourceItem subtitleSourceItem = new SubtitleSourceItem(subtitleSource, false);
                    this.sourcesCheckedListBox.Items.Add(subtitleSourceItem);
                }
            }            
        }

        /// <summary>
        /// Initialise the UI controls that deal with subtitle language
        /// </summary>
        private void InitialiseLanguageComponents()
        {            
            // Add a "None" option to the secondary language combo box only as it's optional whereas primary is mandatory
            this.secondaryLanguageComboBox.Items.Add("-- None --");            

            // Add each available language to the two combo boxes
            foreach (SubtitleLanguage subtitleLanguage in SubtitleUtilities.AvailableLanguages)
            {
                this.primaryLanguageComboBox.Items.Add(subtitleLanguage);
                this.secondaryLanguageComboBox.Items.Add(subtitleLanguage);
            }

            SelectedLanguages selectedLanguages = GetSelectedLanguages();            
         
            // Select the selected primary language in the combo box
            this.primaryLanguageComboBox.SelectedItem = selectedLanguages.Primary;

            // Select the selected secondary language in the combo box if one has been selected else select "None"
            this.secondaryLanguageComboBox.SelectedItem = selectedLanguages.Secondary ?? this.secondaryLanguageComboBox.Items[0];
        }        

        /// <summary>
        /// Capture Media Center events
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parameter1"></param>
        /// <param name="parameter2"></param>
        private void MediaCenterFireMjEvent(string type, string parameter1, string parameter2)
        {            
            if (parameter1.Equals("MCC: NOTIFY_PLAYERSTATE_CHANGE"))
            {
                // Get a handle to the current playing file
                int currentPlayingFileIndex = this.mediaCenter.GetCurPlaylist().Position;
                IMJFileAutomation currentPlayingFile = this.mediaCenter.GetCurPlaylist().GetFile(currentPlayingFileIndex);                
                
                this.GetSubtitlesForFile(currentPlayingFile);
            }            
        }

        /// <summary>
        /// Get subtitles for a file
        /// </summary>
        /// <param name="file">The file</param>
        /// <returns>True if subtitles are downloaded successfully, false if not</returns>
        private bool GetSubtitlesForFile(IMJFileAutomation file)
        {
            string mediaType = file.Get("Media Type", false);

            if (mediaType.Equals("Video", StringComparison.InvariantCultureIgnoreCase))
            {
                string filename = file.Get("Filename", false);

                // Ignore files that don't exist (e.g. YouTube)
                if (!File.Exists(filename))
                {
                    return false;
                }

                try
                {
                    // Don't check for subtitles for the same file multiple times concurrently (for example if Media Center events happen in quick succession)
                    if (!VideoFilesBeingCheckedForSubtitles.Contains(filename))
                    {
                        VideoFilesBeingCheckedForSubtitles.Add(filename);

                        FileInfo currentPlayingFileInfo = new FileInfo(filename);
                        string mediaSubType = file.Get("Media Sub Type", false);
                        bool subtitleDownloadResult = false;
                        
                        SelectedLanguages selectedLanguages = GetSelectedLanguages();                                             
                        SubtitleDownloader subtitleDownloader = new SubtitleDownloader(new SubtitleUtilities(new FileSystem(), new MediaInfo()), new SubtitleDownloaderFactory());
                        IEnumerable<SubtitleSource> subtitleSources = Settings.GetSources().OrderBy(x => x.Position).Select(source => source.Source);

                        if (mediaSubType.Equals("TV Show", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string series = file.Get("Series", false);

                            ushort season;
                            string seasonString = file.Get("Season", false);
                            if (!ushort.TryParse(seasonString, out season))
                            {
                                return false;
                            }

                            ushort episode;
                            string episodeString = file.Get("Episode", false);
                            if (!ushort.TryParse(episodeString, out episode))
                            {
                                return false;
                            }                            

                            subtitleDownloadResult = subtitleDownloader.GetTVShowSubtitles(subtitleSources.ToList(), currentPlayingFileInfo, series, season, episode, selectedLanguages.Primary, selectedLanguages.Secondary, this.hideFilesWithSubtitlesCheckBox.Checked);
                        }
                        else if (mediaSubType.Equals("Movie", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string imdbID = file.Get("IMDb ID", false);

                            if (imdbID.Length <= 2)
                            {
                                return false;
                            }

                            // For some reason Media Center stores IMDb numbers with "tt" at the beginning so remove that
                            imdbID = imdbID.Remove(0, 2);
                            
                            subtitleDownloadResult = subtitleDownloader.GetMovieSubtitles(subtitleSources.ToList(), currentPlayingFileInfo, imdbID, selectedLanguages.Primary, selectedLanguages.Secondary, false);
                        }
                                                
                        if (subtitleDownloadResult)
                        {
                            // Update the cached file list to indicate that the file now has suitable subtitles
                            MediaCenterFile mediaCenterFile = this.cachedVideoFileList.FirstOrDefault(x => x.Filename.Equals(filename, StringComparison.InvariantCultureIgnoreCase));

                            if (mediaCenterFile != null)
                            {
                                mediaCenterFile.HasSuitableSubtitles = true;
                            }                            
                        }

                        return subtitleDownloadResult;
                    }
                }               
                finally
                {                    
                    if (VideoFilesBeingCheckedForSubtitles.Contains(filename))
                    {
                        VideoFilesBeingCheckedForSubtitles.Remove(filename);
                    }
                }
            }

            return false;
        }        

        private void PrimaryLanguageComboBoxSelectedValueChanged(object sender, EventArgs e)
        {
            string selectedLanguageCode = ((SubtitleLanguage)this.primaryLanguageComboBox.SelectedItem).Code;
            Settings.SavePrimaryLanguage(selectedLanguageCode);
            this.getSubtitlesResultsPanel.Visible = false;
        }

        private void SearchForVideoFilesButtonClick(object sender, EventArgs e)
        {
            this.searchForVideoFilesButton.Visible = false;
            this.getSubtitlesProgressBar.Value = 0;
            this.getSubtitlesProgressLabel1.Text = Strings.SearchingLibrary;
            this.getSubtitlesProgressLabel2.Text = string.Format(Strings.FilesWithoutSubtitlesFound, 0);
            this.ReCentreProgressItems();
            this.getSubtitlesProgressPanel.Visible = true;
            this.getSubtitlesResultsPanel.Visible = false;
            this.searchForVideoFilesBackgroundWorker.RunWorkerAsync();
        }

        private void SearchForVideoFilesBackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            IMJFilesAutomation videoFiles = this.mediaCenter.Search("[Media Sub Type]=[Movie],[TV Show]");
            int videoFileCount = videoFiles.GetNumberFiles();

            SelectedLanguages selectedLanguages = GetSelectedLanguages();                

            this.cachedVideoFileList = new List<MediaCenterFile>();
            int filesWithoutSubtitlesCount = 0;            

            SubtitleUtilities.StartFolderFileCaching();
                        
            // Loop through each video in the database and determine if it has suitable subtitles already
            for (int i = 0; i < videoFileCount; i++)
            {
                IMJFileAutomation videoFile = videoFiles.GetFile(i);
                string filename = videoFile.Get("Filename", false);

                bool hasSuitableSubtitles = this.subtitleUtilities.DoesVideoHaveSuitableSubtitles(filename, selectedLanguages.Primary, selectedLanguages.Secondary);

                if (!SubtitleUtilities.IsVideoDiskBased(filename))
                {
                    MediaCenterFile mediaCenterFile = new MediaCenterFile(filename, hasSuitableSubtitles, videoFile);
                    this.cachedVideoFileList.Add(mediaCenterFile);

                    if (!hasSuitableSubtitles)
                    {
                        filesWithoutSubtitlesCount++;
                    }
                }

                int progressPercentage = Convert.ToInt32(((i + 1) / (double)videoFileCount) * 100.0);
                this.searchForVideoFilesBackgroundWorker.ReportProgress(progressPercentage, filesWithoutSubtitlesCount);
            }            
            
            this.PopulateVideoFilesCheckedListBox();
        }

        /// <summary>
        /// Populate the video files checked list box with the list of video files from the database
        /// </summary>
        private void PopulateVideoFilesCheckedListBox()
        {
            if (populatingVideoList)
            {
                return;
            }

            populatingVideoList = true;

            try
            {
                this.getSubtitlesFileListView.Items.Clear();            

                if (this.cachedVideoFileList != null)
                {                    
                    // Add the list of files to the checked list box, obeying the "hide files with subtitles" option
                    IEnumerable<MediaCenterFile> videoFiles = this.hideFilesWithSubtitlesCheckBox.Checked ? this.cachedVideoFileList.Where(x => !x.HasSuitableSubtitles) : this.cachedVideoFileList;                
                
                    foreach (MediaCenterFile videoFile in videoFiles)
                    {
                        ListViewItem listViewItem;

                        if (videoFile.FileAutomation.Get("Media Sub Type", false).Equals("TV Show", StringComparison.InvariantCultureIgnoreCase))
                        {
                            listViewItem = new ListViewItem(
                                                            new[]
                                                            {
                                                                videoFile.FileAutomation.Get("Name", false),                                                             
                                                                videoFile.FileAutomation.Get("Season", false), 
                                                                videoFile.FileAutomation.Get("Episode", false)
                                                            }, 
                                                            this.getSubtitlesFileListView.Groups["getSubtitlesTvShowListViewGroup"])
                                {
                                    Tag = videoFile
                                };
                        }
                        else
                        {
                            listViewItem = new ListViewItem(
                                                            new[]
                                                            {
                                                                videoFile.FileAutomation.Get("Name", false), 
                                                                videoFile.FileAutomation.Get("Year", false), 
                                                                videoFile.FileAutomation.Get("Season", false), 
                                                                videoFile.FileAutomation.Get("Episode", false)
                                                            }, 
                                                            this.getSubtitlesFileListView.Groups["getSubtitlesMovieListViewGroup"])
                            {
                                Tag = videoFile
                            };
                        }

                        this.getSubtitlesFileListView.Items.Add(listViewItem);
                    }                
                }

                this.getSubtitlesDetailPanel.Visible = false;
            }
            finally
            {
                populatingVideoList = false;
            }
        }

        private void SearchForVideoFilesBackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                throw e.Error;
            }

            this.getSubtitlesResultsPanel.Visible = true;
            this.getSubtitlesProgressPanel.Visible = false;
            this.searchForVideoFilesButton.Visible = true;           

            if (this.cachedVideoFileList.Count == 0)
            {
                Message.ShowWarning(Strings.NoVideoFilesFoundInTheLibrary);
            }
        }
        
        private void SearchForVideoFilesBackgroundWorkerProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            this.getSubtitlesProgressLabel2.Text = string.Format(Strings.FilesWithoutSubtitlesFound, (int)e.UserState);
            this.getSubtitlesProgressBar.Value = e.ProgressPercentage;
            this.ReCentreProgressItems();
        }        
        
        private void GetSubtitlesProgressPanelResize(object sender, EventArgs e)
        {
            this.ReCentreProgressItems();
        }

        /// <summary>
        /// Resize the progress UI controls so that they are centred on the panel
        /// </summary>
        private void ReCentreProgressItems()
        {
            this.getSubtitlesProgressLabel2.Left = (this.getSubtitlesProgressPanel.Width - this.getSubtitlesProgressLabel2.Width) / 2;
            this.getSubtitlesProgressLabel1.Left = (this.getSubtitlesProgressPanel.Width - this.getSubtitlesProgressLabel1.Width) / 2;
            this.getSubtitlesProgressBar.Left = (this.getSubtitlesProgressPanel.Width - this.getSubtitlesProgressBar.Width) / 2;
        }
        
        private void GetSubtitlesBackgroundWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            ListView.CheckedListViewItemCollection checkedItems = this.getSubtitlesFileListView.CheckedItems;

            IList<MediaCenterFile> processedFiles = new List<MediaCenterFile>();
            int checkedItemsCount = checkedItems.Count;

            for (int i = 0; i < checkedItemsCount; i++)
            {
                MediaCenterFile selectedVideo = (MediaCenterFile)checkedItems[i].Tag;

                if (this.GetSubtitlesForFile(selectedVideo.FileAutomation))
                {
                    processedFiles.Add(selectedVideo);                    
                    int progressPercentage = Convert.ToInt32(((i + 1) / (double)checkedItemsCount) * 100.0);                  
                    this.getSubtitlesBackgroundWorker.ReportProgress(progressPercentage, processedFiles.Count);                    
                }
            }            
            
            e.Result = processedFiles;
        }
        
        private void GetSubtitlesBackgroundWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                throw e.Error;
            }

            this.searchForVideoFilesButton.Visible = true;
            this.getSubtitlesResultsPanel.Visible = true;
            this.getSubtitlesProgressPanel.Visible = false;
            this.getSubtitlesForSelectedFilesButton.Enabled = true;
            this.PopulateVideoFilesCheckedListBox();         

            IList<MediaCenterFile> processedFiles = (IList<MediaCenterFile>)e.Result;
            string message = string.Format(Strings.SubtitlesDownloaded + Environment.NewLine + Strings.NoSubtitlesFound, processedFiles.Count, this.getSubtitlesFileListView.CheckedItems.Count);
            Message.ShowInformation(message);            
        }

        private void GetSubtitlesForSelectedFilesButtonClick(object sender, EventArgs e)
        {
            if (this.getSubtitlesFileListView.CheckedItems.Count == 0)
            {
                Message.ShowWarning(Strings.YouMustSelectAtLeastOneFileWarning);
                return;
            }

            this.searchForVideoFilesButton.Visible = false;            
            this.getSubtitlesProgressBar.Value = 0;
            this.getSubtitlesProgressLabel1.Text = Strings.DownloadingSubtitles;
            this.getSubtitlesProgressLabel2.Text = string.Format(Strings.SubtitleFilesDownloaded, 0);
            this.ReCentreProgressItems();
            this.getSubtitlesProgressPanel.Visible = true;
            this.getSubtitlesResultsPanel.Visible = false;
            this.getSubtitlesBackgroundWorker.RunWorkerAsync();
        }        

        private void GetSubtitlesBackgroundWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.getSubtitlesProgressLabel2.Text = string.Format(Strings.SubtitleFilesDownloaded, (int)e.UserState);
            this.getSubtitlesProgressBar.Value = e.ProgressPercentage;
            this.ReCentreProgressItems();
        }

        private void SourcesCheckedListBoxItemCheck(object sender, ItemCheckEventArgs e)
        {                
            SubtitleSourceItem changedSource = (SubtitleSourceItem)this.sourcesCheckedListBox.Items[e.Index];
            changedSource.Selected = e.NewValue == CheckState.Checked;

            if (this.interfaceInitialised)
            {                
                IList<SubtitleSourceItem> sources = new List<SubtitleSourceItem>();

                foreach (SubtitleSourceItem sourceItem in this.sourcesCheckedListBox.Items)
                {
                    sources.Add(sourceItem.Equals(changedSource) ? changedSource : sourceItem);
                }

                Settings.SaveSources(sources);
            }
        }

        private void SourceUpButtonClick(object sender, EventArgs e)
        {
            int startIndex = this.sourcesCheckedListBox.SelectedIndex;
            int newIndex = startIndex - 1;

            if (startIndex > 0)
            {                
                SubtitleSourceItem swapItem = (SubtitleSourceItem)this.sourcesCheckedListBox.Items[newIndex];
                swapItem.Position++;
                
                SubtitleSourceItem item = (SubtitleSourceItem)this.sourcesCheckedListBox.Items[startIndex];
                item.Position--;

                this.sourcesCheckedListBox.Items.RemoveAt(startIndex);
                this.sourcesCheckedListBox.Items.Insert(newIndex, item);
                this.sourcesCheckedListBox.SetItemChecked(newIndex, item.Selected);
                this.sourcesCheckedListBox.SetSelected(newIndex, true);
            }            
        }

        private void SourceDownButtonClick(object sender, EventArgs e)
        {
            int startIndex = this.sourcesCheckedListBox.SelectedIndex;
            int newIndex = startIndex + 1;

            if (startIndex > -1 && startIndex < this.sourcesCheckedListBox.Items.Count - 1)
            {                
                SubtitleSourceItem swapItem = (SubtitleSourceItem)this.sourcesCheckedListBox.Items[newIndex];
                swapItem.Position--;
                
                SubtitleSourceItem item = (SubtitleSourceItem)this.sourcesCheckedListBox.Items[startIndex];
                item.Position++;
                
                this.sourcesCheckedListBox.Items.RemoveAt(startIndex);
                this.sourcesCheckedListBox.Items.Insert(newIndex, item);
                this.sourcesCheckedListBox.SetItemChecked(newIndex, item.Selected);
                this.sourcesCheckedListBox.SetSelected(newIndex, true);
            }            
        }

        private void IncludeFilesWithSubtitlesCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            this.PopulateVideoFilesCheckedListBox();
        }

        private void SecondaryLanguageComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedLanguageCode;

            if (this.secondaryLanguageComboBox.SelectedItem is SubtitleLanguage)
            {
                selectedLanguageCode = ((SubtitleLanguage)this.secondaryLanguageComboBox.SelectedItem).Code;
            }
            else
            {
                selectedLanguageCode = string.Empty;
            }

            Settings.SaveSecondaryLanguage(selectedLanguageCode);
            this.getSubtitlesResultsPanel.Visible = false;
        }

        private void GetSubtitlesSelectAllClick(object sender, EventArgs e)
        {
            for (int i = 0; i < this.getSubtitlesFileListView.Items.Count; i++)
            {
                this.getSubtitlesFileListView.Items[i].Checked = true;
            }
        }

        private void GetSubtitlesDeselectAllClick(object sender, EventArgs e)
        {
            for (int i = 0; i < this.getSubtitlesFileListView.Items.Count; i++)
            {
                this.getSubtitlesFileListView.Items[i].Checked = false;
            }
        }                 

        private void PopulateMovieSubtitlesListBox(string videoFilename)
        {
            this.getSubtitlesMovieDetailSubtitlesListBox.Items.Clear();
            this.getSubtitlesMovieDetailSubtitlesListBox.Items.AddRange(this.subtitleUtilities.GetSubtitlesForVideoFile(videoFilename).ToArray());

            if (this.getSubtitlesMovieDetailSubtitlesListBox.Items.Count > 0)
            {
                this.getSubtitlesMovieDetailSubtitlesListBox.SelectedIndex = 0;
            }
            else
            {
                this.getSubtitlesMovieDetailSetLanguageButton.Visible = false;
            }
        }

        private void PopulateTvShowSubtitlesListBox(string videoFilename)
        {
            this.getSubtitlesTvShowDetailSubtitlesListBox.Items.Clear();
            this.getSubtitlesTvShowDetailSubtitlesListBox.Items.AddRange(this.subtitleUtilities.GetSubtitlesForVideoFile(videoFilename).ToArray());

            if (this.getSubtitlesTvShowDetailSubtitlesListBox.Items.Count > 0)
            {
                this.getSubtitlesTvShowDetailSubtitlesListBox.SelectedIndex = 0;
            }
            else
            {
                this.getSubtitlesTvShowDetailSetLanguageButton.Visible = false;
            }
        }        

        private void GetSubtitlesTvShowSubtitlesListBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.getSubtitlesTvShowDetailSubtitlesListBox.SelectedItem != null)
            {                
                this.getSubtitlesTvShowDetailSetLanguageButton.Visible = this.getSubtitlesTvShowDetailSubtitlesListBox.SelectedItem.ToString().StartsWith("Unknown", StringComparison.InvariantCultureIgnoreCase) && !this.getSubtitlesTvShowDetailSubtitlesListBox.SelectedItem.ToString().Contains("(Embedded)");
            }
        }

        private void GetSubtitlesSetLanguageButtonClick(object sender, EventArgs e)
        {
            if (this.getSubtitlesFileListView.SelectedItems.Count > 0)
            {
                MediaCenterFile selectedVideoFile = (MediaCenterFile)this.getSubtitlesFileListView.SelectedItems[0].Tag;

                if (this.getSubtitlesTvShowDetailSubtitlesListBox.SelectedItem == null)
                {
                    return;
                }

                SetLanguageForm setLanguageForm = new SetLanguageForm(((SubtitleFile)this.getSubtitlesTvShowDetailSubtitlesListBox.SelectedItem).Filename);
                setLanguageForm.ShowDialog();                
                
                SubtitleUtilities.StartFolderFileCaching();
                this.PopulateTvShowSubtitlesListBox(selectedVideoFile.Filename);
            }
        }

        private void GetSubtitlesMovieDetailSubtitlesListBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.getSubtitlesMovieDetailSubtitlesListBox.SelectedItem != null)
            {
                this.getSubtitlesMovieDetailSetLanguageButton.Visible = this.getSubtitlesMovieDetailSubtitlesListBox.SelectedItem.ToString().StartsWith("Unknown", StringComparison.InvariantCultureIgnoreCase) && !this.getSubtitlesMovieDetailSubtitlesListBox.SelectedItem.ToString().Contains("(Embedded)");
            }
        }

        private void GetSubtitlesMovieDetailSetLanguageButtonClick(object sender, EventArgs e)
        {
            if (this.getSubtitlesFileListView.SelectedItems.Count > 0)
            {
                MediaCenterFile selectedVideoFile = (MediaCenterFile)this.getSubtitlesFileListView.SelectedItems[0].Tag;

                if (this.getSubtitlesMovieDetailSubtitlesListBox.SelectedItem == null)
                {
                    return;
                }

                SetLanguageForm setLanguageForm = new SetLanguageForm(((SubtitleFile)this.getSubtitlesMovieDetailSubtitlesListBox.SelectedItem).Filename);
                setLanguageForm.ShowDialog();

                SubtitleUtilities.StartFolderFileCaching();
                this.PopulateMovieSubtitlesListBox(selectedVideoFile.Filename);
            }
        }

        private void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs threadExceptionEventArgs)
        {
            this.logger.WriteError(threadExceptionEventArgs.Exception);
            Message.ShowError("An error has occurred. Please close and re-open Media Center before continuing.");
        }

        private void ShowLogButtonClick(object sender, EventArgs e)
        {
            Process.Start(this.logFilename);
        }

        private void ListView1SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.getSubtitlesFileListView.SelectedItems.Count > 0)
            {
                MediaCenterFile mediaCenterFile = (MediaCenterFile)((ListViewItem)this.getSubtitlesFileListView.SelectedItems[0]).Tag;
                this.getSubtitlesMediaSubTypeValueLabel.Text = mediaCenterFile.FileAutomation.Get("Media Sub Type", false);

                if (this.getSubtitlesMediaSubTypeValueLabel.Text.Equals("TV Show", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.getSubtitlesTvShowDetailPanel.Visible = true;
                    this.getSubtitlesMovieDetailPanel.Visible = false;
                    this.getSubtitlesTvShowDetailSeriesValueLabel.Text = mediaCenterFile.FileAutomation.Get("Series", false);
                    this.getSubtitlesTvShowDetailSeasonValueLabel.Text = mediaCenterFile.FileAutomation.Get("Season", false);
                    this.getSubtitlesTvShowDetailEpisodeValueLabel.Text = mediaCenterFile.FileAutomation.Get("Episode", false);
                    this.PopulateTvShowSubtitlesListBox(mediaCenterFile.Filename);
                }
                else if (this.getSubtitlesMediaSubTypeValueLabel.Text.Equals("Movie", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.getSubtitlesMovieDetailPanel.Visible = true;
                    this.getSubtitlesTvShowDetailPanel.Visible = false;
                    this.getSubtitlesMovieDetailNameValueLabel.Text = mediaCenterFile.FileAutomation.Get("Name", false);
                    this.PopulateMovieSubtitlesListBox(mediaCenterFile.Filename);
                }

                this.getSubtitlesDetailPanel.Visible = true;
            }
            else
            {
                this.getSubtitlesDetailPanel.Visible = false;
            }
        }

        private void GetSubtitlesFileListViewResize(object sender, EventArgs e)
        {
            this.columnHeader1.Width = this.getSubtitlesFileListView.Width - this.columnHeader2.Width - this.columnHeader3.Width;
        }             
    }
}