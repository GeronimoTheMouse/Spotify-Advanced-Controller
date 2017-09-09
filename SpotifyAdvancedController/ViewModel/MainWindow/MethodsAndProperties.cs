using System;
using System.ComponentModel;
using SpotifyAdvancedController.ViewModel.Command;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using System.Windows;
using System.Diagnostics;
using System.Speech.Recognition;
using System.IO;

namespace SpotifyAdvancedController.ViewModel.MainWindow
{
    public class MethodsAndProperties : INotifyPropertyChanged
    {
        #region Private and Public Properties
        private SpeechRecognitionEngine SpeechEngine;
        private SpotifyLocalAPI SpotifyAPIInstance;
        private Track CurrentTrack;

        private bool _IsSpotifyMuted = false;
        private string _SongName { get; set; } = "NaN";
        private string _ArtistName { get; set; } = "NaN";
        private string _AlbumName { get; set; } = "NaN";
        private string _Time { get; set; } = "NaN";
        private string _SongStatus { get; set; } = "NaN";
        private string _LastCommand { get; set; } = "NaN";
        private string _ConnectionButtonContent { get; set; } = "Connect";
        private bool _ConnectionButtonEnabled { get; set; } = true;
        private bool _IsVoiceControllerEnabled { get; set; } = false;
        private bool _IsMuteAdsEnabled { get; set; } = false;
        private bool _IsVoiceControllerChecked { get; set; } = false;
        private bool _IsMuteAdsChecked { get; set; } = false;
        private bool _PlayBtnIsEnabled { get; set; } = false;
        private bool _PauseBtnIsEnabled { get; set; } = false;
        private bool _NextBtnIsEnabled { get; set; } = false;
        private bool _PreviousBtnIsEnabled { get; set; } = false;
        private bool _TopMostIsEnabled { get; set; } = false;
        private bool _LastCommandIsEnabled { get; set; } = false;
        private string _SongNameLink { get; set; }
        private string _ArtistNameLink { get; set; }
        private string _AlbumNameLink { get; set; }

        public string SongName { get { return _SongName; } set { _SongName = value; RaisePropertyChanged("SongName"); } }
        public string ArtistName { get { return _ArtistName; } set { _ArtistName = value; RaisePropertyChanged("ArtistName"); } }
        public string AlbumName { get { return _AlbumName; } set { _AlbumName = value; RaisePropertyChanged("AlbumName"); } }
        public string Time { get { return _Time; } set { _Time = value; RaisePropertyChanged("Time"); } }
        public string SongStatus { get { return _SongStatus; } set { _SongStatus = value; RaisePropertyChanged("SongStatus"); } }
        public string LastCommand { get { return _LastCommand; } set { _LastCommand = value; RaisePropertyChanged("LastCommand"); } }
        public string ConnectionButtonContent { get { return _ConnectionButtonContent; } set { _ConnectionButtonContent = value; RaisePropertyChanged("ConnectionButtonContent"); } }
        public bool ConnectionButtonEnabled { get { return _ConnectionButtonEnabled; } set { _ConnectionButtonEnabled = value; RaisePropertyChanged("ConnectionButtonEnabled"); } }
        public bool IsVoiceControllerEnabled { get { return _IsVoiceControllerEnabled; } set { _IsVoiceControllerEnabled = value; RaisePropertyChanged("IsVoiceControllerEnabled"); } }
        public bool IsMuteAdsEnabled { get { return _IsMuteAdsEnabled; } set { _IsMuteAdsEnabled = value; RaisePropertyChanged("IsMuteAdsEnabled"); } }
        public bool IsVoiceControllerChecked { get { return _IsVoiceControllerChecked; } set { if (value == true) SpotifyCommandListener(true); else SpotifyCommandListener(false); _IsVoiceControllerChecked = value; RaisePropertyChanged("IsVoiceControllerChecked"); } }
        public bool IsMuteAdsChecked { get { return _IsMuteAdsChecked; } set { _IsMuteAdsChecked = value; RaisePropertyChanged("IsMuteAdsChecked"); } }
        public bool PlayBtnIsEnabled { get { return _PlayBtnIsEnabled; } set { _PlayBtnIsEnabled = value; RaisePropertyChanged("PlayBtnIsEnabled"); } }
        public bool PauseBtnIsEnabled { get { return _PauseBtnIsEnabled; } set { _PauseBtnIsEnabled = value; RaisePropertyChanged("PauseBtnIsEnabled"); } }
        public bool NextBtnIsEnabled { get { return _NextBtnIsEnabled; } set { _NextBtnIsEnabled = value; RaisePropertyChanged("NextBtnIsEnabled"); } }
        public bool PreviousBtnIsEnabled { get { return _PreviousBtnIsEnabled; } set { _PreviousBtnIsEnabled = value; RaisePropertyChanged("PreviousBtnIsEnabled"); } }
        public bool TopMostIsEnabled { get { return _TopMostIsEnabled; } set { _TopMostIsEnabled = value; RaisePropertyChanged("TopMostIsEnabled"); } }
        public bool LastCommandIsEnabled { get { return _LastCommandIsEnabled; } set { _LastCommandIsEnabled = value; RaisePropertyChanged("LastCommandIsEnabled"); } }
        #endregion

