using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libVLC_Transcode.Helpers
{
  public class Results
  {
    public string option;
    public uint filesize;
    public TimeSpan duration;
    public TimeSpan expected;
    public string filename;

    public Results()
    {
      option = "";

    }

  }
}
