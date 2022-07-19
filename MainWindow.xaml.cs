using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Drawing;
using System.Net;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Media;
using ICSharpCode.SharpZipLib.Zip;

namespace VLauncher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    public partial class MainWindow : Window
    {
        static private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;
        static public string jsonPath;

        bool played;
        bool childemode;

        static Config config = new Config();

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Start";
                        ChildMode.Visibility = Visibility.Visible;
                        Played.Visibility = Visibility.Visible;

                        jsonPath = Path.Combine(rootPath, "Varea Launcher/Varea Games 2_Data/StreamingAssets/Options.json");
                        string json = File.ReadAllText(jsonPath);
                        config = JsonConvert.DeserializeObject<Config>(json);

                        config.played = false;
                        config.childmode = false;
                        JasonToFile();

                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        ChildMode.Visibility = Visibility.Hidden;
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading";
                        ChildMode.Visibility = Visibility.Hidden;
                        Played.Visibility = Visibility.Hidden;
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        ChildMode.Visibility = Visibility.Hidden;
                        Played.Visibility = Visibility.Hidden;
                        break;
                    default:
                        break;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Varea Launcher.zip");
            gameExe = Path.Combine(rootPath, "Varea Launcher", "Varea Launcher data/bin/Debug/Varea Launcher.exe");

            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = "steam://rungameid/250820",
                UseShellExecute = true
            });
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString("https://fourspower.github.io/VLaunch/Version.txt"));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                InstallGameFiles(false, Version.zero);
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    _onlineVersion = new Version(webClient.DownloadString("https://fourspower.github.io/VLaunch/Version.txt"));
                }

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri("https://fourspower.github.io/VLaunch/Varea%20Launcher.zip"), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                string onlineVersion = ((Version)e.UserState).ToString();
                FastZip fastZip = new FastZip();
                fastZip.ExtractZip(gameZip, rootPath, null);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionText.Text = onlineVersion;
                Status = LauncherStatus.ready;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex}");
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Varea Launcher");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }


        }



        

        public static void JasonToFile()
        {
            string jsonend = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(jsonPath, jsonend);
        }

        private void Played_Checked(object sender, RoutedEventArgs e)
        {
            if(Played.IsChecked == true)
            {
                config.played = true;
                JasonToFile();
            }
            else
            {
                config.played = false;
                JasonToFile();
            }
        }

        private void ChildMode_Checked(object sender, RoutedEventArgs e)
        {
            if (ChildMode.IsChecked == true)
            {
                config.childmode = true;
                JasonToFile();
            }
            else
            {
                config.childmode = false;
                JasonToFile();
            }
        }
    }


    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
