using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public class DirectoryListing
    {

        //public string[] Directories
        //{
        //    get;
        //}

        //internal DirectoryListing( string name, string

        List<Entry> _entries = new List<Entry>();

        public string Path
        {
            get;
            internal set;
        }

        public IEnumerable<Entry> Entries
        {
            get
            {
                return _entries.AsEnumerable();
            }
        }

        public IEnumerable<Entry> Files
        {
            get
            {
                return _entries.Where(entry => !entry.IsDirectory);
            }
        }

        public IEnumerable<Entry> Directories
        {
            get
            {
                return _entries.Where(entry => entry.IsDirectory);
            }
        }

        internal void AddEntry(string lsOutputLine)
        {

            var tokens = lsOutputLine.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);

            var permissionsLine = tokens[0];

            var isFile = permissionsLine[0] == '-';

            var isDirectory = permissionsLine[0] == 'd';

            var isSymbolic = permissionsLine[0] == 'l';

            var isSpecial = permissionsLine[0] == 's';

            var owner = tokens[1];

            var group = tokens[2];

            var size = !isFile ? -1 : long.Parse(tokens[3]);

            var timestampIndex = !isFile ? 3 : 4;

            var timeStampAsString = string.Format("{0} {1}", tokens[timestampIndex], tokens[timestampIndex + 1]);

            var timeStamp = DateTime.Parse(timeStampAsString);

            var name = tokens[!isFile ? 5 : 6];

            var path = Path.EndsWith("/") ? Path + name : Path + "/" + name;

            var symbolDestination = (isSymbolic ? (size == -1 ? tokens[7] : tokens[8]) : string.Empty); 

            var permissions = new Permissions()
            {
                User = new Permission()
                {
                    CanRead = permissionsLine[1] == 'r',
                    CanWrite = permissionsLine[2] == 'w',
                    CanExecute = permissionsLine[3] == 'x'
                },
                Group = new Permission()
                {
                    CanRead = permissionsLine[4] == 'r',
                    CanWrite = permissionsLine[5] == 'w',
                    CanExecute = permissionsLine[6] == 'x'
                },
                Other = new Permission()
                {
                    CanRead = permissionsLine[7] == 'r',
                    CanWrite = permissionsLine[8] == 'w',
                    CanExecute = permissionsLine[9] == 'x'
                }
            };

            var entry = new Entry()
            {
                Name = name,
                Owner = owner,
                Group = group,
                Size = size,
                Path = path,
                Timestamp = timeStamp,
                IsFile = isFile,
                IsSymbolic = isSymbolic,
                IsDirectory = isDirectory,
                Permissions = permissions,
                SymbolDestinationPath = symbolDestination
            };

            _entries.Add(entry);

        }

        public class Entry
        {

            public string Name
            {
                get;
                internal set;

            }

            public string Path
            {
                get;
                internal set;
            }

            public long Size
            {
                get;
                internal set;
            }

            public string Owner
            {
                get;
                internal set;
            }

            public string Group
            {
                get;
                internal set;
            }

            public DateTime Timestamp
            {
                get;
                internal set;
            }

            public bool IsFile
            {
                get;
                internal set;
            }

            public bool IsDirectory
            {
                get;
                internal set;
            }

            public bool IsSymbolic
            {
                get;
                internal set;
            }

            public string SymbolDestinationPath
            {
                get;
                internal set;
            }

            public string RealPath
            {
                get
                {
                    return IsSymbolic ? SymbolDestinationPath : Path;
                }
            }

            public Permissions Permissions
            {
                get;
                internal set;
            }

        }

        public struct Permissions
        {

            public Permission User
            {
                get;
                internal set;
            }

            public Permission Group
            {
                get;
                internal set;
            }

            public Permission Other
            {
                get;
                internal set;
            }

        }

        public struct Permission
        {
            public bool CanRead
            {
                get;
                internal set;
            }

            public bool CanWrite
            {
                get;
                internal set;
            }

            public bool CanExecute
            {
                get;
                internal set;
            }

        }



    }

}