        #region Commands
        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand PlaySongCommand { get; set; }
        public RelayCommand PauseSongCommand { get; set; }
        public RelayCommand NextSongCommand { get; set; }
        public RelayCommand PreviousSongCommand { get; set; }
        public RelayCommand SongNavigateCommand { get; set; }
        public RelayCommand ArtistNavigateCommand { get; set; }
        public RelayCommand AlbumNavigateCommand { get; set; }
        #endregion

        /// <summary>
        /// The ctor(Constructor) of our program
        /// </summary>
        public MethodsAndProperties()
        {
            //initialize the Spotify API
            SpotifyAPIInstance = new SpotifyLocalAPI();
            SpotifyAPIInstance.OnPlayStateChange += SpotifyAPIInstance_OnPlayStateChange;
            SpotifyAPIInstance.OnTrackChange += SpotifyAPIInstance_OnTrackChange;
            SpotifyAPIInstance.OnTrackTimeChange += SpotifyAPIInstance_OnTrackTimeChange;
            //initialize commands
            ConnectCommand = new RelayCommand(Connect);
            PlaySongCommand = new RelayCommand(PlaySong);
            PauseSongCommand = new RelayCommand(PauseSong);
            NextSongCommand = new RelayCommand(NextSong);
            PreviousSongCommand = new RelayCommand(PreviousSong);
            SongNavigateCommand = new RelayCommand(SongNavigate);
            ArtistNavigateCommand = new RelayCommand(ArtistNavigate);
            AlbumNavigateCommand = new RelayCommand(AlbumNavigate);
            //initialize speech engine
            SpeechEngine = new SpeechRecognitionEngine();
            Choices commands = new Choices();
            commands.Add(new string[] { "play", "stop", "next", "back" });
            GrammarBuilder GramBuilder = new GrammarBuilder();
            GramBuilder.Append(commands);
            Grammar grammar = new Grammar(GramBuilder);
            SpeechEngine.LoadGrammarAsync(grammar);
            SpeechEngine.SetInputToDefaultAudioDevice();
            SpeechEngine.SpeechRecognized += SpeechEngine_SpeechRecognized;
        }

