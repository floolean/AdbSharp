using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public enum EventType
    {
        Abs = 3,
        Syn = 0
    }

    public enum AbsEventParam
    {
        MultiTouchTrackingId = 57,
        MultiTouchPositionX = 53,
        MultiTouchPositionY = 54,
        MultiTouchTouchMajor = 48,
        MultiTouchPressure = 58
    }

    public enum SynEventParam
    {
        MultiTouchReport = 2,
        Report = 0
    }

}
