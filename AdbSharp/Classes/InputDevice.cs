using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public class InputDevice
    {

        public string Name
        {
            get;
            set;
        }

        public string Path
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, Path);
        }

    }
}
