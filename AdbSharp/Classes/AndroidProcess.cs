using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public class AndroidProcess
    {

        public string User
        {
            get;
            set;
        }

        public int Pid
        {
            get;
            set;
        }

        public int PPid
        {
            get;
            set;
        }

        public long VSize
        {
            get;
            set;
        }

        public int Rss
        {
            get;
            set;
        }

        public string Wchan
        {
            get;
            set;
        }

        public string PC
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

    }

}
