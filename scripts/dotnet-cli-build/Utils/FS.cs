using Microsoft.DotNet.Cli.Build.Framework;
using System.IO;
using System.Runtime.InteropServices;
using System;

namespace Microsoft.DotNet.Cli.Build
{
    public static class FS
    {
        public static void Mkdirp(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        public static void Rmdir(string dir)
        {
            if(Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        public static void Chmod(string file, string mode)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Command.Create("chmod", mode, file).Execute().EnsureSuccessful();
            }
        }

        public static void CopyRecursive(string sourceDirectory, string destinationDirectory, bool overwrite = false)
        {
            Mkdirp(destinationDirectory);

            foreach(var dir in Directory.EnumerateDirectories(sourceDirectory))
            {
                CopyRecursive(dir, Path.Combine(destinationDirectory, Path.GetFileName(dir)), overwrite);
            }

            foreach(var file in Directory.EnumerateFiles(sourceDirectory))
            {
                var dest = Path.Combine(destinationDirectory, Path.GetFileName(file));
                if (!File.Exists(file) || overwrite)
                {
                    // We say overwrite true, because we only get here if the file didn't exist (thus it doesn't matter) or we
                    // wanted to overwrite :)
                    File.Copy(file, dest, overwrite: true);
                }
            }
        }
    }
}
