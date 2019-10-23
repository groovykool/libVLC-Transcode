using LibVLCSharp.Shared;
using System;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using libVLC_Transcode.Helpers;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace libVLC_Transcode.Views
{
  public sealed partial class MainPage : Page
  {

    MediaPlayer mprec;
    LibVLC rLIB;
    Media mrec;
    int lines = 0;
    string option = "", pathstring = "", fname = "",  urlstring = "";
    StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
    double lasttime = 0, time = 0;
    private ObservableCollection<Results> resultlist = new ObservableCollection<Results>();
    private ObservableCollection<string> CBurlSource = new ObservableCollection<string>();
    ScrollViewer OTscroll;

    public MainPage()
    {
      InitializeComponent();
      //Assign event handlers
      Loaded += MainPage_Loaded;
      Unloaded += MainPage_Unloaded;
    }

    private void MainPage_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      RecordDipose();
    }

    private void MainPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      OT.Text += "\n";
      InitCBbox();
      InitAutoScroll();
    }

    
    private async void Mprec_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
    {
      time = (double)e.Time / 1000;
      if (time > (lasttime + 10))
      {
        lasttime = time;
        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
          OT.Text += $"\nVideo Time {time} Sec\n";
        });
      }
    }

    private void RecordDipose()
    {
      mprec.Stop();
      mprec.Dispose();
      mrec.Dispose();
      rLIB.Dispose();
    }

    public async Task log(LogEventArgs ee)
    {
      //Run on UIthread to write to control
      await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
      {
        lines++;
        OT.Text += $"libVLC:{lines}[{ee.Level}] {ee.Module}:{ee.Message}\n";
      });

    }
    private void Pause_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      mprec.Pause();
      OT.Text += "\nMediaPlayer was Paused \n\n";
    }

    private async void Stop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      await RecStop();
    }

    private async Task RecStop()
    {
      mprec.Stop();
      OT.Text += "\nMediaPlayer was Stopped \n\n";
      lasttime = 0;

      StorageFile temp = await tempFolder.GetFileAsync(fname);
      var tempProperties = await temp.GetBasicPropertiesAsync();
      var fsize = tempProperties.Size;

      if (fsize < 5000)
      {
        OT.Text += $"\nRecord File Size: {fsize} *Record Failed*  \n\n";
        var res = new Results { option = (string)CB.SelectedItem, filename = "Record FAILED" };
        resultlist.Add(res);
      }
      else
      {
        StorageFolder picFolder = KnownFolders.VideosLibrary;
        StorageFolder newFolder = await picFolder.CreateFolderAsync("libVLC-Transcode", CreationCollisionOption.OpenIfExists);
        StorageFile copiedFile = await temp.CopyAsync(newFolder, fname, NameCollisionOption.GenerateUniqueName);
        var videoProperties = await temp.Properties.GetVideoPropertiesAsync();
        await temp.DeleteAsync();

        if (copiedFile.IsAvailable)
        {
          OT.Text += "\n";
          OT.Text += $"Record Success: File Saved in Pictures Library:libVLC-Transcode: {copiedFile.Name}\n";
          OT.Text += $"Record File Size: {fsize}\n\n";
          var vlctime = new TimeSpan(0, 0, (int)time);
          var ts = videoProperties.Duration;
          var dur = new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);
          OT.Text += $"Video Duration: {dur}     LibVLC Time:  {vlctime}\n\n";
          var res = new Results { option = (string)CB.SelectedItem, filename = copiedFile.Name, filesize = (uint)fsize, duration = dur, expected = vlctime };
          resultlist.Add(res);
        }
      }
      LV.ItemsSource = resultlist;
      RecordDipose();
      time = 0;
      Pause.IsEnabled = false;
      Stop.IsEnabled = false;
      PlayRec.IsEnabled = true;

    }


    private async void PlayRec_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      PlayRec.IsEnabled = false;
      OT.Text = "***Start of Log***\n";
      //Get transcode selection and setup
      switch (CB.SelectedItem)
      {
        case "no transcode mp4":
          fname = "video-none" + ".mp4";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#file{{dst={pathstring}}}";
          OT.Text += "no transcode mp4 was selected\n\n";
          break;
        case "transcode mp2v ts":
          fname = "video-mp2v" + ".ts";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=mp2v,vb=2000,scale=Auto,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=ts,dst={pathstring}}}";
          OT.Text += "transcode mp2v ts was selected\n\n";
          break;
        case "transcode mp4v mp4":
          fname = "video-mp4v" + ".mp4";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=mp4v,vb=2000,scale=Auto,acodec=mp4a,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=mp4,dst={pathstring}}}";
          OT.Text += "transcode mp4v mp4 was selected\n\n";
          break;
        case "transcode theo ogg":
          fname = "video-theo" + ".ogv";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=theo,vb=2000,scale=Auto,acodec=vorb,ab=128,channels=2,samplerate=8000,scodec=none}}:std{{access=file,mux=ogg,dst={pathstring}}}";
          OT.Text += "transcode theo ogg was selected\n\n";
          break;
        default:
          break;
      }

      await RecordInit();
      mprec.Play(mrec);
      OT.Text += "MediaPlayer is Playing \n\n";
      Pause.IsEnabled = true;
      Stop.IsEnabled = true;
    }

    private Task RecordInit()
    {

      //urlstring = "http://194.103.218.16/mjpg/video.mjpg";
      //urlstring = "rtsp://public:public@hyc.homeip.net/cam/realmonitor?channel=1&subtype=1";
      //urlstring = "http://24.43.239.50/mjpg/video.mjpg";
      //urlstring = "rtsp://:@tonyw.selfip.com:6001/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif";
      var liboptions = new string[]
                {
                    $"--no-osd",
                    $"--no-spu",
                    $"--realrtsp-caching=500",
                    $"--udp-caching=500",
                    $"--tcp-caching=500",
                    $"--sout-file-overwrite",
                    $"--network-caching=1666",
                    $"--rtsp-tcp"
                };

      rLIB = new LibVLC(liboptions);
      //assign log event handler
      rLIB.Log += async (sender, ee) => await log(ee);
      mprec = new MediaPlayer(rLIB);
      mprec.TimeChanged += Mprec_TimeChanged;
      urlstring = (string)CBurl.SelectedValue;
      mrec = new Media(rLIB, urlstring, FromType.FromLocation);
      mrec.AddOption(option);
      OT.Text += $"\nOption was added: {option}\n";
      //mrec.AddOption(":sout-keep");
      return Task.CompletedTask;

    }
    private void OT_TextChanged(object sender, TextChangedEventArgs e)
    {
      OTscroll.ChangeView(0.0f, OTscroll.ExtentHeight, 1.0f);
    }

    private void InitAutoScroll()
    {
      //Keep log text box scrolled to the bottom
      var grid = (Grid)VisualTreeHelper.GetChild(OT, 0);
      for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
      {
        object obj = VisualTreeHelper.GetChild(grid, i);
        if (!(obj is ScrollViewer)) continue;
        OTscroll = (ScrollViewer)obj;
        break;
      }

    }
    private void InitCBbox()
    {
      CBurlSource.Add("rtsp://:@tonyw.selfip.com:6001/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif");
      CBurlSource.Add("http://194.103.218.16/mjpg/video.mjpg");
    }
  }
}
