using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build
{
    public static class Dirs
    {
        public static readonly string Base = Path.Combine(
            Directory.GetCurrentDirectory(),
            "artifacts",
            PlatformServices.Default.Runtime.GetRuntimeIdentifier());
        public static readonly string Stage1 = Path.Combine(Base, "stage1");
        public static readonly string Stage1Compilation = Path.Combine(Base, "stage1compilation");
        public static readonly string Stage2 = Path.Combine(Base, "stage2");
        public static readonly string Stage2Compilation = Path.Combine(Base, "stage2compilation");
        public static readonly string Corehost = Path.Combine(Base, "corehost");
        public static readonly string TestBase = Path.Combine(Base, "tests");
        public static readonly string TestPackages = Path.Combine(TestBase, "packages");

        public static readonly string NuGetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? GetNuGetPackagesDir();

        private static string GetNuGetPackagesDir()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".nuget", "packages");
            }
            return Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".nuget", "packages");
        }
    }
}
