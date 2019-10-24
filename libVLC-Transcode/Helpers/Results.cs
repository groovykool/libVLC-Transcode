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
    public string duration;
    public string expected;
    public string filename;

    public Results()
    {
      option = "";

    }

  }
}
