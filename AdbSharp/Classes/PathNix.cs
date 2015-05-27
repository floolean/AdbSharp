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

        public static string GetFilename(string path)
        {
            var last = path.Split(new char[] { '/' }, StringSplitOptions.None).Last();

            return last.Replace("\"", string.Empty);

        }

        public static string GetDirectoryPath(string path)
        {

            if (path.StartsWith("\""))
                path = path.Substring(1);

            if (path.EndsWith("\""))
                path = path.Substring(0, path.Length - 1);

            var tokens = path.Split(new char[] { '/' }, StringSplitOptions.None);

            return string.Join( "/", tokens.Take(tokens.Length - 1));

        }


    }
}
