using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Management;
using System.Diagnostics;
using Microsoft.Win32;
using System.Drawing;
using Image = System.Drawing.Image;

namespace NVIDIA_Ansel_AI_Up_Res
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool forceMode = false;
        SupportLevel supportLevel;
        bool systemCheckComplete = false;
        bool outputFolderMode = false;
        int threadCount = 1;
        int tasksCompleted = 0;
        int failedImages = 0;
        bool firstTime;
        readonly List<string> imagePaths = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            OnStart();
        }

        void OnStart()
        {
            ApplySettings();

            if (firstTime)
            {
                var i = (int)MessageBox.Show("NVIDIA Display Adapter is required ;)\n\n\nTL;DR\nDo not up-res images that will result in a resolution of over 9000x9000.\n\nWhen up-ressing an image that will result in a resolution of over 9000x9000, the image is likely to provide an error during processing due to memory limitations with NVIDIA's API.", "Important Info", MessageBoxButton.OK, MessageBoxImage.Information);
                firstTime = false;

                if (Properties.Settings.Default == null)
                    return;

                Properties.Settings.Default.FirstTime = false;
                Properties.Settings.Default.Save();
            }
        }

        void ApplySettings()
        {
            if (Properties.Settings.Default == null)
                return;

            outputFolderMode = Properties.Settings.Default.OutputFolderMode;
            outputFolderModeCheckBox.IsChecked = Properties.Settings.Default.OutputFolderMode;
            threadCount = Properties.Settings.Default.ThreadCount;
            firstTime = Properties.Settings.Default.FirstTime;
            MaxSizeModeCheckBox.IsChecked = Properties.Settings.Default.ResolutionLimiter;
        }

        void SetupThreadComboBox()
        {
            int threads = Environment.ProcessorCount;

            for (var i = 1; i < threads + 1; i++)
                threadsComboBox.Items.Add(i);

            if (threadCount > Environment.ProcessorCount)
                threadCount = Environment.ProcessorCount;

            threadsComboBox.SelectedIndex = threadCount - 1;
        }


        // UI start
        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DetectSystem();
        }

        // Checks whether the system is able to use the app.
        void DetectSystem()
        {
            GraphicsAdapter graphicsAdapter = DetectGraphicsAdapter();

            supportLevel = graphicsAdapter.SupportLevel;

            if (graphicsAdapter.SupportLevel != SupportLevel.Full)
                forceModeCheckBox.Visibility = Visibility.Visible;

            // Has graphics adapter but not app (probably driver update needed).
            if ((graphicsAdapter.SupportLevel != SupportLevel.None) && !DetectApp())
                MessageBox.Show("I was unable to locate 'C:/Program Files/NVIDIA Corporation/NVIDIA NvDLISR/nvdlisrwrapper.exe'. Without this app, I cannot do what I am supposed to do.\n\nIt is very likely that your Display Adapter Driver needs updating.", "Prerequisites Missing", MessageBoxButton.OK, MessageBoxImage.Error);
            // Has neither the graphics adapter nor the app (probably not NVIDIA).
            else if (graphicsAdapter.SupportLevel == SupportLevel.None && !DetectApp())
                MessageBox.Show("An NVIDIA GeForce GTX or RTX display adapter is required and neither could not be found.", "Prerequisites Missing", MessageBoxButton.OK, MessageBoxImage.Error);
            // Has the app but not the graphics adapter (probably used to have NVIDIA but not anymore; could also be the app not recognising the graphics adapter properly).
            else if (graphicsAdapter.SupportLevel == SupportLevel.None && DetectApp())
                MessageBox.Show("An NVIDIA GeForce GTX or RTX display adapter is required and neither could be found.\n\nIf you feel like this is a mistake, enable 'Force Mode'.", "Prerequisites Missing", MessageBoxButton.OK, MessageBoxImage.Error);

            UpdateOptions();

            systemCheckComplete = true;
        }

        void UpdateOptions()
        {
            colourModeComboBox.Items.Clear();

            // If no support
            if (supportLevel == SupportLevel.None)
            {
                colourModeComboBox.IsEnabled = false;
                resolutionScaleComboBox.IsEnabled = false;
                startButton.IsEnabled = false;
                browseImagesButton.IsEnabled = false;
                clearImagesButton.IsEnabled = false;
                forceModeCheckBox.IsEnabled = true;
                threadsComboBox.IsEnabled = false;
                outputFolderModeCheckBox.IsEnabled = false;
                MaxSizeModeCheckBox.IsEnabled = false;
            }
            // If partial support
            else if (supportLevel == SupportLevel.Partial)
            {
                colourModeComboBox.IsEnabled = true;
                resolutionScaleComboBox.IsEnabled = true;
                startButton.IsEnabled = true;
                browseImagesButton.IsEnabled = true;
                clearImagesButton.IsEnabled = true;
                colourModeComboBox.Items.Add("Greyscale");
                colourModeComboBox.SelectedIndex = 0;
                forceModeCheckBox.IsEnabled = true;
                threadsComboBox.IsEnabled = true;
                outputFolderModeCheckBox.IsEnabled = true;
                MaxSizeModeCheckBox.IsEnabled = true;
            }
            // If full support or force mode
            if (supportLevel == SupportLevel.Full || forceMode)
            {
                colourModeComboBox.IsEnabled = true;
                resolutionScaleComboBox.IsEnabled = true;
                startButton.IsEnabled = true;
                browseImagesButton.IsEnabled = true;
                clearImagesButton.IsEnabled = true;
                colourModeComboBox.Items.Add("Colour");
                colourModeComboBox.Items.Add("Greyscale");
                colourModeComboBox.SelectedIndex = 0;
                forceModeCheckBox.IsEnabled = true;
                threadsComboBox.IsEnabled = true;
                outputFolderModeCheckBox.IsEnabled = true;
                MaxSizeModeCheckBox.IsEnabled = true;
            }

            SetupThreadComboBox();
        }

        // Checks if the app required for up-ressing is present.
        bool DetectApp()
        {
            if (!File.Exists("C:/Program Files/NVIDIA Corporation/NVIDIA NvDLISR/nvdlisrwrapper.exe"))
                return false;

            return true;
        }

        // Returns the most suitable graphics adapter in the system whiles also displaying to the user what graphics adapter they have.
        GraphicsAdapter DetectGraphicsAdapter()
        {
            GraphicsAdapter graphicsAdapter = GetGraphicsAdapter();

            string graphicsAdapterName = graphicsAdapter.Name;

            if (graphicsAdapterName.Length > 26)
            {
                graphicsAdapterName = graphicsAdapterName.Substring(0, 24);
                graphicsAdapterName += "...";
            }

            if (graphicsAdapter.SupportLevel == SupportLevel.Full)
            {
                graphicsAdapterLabel.Text = $"{graphicsAdapterName} (Full Support)";
                graphicsAdapterLabel.ToolTip = $"{graphicsAdapter.Name} (Full Support)";
            }
            else if (graphicsAdapter.SupportLevel == SupportLevel.Partial)
            {
                graphicsAdapterLabel.Text = $"{graphicsAdapterName} (Partial Support)";
                graphicsAdapterLabel.ToolTip = $"{graphicsAdapter.Name} (Partial Support)";
            }
            else
            {
                graphicsAdapterLabel.Text = $"{graphicsAdapterName} (No Support)";
                graphicsAdapterLabel.ToolTip = $"{graphicsAdapter.Name} (No Support)";
            }

            return graphicsAdapter;
        }

        // Returns the most suitable graphics adapter present in the system.
        GraphicsAdapter GetGraphicsAdapter()
        {
            // Gets every graphics adapter in use by the system.
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DisplayConfiguration");

            List<GraphicsAdapter> graphicsAdapters = new List<GraphicsAdapter>();

            // Saves the name of every found graphics card in a list.
            foreach (ManagementObject mo in searcher.Get())
                foreach (PropertyData property in mo.Properties)
                    if (property.Name == "Description")
                        graphicsAdapters.Add(new GraphicsAdapter(property.Value.ToString()));

            // If no graphics adapters could be found...
            if (graphicsAdapters.Count < 1)
            {
                MessageBox.Show("For some unknown reason, your display adapter could not be found.\n\nPerhaps you have some security program blocking me access?\n\nIf you feel like this is a mistake, enable 'Force Mode'.", "Display adapter not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return new GraphicsAdapter("N/A");
            }

            // For every graphics adapter, find its support level by looking up its name.
            foreach (var graphicsAdapter in graphicsAdapters)
            {

                if (FullySupportedGraphicsAdapters().Any(s => graphicsAdapter.Name.ToLower().Contains(s)))
                    graphicsAdapter.SupportLevel = SupportLevel.Full;
                else if (PartiallySupportedGraphicsAdapters().Any(s => graphicsAdapter.Name.ToLower().Contains(s)))
                    graphicsAdapter.SupportLevel = SupportLevel.Partial;
                else
                    graphicsAdapter.SupportLevel = SupportLevel.None;
            }

            // Orders the graphics adapters by support level (first graphics adapter should be most suitable).
            graphicsAdapters.OrderBy(o => o.SupportLevel).Reverse().ToList();

            // Returns the most suitable graphics adapter.
            return graphicsAdapters[0];
        }

        List<string> FullySupportedGraphicsAdapters()
        {
            List<string> str = new List<string>();
            str.Add("rtx");

            return str;
        }

        List<string> PartiallySupportedGraphicsAdapters()
        {
            List<string> str = new List<string>();
            str.Add("1030");
            str.Add("1040");
            str.Add("1050");
            str.Add("1060");
            str.Add("1070");
            str.Add("1080");
            str.Add("titan");
            str.Add("1640");
            str.Add("1650");
            str.Add("1660");
            str.Add("1670");
            str.Add("1680");

            return str;
        }

        private void ForceModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            forceMode = (bool)forceModeCheckBox.IsChecked;
            UpdateOptions();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartImageProcessing();
        }

        async void StartImageProcessing()
        {
            if (!systemCheckComplete)
            {
                MessageBox.Show("System check not complete. If this keeps occuring, please restart the app.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (imagePaths.Count < 1)
            {
                MessageBox.Show("An image must be selected first.", "FYI", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!File.Exists(imagePaths[0]))
            {
                MessageBox.Show("Could not access the selected image.\n\nDoes the image exist?\nAm I being blocked by security?", "Cannot access image", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            string time = DateTime.Now.ToString("yyyy MM dd HH mm ss");
            int resolutionFactor = (int)Math.Pow(2, resolutionScaleComboBox.SelectedIndex + 1);
            string colourMode = (colourModeComboBox.SelectedItem == "Colour") ? "2" : "1";
            bool limitSize = this.MaxSizeModeCheckBox.IsChecked == true;

            // Reset
            tasksCompleted = 0;
            failedImages = 0;

            // Deactivates user input
            startButton.IsEnabled = false;
            startButton.Content = $"Processing... ({tasksCompleted + 1}/{imagePaths.Count})";
            browseImagesButton.IsEnabled = false;
            clearImagesButton.IsEnabled = false;
            resolutionScaleComboBox.IsEnabled = false;
            colourModeComboBox.IsEnabled = false;

            var progress = new Progress<int>(progressValue =>
            {
                Debug.Write(progressValue);
                startButton.Content = $"Processing... ({progressValue++}/{imagePaths.Count})";
            });

            await Task.Run(() => ProcessImage(time, resolutionFactor, colourMode, limitSize, progress));

            // Activates user input
            startButton.IsEnabled = true;
            startButton.Content = "Enhance";
            browseImagesButton.IsEnabled = true;
            clearImagesButton.IsEnabled = true;
            resolutionScaleComboBox.IsEnabled = true;
            colourModeComboBox.IsEnabled = true;

            // If no images failed, show a success image.
            if (failedImages <= 0)
                MessageBox.Show($"All images have enhanced!", "Process complete", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            else
                MessageBox.Show($"{failedImages} images failed to enhance!", "Process complete", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // Processes the selected images into the NVIDIA Ansel AI Up-Res app in a multithreaded way.
        async Task ProcessImage(string time, int defaultResolutionFactor, string colourMode, bool limitSize, IProgress<int> progress)
        {
            string tempDirectoryName = string.Empty;

            // If output folder mode is on, it will create a new folder and use it as its directory.
            if (outputFolderMode)
            {
                tempDirectoryName = Path.Combine(Path.GetDirectoryName(imagePaths[0]), $"Enhanced Output {time}");
                Directory.CreateDirectory(tempDirectoryName);
            }

            await Task.Run(() =>
            {
                // Divides the task into many parallel tasks (multithreading).
                Parallel.ForEach(imagePaths, new ParallelOptions()
                {
                    // The user selected thread count becomes the max possible threads the following tasks will use at once.
                    MaxDegreeOfParallelism = threadCount
                }, (sourceImagePath) =>
                {
                    string withoutExtension = Path.GetFileNameWithoutExtension(sourceImagePath);
                    string directoryName = Path.GetDirectoryName(sourceImagePath);
                    string extension = Path.GetExtension(sourceImagePath);
                    int currentResolutionFactor = defaultResolutionFactor; // Copy of resolution (in case temp change to res is needed)
                    Image sourceImage = Image.FromFile(sourceImagePath);
                    bool isTransparentImage = TransparencySupport.HasTransparency(sourceImage);

                    // Checks if image height/width will overgrow (8000x8000) which is well.. BIG - modifies resolution factor
                    // with the ability to ignore it.
                    if (limitSize)
                    {
                        int largestUpscaledDimension = (Math.Max(sourceImage.Width, sourceImage.Height) * defaultResolutionFactor);

                        // Calculate new resolution factor to max out to be 8000px.
                        if (largestUpscaledDimension >= 8000)
                        {
                            currentResolutionFactor = (int)Math.Floor(defaultResolutionFactor / (largestUpscaledDimension / 8000d));

                            // Floors the new resolution factor down to the closest power of 2.
                            currentResolutionFactor = (int)Math.Pow(2, (int)Math.Log(currentResolutionFactor, 2));
                            MessageBox.Show(currentResolutionFactor.ToString());
                        }
                    }
                    sourceImage.Dispose();

                    // Resolution factor is lower than what the upscaler than handle.
                    if (currentResolutionFactor < 2)
                    {
                        failedImages++;
                        tasksCompleted++;

                        // Updates the program's progress to the UI.
                        if (progress != null)
                            progress.Report(tasksCompleted);

                        return;
                    }

                    if (!outputFolderMode)
                        tempDirectoryName = directoryName;

                    // Gives the correct colour mode labelling.
                    string colourModeStr;
                    if (colourMode == 1.ToString())
                        colourModeStr = "Greyscale";
                    else
                        colourModeStr = "Colour";

                    Func<string, bool, string, string> startUpscale = delegate (string args, bool isMove, string customName)
                     {
                         // Starts the NVIDIA Ansel AI Up-Res app and insert the selected image into it.
                         using (Process process = new Process())
                         {
                             process.StartInfo.FileName = "C:/Program Files/NVIDIA Corporation/NVIDIA NvDLISR/nvdlisrwrapper.exe";
                             process.StartInfo.Arguments = args;
                             process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                             process.Start();
                             process.WaitForExit();
                         }

                         try
                         {
                             // Dynamiquel: "Not sure this is an ideal place for this but whatever." Credit: OlympicAngel.
                             string defaultPath = Path.Combine(directoryName, withoutExtension);
                             string newPath = $"{defaultPath}_{colourModeStr}_x{currentResolutionFactor * 2}{extension}";

                             if (!isMove)
                             {
                                 string customPath = $"{defaultPath}_{customName}";

                                 if (File.Exists(customPath))
                                     File.Delete(customPath);

                                 File.Move(defaultPath, customPath);

                                 return customPath;
                             }

                             if (File.Exists(newPath))
                                 File.Delete(newPath);

                             File.Move(defaultPath, newPath);

                             return newPath;
                         }
                         catch (Exception ex)
                         {
                             failedImages++;

                             MessageBox.Show($"This could either be due to NVIDIA's API memory limitations (try a lower resolution) or something to do with file write permissions.\n\n{ex}", 
                                 "An error has occured!", MessageBoxButton.OK, MessageBoxImage.Hand);

                                return "";
                         }
                     };

                    // Creates the command needed to put into the NVIDIA app. 'url 2/4 1/2'
                    string command = $"{sourceImagePath} {currentResolutionFactor} {colourMode}";

                    // Preform the normal upscale as normal (if img has transparency dont yet move it to user's location)
                    string upscaledImagePath = startUpscale(command, !isTransparentImage, "normal");

                    if (isTransparentImage)
                    {
                        Bitmap transUpscaled = null;

                        try
                        {
                            transUpscaled = TransparencySupport.UpscaleWithAlpha(upscaledImagePath, sourceImagePath, currentResolutionFactor, startUpscale);
                            string newPath = Path.Combine(tempDirectoryName, $"{withoutExtension}_trans_{colourModeStr}_x{currentResolutionFactor * 2}{extension}");
                            transUpscaled.Save(newPath);
                        }
                        catch (Exception ex)
                        {
                            failedImages++;

                            MessageBox.Show($"An error has occured during the transparency merge stage.\n\n{ex}", 
                                "An error has occured!", MessageBoxButton.OK, MessageBoxImage.Hand);
                        }
                        finally
                        {
                            transUpscaled?.Dispose();
                        }
                    }

                    tasksCompleted++;

                    // Updates the program's progress to the UI.
                    if (progress != null)
                        progress.Report(tasksCompleted);
                });
            });
        }

        private void BrowseImagesButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseImage();
        }

        // Allows the user to select images.
        void BrowseImage()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Select images (to reduce errors, do not up-res an image that will result in a resolution over 9000x9000)";
            openFileDialog.DefaultExt = ".png";
            openFileDialog.Filter = "PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg";
            openFileDialog.ShowDialog();

            foreach (string fileName in openFileDialog.FileNames)
                if (!imagePaths.Contains(fileName))
                    imagePaths.Add(fileName);

            RefreshImageGrid();
        }

        // Refreshes the image grid to ensure it alligns correctly with the image list.
        void RefreshImageGrid()
        {
            if (imagePaths.Count > 0)
            {
                image3L.Visibility = Visibility.Hidden;
                image3L.Content = "";
                image3L.ToolTip = "";
                ImageSource imageSource = new BitmapImage(new Uri(imagePaths[0]));
                image.Source = imageSource;
                imageB.Height = 100;
                imageB.Width = 100;

                if (imagePaths.Count > 1)
                {
                    ImageSource imageSource1 = new BitmapImage(new Uri(imagePaths[1]));
                    image1.Source = imageSource1;
                    image1B.Height = 100;
                    image1B.Width = 50;
                    imageB.Height = 100;
                    imageB.Width = 50;

                    if (imagePaths.Count > 2)
                    {
                        ImageSource imageSource2 = new BitmapImage(new Uri(imagePaths[2]));
                        image2.Source = imageSource2;
                        image2B.Height = 50;
                        image2B.Width = 50;
                        imageB.Height = 50;

                        if (imagePaths.Count > 3)
                        {
                            image1B.Height = 50;
                            image3B.Height = 50;
                            image3B.Width = 50;
                            image2B.Height = 50;

                            if (imagePaths.Count < 5)
                            {
                                ImageSource imageSource3 = new BitmapImage(new Uri(imagePaths[3]));
                                image3.Source = imageSource3;
                            }
                            else
                            {
                                image3.Source = null;
                                if (imagePaths.Count < 104)
                                    image3L.Content = $"+{imagePaths.Count - 3}";
                                else
                                    image3L.Content = $"+100";

                                image3L.ToolTip = $"+{imagePaths.Count - 3} other images";

                                image3L.Visibility = Visibility.Visible;
                            }
                        }
                    }
                }
            }
        }

        private void OutputFolderModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            outputFolderMode = (bool)outputFolderModeCheckBox.IsChecked;

            if (Properties.Settings.Default == null)
                return;

            Properties.Settings.Default.OutputFolderMode = outputFolderMode;
            Properties.Settings.Default.Save();
        }

        private void ClearImagesButton_Click(object sender, RoutedEventArgs e)
        {
            ClearImages();
        }

        void ClearImages()
        {
            if (imagePaths.Count > 0)
            {
                imagePaths.Clear();
                image.Source = null;
                imageB.Height = 0;
                imageB.Width = 0;
                image1.Source = null;
                image1B.Height = 0;
                image1B.Width = 0;
                image2.Source = null;
                image2B.Height = 0;
                image2B.Width = 0;
                image3.Source = null;
                image3B.Height = 0;
                image3B.Width = 0;
                image3L.Content = null;
                image3L.ToolTip = null;
                image3L.Visibility = Visibility.Hidden;
            }
        }

        private void ThreadsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            threadCount = threadsComboBox.SelectedIndex + 1;

            if (Properties.Settings.Default == null)
                return;

            Properties.Settings.Default.ThreadCount = threadCount;
            Properties.Settings.Default.Save();
        }

        private void MaxSizeModeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default == null)
                return;

            Properties.Settings.Default.ResolutionLimiter = (bool)MaxSizeModeCheckBox.IsChecked;
            Properties.Settings.Default.Save();
        }
    }
}

class GraphicsAdapter
{
    public string Name { get; set; }
    public string DriverVersion { get; set; }
    public SupportLevel SupportLevel { get; set; }

    public GraphicsAdapter(string name)
    {
        Name = name;
    }
}

enum SupportLevel
{
    None,
    Partial,
    Full
}