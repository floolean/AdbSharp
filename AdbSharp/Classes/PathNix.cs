using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdbSharp
{
    public static class PathNix
    {

        const string NIX_SLASH = "/";

        public static string Combine(string path1, string path2)
        {

            if (path1.EndsWith(NIX_SLASH))
            {

                if (path2.StartsWith(NIX_SLASH))
                {
                    return path1 + path2.Substring(1);
                }
                else
                {
                    return path1 + path2;
                }
            }
            else
            {

                if (path2.StartsWith(NIX_SLASH))
                {
                    return path1 + path2;
                }
                else
                {
                    return path1 + NIX_SLASH + path2;
                }

            }

        }


    }
}
