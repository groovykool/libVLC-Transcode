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
using System.Threading;

namespace libVLC_Transcode.Views
{
  public sealed partial class MainPage : Page
  {

    MediaPlayer mprec;
    LibVLC rLIB;
    Media mrec;
    int lines = 0;
    string option = "", pathstring = "", fname = "", urlstring = "",selapsed="";
    StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
    DateTimeOffset time,lasttime,starttime;
    private ObservableCollection<Results> resultlist = new ObservableCollection<Results>();
    private ObservableCollection<string> CBurlSource = new ObservableCollection<string>();
    Timer timer;
    bool recdone = false;
    


    public MainPage()
    {
      InitializeComponent();
      //Assign event handlers
      Loaded += MainPage_Loaded;
      Unloaded += MainPage_Unloaded;
      Scroll.ViewChanged += Scroll_ViewChanged;
      
    }

    private void Scroll_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
      if(recdone)
      {
        Scroll.ChangeView(0.0f, Scroll.ExtentHeight, 1.0f);
        recdone = false;
      }
    }

    private void MainPage_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      RecordDipose();
    }

    private void MainPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      OT.Text += "\n";
      InitCBbox();

    }


    private void RecordDipose()
    {
      mprec.Stop();
      mprec.Dispose();
      mrec.Dispose();
      rLIB.Dispose();
    }


    private void Pause_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      mprec.Pause();
      timer.Dispose();
      OT.Text += "MediaPlayer was Paused and Play timer Diposed. \n";
     
    }

    private async void Stop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      await RecStop();
      recdone = true;
      Scroll.ChangeView(0.0f, Scroll.ExtentHeight, 1.0f);
    }

    private async Task RecStop()
    {
      mprec.Stop();
      timer.Dispose();
      OT.Text += "MediaPlayer was Stopped \n";

      StorageFile temp = await tempFolder.GetFileAsync(fname);
      var tempProperties = await temp.GetBasicPropertiesAsync();
      var fsize = tempProperties.Size;

      if (fsize < 5000)
      {
        OT.Text += $"Record File Size: {fsize} *Record Failed*  \n";
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
          //OT.Text += "\n";
          OT.Text += $"Record Success: File Saved in Pictures Library:libVLC-Transcode: {copiedFile.Name}\n";
          OT.Text += $"Record File Size: {fsize}\n";
          //var vlctime = new TimeSpan(0, 0, (int)time);
          var dur = videoProperties.Duration.ToString(@"hh\:mm\:ss");
          
          OT.Text += $"Video Duration: {dur} Play Time: {selapsed}\n\n\n";
          var res = new Results { option = (string)CB.SelectedItem, filename = copiedFile.Name, filesize = (uint)fsize, duration = dur, expected=selapsed };
          resultlist.Add(res);
          //Scroll.ChangeView(0.0f, Scroll.ExtentHeight, 1.0f);

        }
      }
      LV.ItemsSource = resultlist;
      RecordDipose();
      Pause.IsEnabled = false;
      Stop.IsEnabled = false;
      PlayRec.IsEnabled = true;
      Scroll.ChangeView(0.0f, Scroll.ExtentHeight, 1.0f);
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
          OT.Text += "'no transcode mp4' was selected\n";
          break;
        case "transcode mp2v ts":
          fname = "video-mp2v" + ".ts";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=mp2v,vb=2000,scale=Auto,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=ts,dst={pathstring}}}";
          OT.Text += "'transcode mp2v ts' was selected\n";
          break;
        case "transcode mp4v mp4":
          fname = "video-mp4v" + ".mp4";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=mp4v,vb=2000,scale=Auto,acodec=mp4a,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=mp4,dst={pathstring}}}";
          OT.Text += "'transcode mp4v mp4' was selected\n";
          break;
        case "transcode theo ogg":
          fname = "video-theo" + ".ogv";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=theo,vb=2000,scale=Auto,acodec=vorb,ab=128,channels=2,samplerate=8000,scodec=none}}:std{{access=file,mux=ogg,dst={pathstring}}}";
          OT.Text += "'transcode theo ogg' was selected\n";
          break;
        default:
          break;
      }

      await RecordInit();
      mprec.Play(mrec);
      PlayTimerSetup();
      OT.Text += "MediaPlayer is Playing \n";
      Pause.IsEnabled = true;
      Stop.IsEnabled = true;
    }

    private Task RecordInit()
    {

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

      if (libLog.IsChecked == true)
      {
        //assign log event handler
        rLIB.Log += async (sender, ee) => await log(ee);
      }

      mprec = new MediaPlayer(rLIB);
      urlstring = (string)CBurl.SelectedValue;
      mrec = new Media(rLIB, urlstring, FromType.FromLocation);
      mrec.AddOption(option);
      OT.Text += $"Option was added:\n{option}\n";
      //mrec.AddOption(":sout-keep");
      return Task.CompletedTask;

    }
    public async Task log(LogEventArgs ee)
    {
      lines++;
      var mess = $"libVLC:{lines}[{ee.Level}] {ee.Module}:{ee.Message}\n";
      //Run on UIthread to write to control
      await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
      {
        OT.Text += mess;
        Scroll.ChangeView(0.0f, Scroll.ExtentHeight, 1.0f);
      });


    }


    private void InitCBbox()
    {
      CBurlSource.Add("rtsp://:@tonyw.selfip.com:6001/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif");
      CBurlSource.Add("rtsp://public:public@hyc.homeip.net/cam/realmonitor?channel=1&subtype=1");
      CBurlSource.Add("rtsp://b1.dnsdojo.com:1935/live/sys3.stream");
      CBurlSource.Add("http://64.118.25.194/mjpg/video.mjpg");
      CBurlSource.Add("http://201.202.191.78:8083/mjpg/video.mjpg");
      CBurlSource.Add("http://h2owebcam.axiscam.net/mjpg/video.mjpg");
      CBurlSource.Add("http://91.133.75.164/mjpg/video.mjpg");
      CBurlSource.Add("http://82.150.206.177/cgi-bin/faststream.jpg?stream=half&fps=15&rand=COUNTER");
      CBurlSource.Add("http://114.179.87.1:8080/-wvhttp-01-/GetOneShot?image_size=640x480&frame_count=1000000000");
      CBurlSource.Add("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/TearsOfSteel.mp4");
    }
    public void PlayTimerSetup()
    {
      starttime = DateTimeOffset.Now;
      lasttime = starttime;
      var period = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;
      timer = new Timer(TimerAsync, null, period, period);
    }

    private async void TimerAsync(object state)
    {
      time = DateTimeOffset.Now;
      selapsed = (time - starttime).ToString(@"hh\:mm\:ss");
      //elapsed = new TimeSpan(elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
      if ((time - lasttime).TotalSeconds > 10.0)
      {
        lasttime = time;
        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {

              OT.Text += $"Video Play Timer: {selapsed}\n";
              Scroll.ChangeView(0.0f, Scroll.ExtentHeight, 1.0f);

            });
      }
      

    }
  }
}
