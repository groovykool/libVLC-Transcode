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
      //Setup LibVLC objects
      RecordInit();
    }

    private void RecordInit()
    {
      StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;

      var pathstring = tempFolder.Path + "\\recordtemp.mp4";


      //var urlstring = "http://194.103.218.16/mjpg/video.mjpg";
      //var urlstring = "rtsp://public:public@hyc.homeip.net/cam/realmonitor?channel=1&subtype=1";
      var urlstring = "http://24.43.239.50/mjpg/video.mjpg";
      //var urlstring = "rtsp://:@tonyw.selfip.com:6001/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif";
      rLIB = new LibVLC();
      mprec = new MediaPlayer(rLIB);
      mrec = new Media(rLIB, urlstring, FromType.FromLocation);
      //assign log event handler
      rLIB.Log += (sender, ee) => log(ee);
      //sout=#transcode{vcodec=hevc,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}:std{access=file{no-overwrite},mux=mp4,dst='C:/Users/groov/Videos/tester3.mp4'}
      //transcode{vcodec=theo,vb=800,scale=Auto,acodec=vorb,ab=128,channels=2,samplerate=8000,scodec=none}:std{access=file{no-overwrite},mux=ogg,dst='C:/Users/groov/Videos/test3.ogv'}'
      //var option = $":sout=#transcode{{vcodec=hevc,vb=800,scale=Auto,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=mp4,dst={pathstring}}}";
      //var option = $":sout=#file{{mux=mp4,dst={pathstring}}}";
      //var option = $":sout=#transcode{{vcodec=mp2v,vb=3000,scale=Auto,acodec=mpga,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=ts,dst={pathstring}}}";
      var option = $":sout=#transcode{{vcodec=mp4v,vb=2000,scale=Auto,acodec=mp4a,ab=128,channels=2,samplerate=44100,scodec=none}}:std{{access=file,mux=mp4,dst={pathstring}}}";
      OT.Text += $"Option was added: {option} \n";
      mrec.AddOption(option);
      //mrec.AddOption(":sout-keep");

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

      //Debug.WriteLine($"[{ee.Level}] {ee.Module}:{ee.Message}");
      //Run on UIthread to write to control
      lines++;
      
      await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
      {
        if (lines > 1000)
        {
          OT.Text = "";
          lines = 0;
        }
        OT.Text += $"libVLC:[Line:{lines}][{ee.Level}] {ee.Module}:{ee.Message}\n";

      });

    }
    private void Pause_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      mprec.Pause();
      OT.Text += "MediaPlayer was Paused \n";
    }

    private void Stop_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      RecStop();
    }

    private async void RecStop()
    {
      mprec.Stop();
      OT.Text += "MediaPlayer was Stopped \n";
      DateTimeOffset dateOffset1;
      dateOffset1 = DateTimeOffset.Now;
      StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;
      StorageFile tempImage = await tempFolder.GetFileAsync("recordtemp.mp4");
      var tempProperties = await tempImage.GetBasicPropertiesAsync();
      var fsize = tempProperties.Size;
      Debug.WriteLine(fsize.ToString());
      if (fsize < 5000)
      {
        OT.Text += "Record File Size: " + fsize.ToString() + "  *Record Failed*  " + "\n";
      }
      else
      {
        OT.Text += "Record File Size: " + fsize.ToString() + "\n";
        StorageFolder picFolder = KnownFolders.VideosLibrary;
        StorageFolder newFolder = await picFolder.CreateFolderAsync("libVLC-Transcode", CreationCollisionOption.OpenIfExists);
        //append the date and time to the snapshot file.
        var fname = "video" + dateOffset1.ToString("yyyy-MM-dd-HH-mm-ss-FFF") + ".mp4";
        StorageFile copiedFile = await tempImage.CopyAsync(newFolder, fname, NameCollisionOption.ReplaceExisting);
        await tempImage.DeleteAsync();
        if (copiedFile.IsAvailable)
        {
          OT.Text += "Record Success: File Saved in Pictures Library:libVLC-Transcode " + copiedFile.Name + "\n";
        }
      }




    }
    public event PropertyChangedEventHandler PropertyChanged;

    private void PlayRec_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
      mprec.Play(mrec);
      OT.Text += "MediaPlayer is Playing \n";
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
  }
}
