using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Windows;
using SpotifyAdvancedController.ViewModel.Command;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;

namespace SpotifyAdvancedController.ViewModel.MainWindow
{
    public class ViewModel : BaseViewModel
    {
        #region Private and Public Properties

        private readonly SpeechRecognitionEngine _speechEngine;
        private readonly SpotifyLocalAPI _spotifyApiInstance;
        private Track _currentTrack;

        private bool _isSpotifyMuted;
        private double _currentTrackTime;
        private bool _isRunning;
        private string _lastCommand = "NaN";
        private bool _connectionHasBeenMade;
        private bool _muteAds;
        private bool _controlThroughVoice;
        private string _spotifyVersion;

        public double CurrentTrackTime
        {
            get => _currentTrackTime;
            set
            {
                _currentTrackTime = value;
                RaisePropertyChanged("CurrentTrackTime");
            }
        }
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                RaisePropertyChanged("IsRunning");
            }
        }
        public string LastCommand
        {
            get => _lastCommand;
            set
            {
                _lastCommand = value;
                RaisePropertyChanged("LastCommand");
            }
        }
        public bool ConnectionHasBeenMade
        {
            get => _connectionHasBeenMade;
            set
            {
                _connectionHasBeenMade = value;
                RaisePropertyChanged("ConnectionHasBeenMade");
            }
        }
        public bool MuteAds
        {
            get => _muteAds;
            set
            {
                _muteAds = value;
                RaisePropertyChanged("MuteAds");
            }
        }
        public bool ControlThroughVoice
        {
            get => _controlThroughVoice;
            set
            {
                _controlThroughVoice = value;
                SpotifyCommandListener(value);
                RaisePropertyChanged("ControlThroughVoice");
            }
        }
        public Track CurrentTrack
        {
            get => _currentTrack;
            set
            {
                _currentTrack = value;
                RaisePropertyChanged("CurrentTrack");
            }
        }
        public string SpotifyVersion
        {
            get => _spotifyVersion;
            set
            {
                _spotifyVersion = value;
                RaisePropertyChanged("SpotifyVersion");
            }
        }
        #endregion

        #region Commands
        public RelayCommand ConnectCommand { get; }
        public RelayCommand PlaySongCommand { get; }
        public RelayCommand PauseSongCommand { get; }
        public RelayCommand NextSongCommand { get; }
        public RelayCommand PreviousSongCommand { get; }
        public RelayCommand SongNavigateCommand { get; }
        public RelayCommand ArtistNavigateCommand { get; }
        public RelayCommand AlbumNavigateCommand { get; }
        #endregion

        /// <summary>
        ///     The constructor of our program
        /// </summary>
        public ViewModel()
        {
            //initialize the Spotify API
            _spotifyApiInstance = new SpotifyLocalAPI();
            _spotifyApiInstance.OnPlayStateChange += SpotifyAPIInstance_OnPlayStateChange;
            _spotifyApiInstance.OnTrackChange += SpotifyAPIInstance_OnTrackChange;
            _spotifyApiInstance.OnTrackTimeChange += SpotifyAPIInstance_OnTrackTimeChange;
            //initialize commands
            ConnectCommand = new RelayCommand(SpotifyConnectionManager);
            PlaySongCommand = new RelayCommand(PlaySong);
            PauseSongCommand = new RelayCommand(PauseSong);
            NextSongCommand = new RelayCommand(NextSong);
            PreviousSongCommand = new RelayCommand(PreviousSong);
            SongNavigateCommand = new RelayCommand(SongNavigate);
            ArtistNavigateCommand = new RelayCommand(ArtistNavigate);
            AlbumNavigateCommand = new RelayCommand(AlbumNavigate);
            //initialize speech engine
            _speechEngine = new SpeechRecognitionEngine();
            var commands = new Choices();
            commands.Add("play", "stop", "next", "back");
            var gramBuilder = new GrammarBuilder();
            gramBuilder.Append(commands);
            var grammar = new Grammar(gramBuilder);
            _speechEngine.LoadGrammarAsync(grammar);
            _speechEngine.SetInputToDefaultAudioDevice();
            _speechEngine.SpeechRecognized += SpeechEngine_SpeechRecognized;
        }

        /// <summary>
        ///     OnTrackChange event. Occurs when the spotify song changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpotifyAPIInstance_OnTrackChange(object sender, TrackChangeEventArgs e)
        {
            CurrentTrack = e.NewTrack;

            if (CurrentTrack.IsAd() || CurrentTrack.ArtistResource.Name == "" || CurrentTrack.ArtistResource.Name == null)
            {
                CurrentTrack.TrackResource.Name = "Advertisement";
                CurrentTrack.AlbumResource.Name = "Advertisement";
                CurrentTrack.ArtistResource.Name = "Advertisement";
                if (MuteAds)
                {
                    VolumeMixerManager.SetApplicationMute(GetSpotifyPID(), true);
                    _isSpotifyMuted = true;
                }
            }

            if (_isSpotifyMuted)
            {
                VolumeMixerManager.SetApplicationMute(GetSpotifyPID(), false);
                _isSpotifyMuted = false;
            }
        }

        /// <summary>
        ///     OnTrackTimeChange. Occurs when the spotify time changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpotifyAPIInstance_OnTrackTimeChange(object sender, TrackTimeChangeEventArgs e)
        {
            CurrentTrackTime = e.TrackTime;
            IsRunning = _spotifyApiInstance.GetStatus().Playing;
        }

        /// <summary>
        ///     OnPlayStateChange. Occurs when the spotify changes state. For example
        ///     from play to pause.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpotifyAPIInstance_OnPlayStateChange(object sender, PlayStateEventArgs e)
        {
            IsRunning = e.Playing;
        }

        /// <summary>
        ///     The SpeechRecognized event. Occurs when someone speeks and the engine
        ///     is opened.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SpeechEngine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            switch (e.Result.Text)
            {
                case "play":
                     await _spotifyApiInstance.Play();
                    LastCommand = "Play Song";
                    break;
                case "stop":
                    await _spotifyApiInstance.Pause();
                    LastCommand = "Pause Song";
                    break;
                case "next":
                    _spotifyApiInstance.Skip();
                    LastCommand = "Next Song";
                    break;
                case "back":
                    _spotifyApiInstance.Previous();
                    LastCommand = "Previous Song";
                    break;
            }
        }

        /// <summary>
        ///     It navigates into the spotify page where the Song is.
        /// </summary>
        /// <param name="obg"></param>
        private void SongNavigate(object obg)
        {
            if (CurrentTrack.TrackResource.Uri != null)
                Process.Start(CurrentTrack.TrackResource.Uri);
        }

        /// <summary>
        ///     It navigates into the spotify page where the Artist is.
        /// </summary>
        /// <param name="obj"></param>
        private void ArtistNavigate(object obj)
        {
            if (CurrentTrack.ArtistResource.Uri != null)
                Process.Start(CurrentTrack.ArtistResource.Uri);
        }

        /// <summary>
        ///     It navigates into the spotify page where the Album is.
        /// </summary>
        /// <param name="obj"></param>
        private void AlbumNavigate(object obj)
        {
            if (CurrentTrack.AlbumResource.Uri != null)
                Process.Start(CurrentTrack.AlbumResource.Uri);
        }

        /// <summary>
        ///     Starts the speech recognition engine in order
        ///     to receive commands
        /// </summary>
        /// <param name="start"></param>
        private void SpotifyCommandListener(bool start)
        {
            if (start)
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            else
                _speechEngine.RecognizeAsyncStop();
        }

        /// <summary>
        /// Gets the Spotify Process ID based on it's class name
        /// </summary>
        /// <returns></returns>
        private int GetSpotifyPID()
        {
            var spotifyProcesses = Process.GetProcessesByName("Spotify");
            return (from proc in spotifyProcesses where VolumeMixerManager.GetApplicationClassName(proc.Id) == "SpotifyMainWindow" select proc.Id).FirstOrDefault();
        }

        /// <summary>
        ///     The play song command
        /// </summary>
        /// <param name="obj"></param>
        private async void PlaySong(object obj)
        {
            await _spotifyApiInstance.Play();
            LastCommand = "Play Song";
        }

        /// <summary>
        ///     The pause song command
        /// </summary>
        /// <param name="obj"></param>
        private async void PauseSong(object obj)
        {
            await _spotifyApiInstance.Pause();
            LastCommand = "Pause Song";
        }

        /// <summary>
        ///     The next song command
        /// </summary>
        /// <param name="obj"></param>
        private void NextSong(object obj)
        {
            _spotifyApiInstance.Skip();
            LastCommand = "Next Song";
        }

        /// <summary>
        ///     The previous song command
        /// </summary>
        /// <param name="obj"></param>
        private void PreviousSong(object obj)
        {
            _spotifyApiInstance.Previous();
            LastCommand = "Previous Song";
        }

        /// <summary>
        ///     This method perform the necessary actions to connect
        ///     to spotify
        /// </summary>
        /// <param name="obj"></param>
        private void SpotifyConnectionManager(object obj)
        {
            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                var result = MessageBox.Show("Spotify isn't running! Would you like to open it for you ?",
                    "Spotify isn't running", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (result == MessageBoxResult.Yes)
                {
                    var spotifyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Spotify", "Spotify.exe");
                    if (File.Exists(spotifyPath))
                    {
                        Process.Start(spotifyPath);
                        MessageBox.Show("Spotify succesfully started. Please re-connect !", "Spotify Started",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Spotify isn't installed in the default directory. Starting spotify failed. Please open it manual or install spotify on the default directory!!",
                            "Operation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
                return;
            }
            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                MessageBox.Show("SpotifyWebHelper isn't running! Please re-start Spotify !!");
                return;
            }

            var connResult = Connect();

            if (connResult == "Successful")
            {
                ConnectionHasBeenMade = true;
                _spotifyApiInstance.ListenForEvents = true;
                StatusResponse status = _spotifyApiInstance.GetStatus();
                CurrentTrack = status.Track;
                SpotifyVersion = status.ClientVersion;
                IsRunning = false;
            }
            else
            {
                var messageBoxResult =
                    MessageBox.Show(
                        "Couldn't connect to the spotify client. Error: " + connResult + " \nTry to re-connect?",
                        "Spotify", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                    SpotifyConnectionManager(null);
            }
        }

        /// <summary>
        ///     Connect into Spotify and return a result
        /// </summary>
        /// <returns></returns>
        private string Connect()
        {
            try
            {
                var successful = _spotifyApiInstance.Connect();

                if (successful)
                    return "Successful";
                return "NotSuccessful";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