        /// <summary>
        /// OnTrackChange event. Occurs when the spotify song changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpotifyAPIInstance_OnTrackChange(object sender, TrackChangeEventArgs e)
        {
            CurrentTrack = e.NewTrack;
            UpdateTrack();
        }
        /// <summary>
        /// OnTrackTimeChange. Occurs when the spotify time changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpotifyAPIInstance_OnTrackTimeChange(object sender, TrackTimeChangeEventArgs e)
        {
            Time = $@"{FormatTime(e.TrackTime)}/{FormatTime(CurrentTrack.Length)}";
            if (SongStatus == "NaN")
                SongStatus = "True";
        }
        /// <summary>
        /// OnPlayStateChange. Occurs when the spotify changes state. For example
        /// from play to pause.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpotifyAPIInstance_OnPlayStateChange(object sender, PlayStateEventArgs e)
        {
            SongStatus = e.Playing.ToString();
        }
        /// <summary>
        /// The SpeechRecognized event. Occurs when someone speeks and the engine
        /// is opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpeechEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            switch (e.Result.Text)
            {
                case "play":
                    SpotifyAPIInstance.Play();
                    LastCommand = "Play Song";
                    break;
                case "stop":
                    SpotifyAPIInstance.Pause();
                    LastCommand = "Pause Song";
                    break;
                case "next":
                    SpotifyAPIInstance.Skip();
                    LastCommand = "Next Song";
                    break;
                case "back":
                    SpotifyAPIInstance.Previous();
                    LastCommand = "Previous Song";
                    break;
            }
        }
        /// <summary>
        /// It navigates into the spotify page where the Song is.
        /// </summary>
        /// <param name="obg"></param>
        private void SongNavigate(object obg)
        {
            if(_SongNameLink != null)
                Process.Start(_SongNameLink);
        }
        /// <summary>
        /// It navigates into the spotify page where the Artist is.
        /// </summary>
        /// <param name="obj"></param>
        private void ArtistNavigate(object obj)
        {
            if(_SongNameLink != null)
                Process.Start(_ArtistNameLink);
        }
        /// <summary>
        /// It navigates into the spotify page where the Album is.
        /// </summary>
        /// <param name="obj"></param>
        private void AlbumNavigate(object obj)
        {
            if(_SongNameLink != null)
                Process.Start(_AlbumNameLink);
        }
        /// <summary>
        /// Starts the speech recognition engine in order
        /// to receive commands
        /// </summary>
        /// <param name="Start"></param>
        private void SpotifyCommandListener(bool Start)
        {
            if (Start)
                SpeechEngine.RecognizeAsync(RecognizeMode.Multiple);
            else
                SpeechEngine.RecognizeAsyncStop();
        }
        /// <summary>
        /// Formats the currenttrack time
        /// </summary>
        /// <param name="sec"></param>
        /// <returns></returns>
        private String FormatTime(double sec)
        {
            TimeSpan span = TimeSpan.FromSeconds(sec);
            String secs = span.Seconds.ToString(), mins = span.Minutes.ToString();
            if (secs.Length < 2)
                secs = "0" + secs;
            return mins + ":" + secs;
        }
        /// <summary>
        /// Gets the Spotify Process ID based on it's class name
        /// </summary>
        /// <returns></returns>
        private int GetSpotifyPID()
        {
            Process[] SpotifyProcesses = Process.GetProcessesByName("Spotify");
            foreach (Process Proc in SpotifyProcesses)
            {
                if (VolumeMixerManager.GetApplicationClassName(Proc.Id) == "SpotifyMainWindow")
                {
                    return Proc.Id;
                }
            }
            return 0;
        }
        /// <summary>
        /// This method updates the track information into the UI
        /// </summary>
        private void UpdateTrack()
        {
            if (CurrentTrack.IsAd() && IsMuteAdsEnabled)
            {
                VolumeMixerManager.SetApplicationMute(GetSpotifyPID(), true);
                _IsSpotifyMuted = true;
                SongName = "Advertisement";
                _SongNameLink = null;
                ArtistName = "";
                _ArtistNameLink = null;
                AlbumName = "";
                _AlbumNameLink = null;
                return;
            }
            else
            {
                if (_IsSpotifyMuted)
                    VolumeMixerManager.SetApplicationMute(GetSpotifyPID(), false);
            }

            SongName = CurrentTrack.TrackResource.Name;
            _SongNameLink = CurrentTrack.TrackResource.Uri;
            ArtistName = CurrentTrack.ArtistResource.Name;
            _ArtistNameLink = CurrentTrack.ArtistResource.Uri;
            AlbumName = CurrentTrack.AlbumResource.Name;
            _AlbumNameLink = CurrentTrack.AlbumResource.Uri;
            Time = $@"0:00/{FormatTime(CurrentTrack.Length)}";
        }
        /// <summary>
        /// The play song command
        /// </summary>
        /// <param name="obj"></param>
        private void PlaySong(object obj)
        {
            SpotifyAPIInstance.Play();
            LastCommand = "Play Song";
        }
        /// <summary>
        /// The pause song command
        /// </summary>
        /// <param name="obj"></param>
        private void PauseSong(object obj)
        {
            SpotifyAPIInstance.Pause();
            LastCommand = "Pause Song";
        }
        /// <summary>
        /// The next song command
        /// </summary>
        /// <param name="obj"></param>
        private void NextSong(object obj)
        {
            SpotifyAPIInstance.Skip();
            LastCommand = "Next Song";
        }
        /// <summary>
        /// The previous song command
        /// </summary>
        /// <param name="obj"></param>
        private void PreviousSong(object obj)
        {
            SpotifyAPIInstance.Previous();
            LastCommand = "Previous Song";
        }
        /// <summary>
        /// This method connects into the Spotify client
        /// </summary>
        /// <param name="obj"></param>
        private void Connect(object obj)
        {
            bool successful = false;
            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
               MessageBoxResult Result = MessageBox.Show("Spotify isn't running! Would you like to open it for you ?", "Spotify isn't running", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                
                if(Result == MessageBoxResult.Yes)
                {
                    string SpotifyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Spotify", "Spotify.exe");
                    if (File.Exists(SpotifyPath))
                    {
                        Process.Start(SpotifyPath);
                        MessageBox.Show("Spotify succesfully started. Please re-connect !", "Spotify Started", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Spotify isn't installed in the default directory. Starting spotify failed. Please open it manual or install spotify on the default directory!!", "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
                else
                {
                    return;
                }
            }
            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                MessageBox.Show("SpotifyWebHelper isn't running! Please re-start Spotify !!");
                return;
            }
            try
            {
                successful = SpotifyAPIInstance.Connect();
            }
            catch (Exception)
            {
              MessageBoxResult Result = MessageBox.Show("Connection failed. Please check your internet connection. Try to re-connect ?", "Fatal Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if(Result == MessageBoxResult.Yes)
                {
                    Connect(null);
                    return;
                }
                else
                {
                    return;
                }
            }
            if (successful)
            {
                ConnectionButtonContent = "Connection to Spotify Established";
                ConnectionButtonEnabled = false;
                SpotifyAPIInstance.ListenForEvents = true;

                ConnectionButtonEnabled = false;
                IsVoiceControllerEnabled = false;
                IsMuteAdsEnabled = true;
                IsVoiceControllerEnabled = true;
                PlayBtnIsEnabled = true;
                PauseBtnIsEnabled = true;
                NextBtnIsEnabled = true;
                PreviousBtnIsEnabled = true;
                TopMostIsEnabled = true;
                LastCommandIsEnabled = true;
                StatusResponse status = SpotifyAPIInstance.GetStatus();
                CurrentTrack = status.Track;
                UpdateTrack();
            }
            else
            {
                MessageBoxResult Result = MessageBox.Show("Couldn't connect to the spotify client. Retry?", "Spotify", MessageBoxButton.YesNo);
                if (Result == MessageBoxResult.Yes)
                    Connect(null);
            }
        }

        #region PropertyChangedEventHandler and RaisePropertyChanged method
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
        #endregion
    }
}
