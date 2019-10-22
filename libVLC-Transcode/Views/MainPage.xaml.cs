using LibVLCSharp.Shared;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Core;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace libVLC_Transcode.Views
{
  public sealed partial class MainPage : Page, INotifyPropertyChanged
  {

    MediaPlayer mprec;
    LibVLC rLIB;
    Media mrec;
    int lines = 0;
    string option = "",pathstring="",fname="",cbmess="";
    StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
    double lasttime = 0;
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
      OT.Text += "\n" + cbmess;
      //Setup LibVLC objects
      //RecordInit();
    }

    private void RecordInit()
    {

      
      //pathstring = tempFolder.Path + "\\recordtemp.ts";


      //var urlstring = "http://194.103.218.16/mjpg/video.mjpg";
      //var urlstring = "rtsp://public:public@hyc.homeip.net/cam/realmonitor?channel=1&subtype=1";
      //var urlstring = "http://24.43.239.50/mjpg/video.mjpg";
      var urlstring = "rtsp://:@tonyw.selfip.com:6001/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif";
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
      mprec = new MediaPlayer(rLIB);
      mprec.TimeChanged += Mprec_TimeChanged;
      mrec = new Media(rLIB, urlstring, FromType.FromLocation);
      //assign log event handler
      rLIB.Log += (sender, ee) => log(ee);
      //sout=#transcode{vcodec=hevc,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}:std{access=file{no-overwrite},mux=mp4,dst='C:/Users/groov/Videos/tester3.mp4'}
      //transcode{vcodec=theo,vb=800,scale=Auto,acodec=vorb,ab=128,channels=2,samplerate=8000,scodec=none}:std{access=file{no-overwrite},mux=ogg,dst='C:/Users/groov/Videos/test3.ogv'}'
      //var option = $":sout=#transcode{{vcodec=hevc,vb=800,scale=Auto,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=mp4,dst={pathstring}}}";
      //var option = $":sout=#file{{dst={pathstring}}}";
      //var option = $":sout=#transcode{{vcodec=mp2v,vb=3000,scale=Auto,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=ts,dst={pathstring}}}";
      //`transcode{vcodec=mp2v,vb=3000,scale=Auto,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}:std{access=file{no-overwrite},mux=ts,dst='C:/Users/groov/Videos/Codec-Container-Tests/testmpeg2-2.ts'}'
      //var option = $":sout=#transcode{{vcodec=mp4v,vb=2000,scale=Auto,acodec=mp4a,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=mp4,dst={pathstring}}}";
      OT.Text += $"\n\nOption was added: {option} \n\n";
      mrec.AddOption(option);
      //mrec.AddOption(":sout-keep");

    }

    private async void Mprec_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
    {
      var time = (double)e.Time/1000;
      if (time > (lasttime + 10))
      {
        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
          OT.Text += $"\nVideo Time {time} Sec\n";
          lasttime = time;
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

    public async void log(LogEventArgs ee)
    {

      
      //Run on UIthread to write to control
      
      await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
      {
        if (lines > 2000)
        {
          OT.Text = "";
          lines = 0;
        }
        lines++;
        OT.Text += $"libVLC:{lines}[{ee.Level}] {ee.Module}:{ee.Message}\n";

      });

    }
    private void Pause_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      mprec.Pause();
      OT.Text += "\n\nMediaPlayer was Paused \n\n";
    }

    private void Stop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      RecStop();
    }

    private async void RecStop()
    {
      mprec.Stop();
      OT.Text += "\n\nMediaPlayer was Stopped \n\n";
      lasttime = 0;
      //StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
      StorageFile temp= await tempFolder.GetFileAsync(fname);
      var tempProperties = await temp.GetBasicPropertiesAsync();
      var fsize = tempProperties.Size;
      Debug.WriteLine(fsize.ToString());
      if (fsize < 5000)
      {
        OT.Text += $"\n\nRecord File Size: {fsize} *Record Failed*  \n\n";
      }
      else
      {
        OT.Text += $"\n\nRecord File Size: {fsize}\n\n";
        StorageFolder picFolder = KnownFolders.VideosLibrary;
        StorageFolder newFolder = await picFolder.CreateFolderAsync("libVLC-Transcode", CreationCollisionOption.OpenIfExists);
        //append the date and time to the snapshot file.
        
        StorageFile copiedFile = await temp.CopyAsync(newFolder, fname, NameCollisionOption.GenerateUniqueName);
        await temp.DeleteAsync();
        if (copiedFile.IsAvailable)
        {
          OT.Text += "\n\nRecord Success: File Saved in Pictures Library:libVLC-Transcode " + copiedFile.Name + "\n\n";
        }
      }




    }
    public event PropertyChangedEventHandler PropertyChanged;

    private void PlayRec_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      RecordInit();
      mprec.Play(mrec);
      OT.Text += "\n\nMediaPlayer is Playing \n\n";
    }

    private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
    {
      if (Equals(storage, value))
      {
        return;
      }

      storage = value;
      OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void OT_TextChanged(object sender, TextChangedEventArgs e)
    {

      //Keep log text box scrolled to the bottom
      var grid = (Grid)VisualTreeHelper.GetChild(OT, 0);
      for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
      {
        object obj = VisualTreeHelper.GetChild(grid, i);
        if (!(obj is ScrollViewer)) continue;
        ((ScrollViewer)obj).ChangeView(0.0f, ((ScrollViewer)obj).ExtentHeight, 1.0f);
        break;
      }

    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      switch (CB.SelectedItem)
      {
        case "no transcode mp4":
          fname = "video-none" + ".mp4";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#file{{dst={pathstring}}}";
          if (OT == null)
          {
            cbmess += "\n\nno transcode mp4 was selected\n\n";
          }
          else
          {
            OT.Text+= "\n\nno transcode mp4 was selected\n\n";
          }
          break;
        case "transcode mp2v ts":
          fname = "video-mp2v" + ".ts";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=mp2v,vb=3000,scale=Auto,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=ts,dst={pathstring}}}";
          OT.Text += "\n\ntranscode mp2v ts was selected\n\n";
          break;

        case "transcode mp4v mp4":
          fname = "video-mp4v" + ".mp4";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=mp4v,vb=2000,scale=Auto,acodec=mp4a,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=mp4,dst={pathstring}}}";
          OT.Text += "\n\ntranscode mp4v mp4 was selected\n\n";
          break;

        case "transcode theo ogg":
          fname = "video-theo" + ".ogg";
          pathstring = tempFolder.Path + "\\" + fname;
          option = $":sout=#transcode{{vcodec=theo,vb=2000,scale=Auto,acodec=vorb,ab=128,channels=2,samplerate=8000,scodec=none}}:std{{access=file,mux=ogg,dst={pathstring}}}";
          OT.Text += "\n\ntranscode theo ogg was selected\n\n";
          break;

        default:
          break;
      }
    }
  }
}
