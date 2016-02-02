using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Cli.Build
{
    public class PrepareTargets
    {
        [Target("CheckPrereqs,LocateStage0,RestorePackages")]
        public static BuildTargetResult Prepare(BuildTargetContext c)
        {
            var runtimeInfo = PlatformServices.Default.Runtime;

            if(c.BuildContext["Configuration"] == null)
            {
                c.BuildContext["Configuration"] = "Debug";
            }

            c.Info($"Building {c.BuildContext["Configuration"]} to: {Dirs.Base}");
            c.Info("Build Environment:");
            c.Info($" Operating System: {runtimeInfo.OperatingSystem} {runtimeInfo.OperatingSystemVersion}");
            c.Info($" Platform: {runtimeInfo.OperatingSystemPlatform}");
            return c.Success();
        }

        [Target]
        public static BuildTargetResult LocateStage0(BuildTargetContext c)
        {
            // We should have been run in the repo root, so locate the stage 0 relative to current directory
            var stage0 = DotNetCli.Stage0.BinPath;

            if (!Directory.Exists(stage0))
            {
                return c.Failed($"Stage 0 directory does not exist: {stage0}");
            }

            // Identify the version
            var version = File.ReadAllLines(Path.Combine(stage0, "..", ".version"));
            c.Info($"Using Stage 0 Version: {version[1]}");

            return c.Success();
        }

        [Target]
        public static BuildTargetResult CheckPrereqs(BuildTargetContext c)
        {
            try
            {
                Command.Create("cmake", "--version")
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute();
            }
            catch (Exception ex)
            {
                string message = $@"Error running cmake: {ex.Message}
cmake is required to build the native host 'corehost'";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    message += Environment.NewLine + "Download it from https://www.cmake.org";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    message += Environment.NewLine + "Ubuntu: 'sudo apt-get install cmake'";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    message += Environment.NewLine + "OS X w/Homebrew: 'brew install cmake'";
                }
                return c.Failed(message);
            }

            return c.Success();
        }

        [Target]
        public static BuildTargetResult RestorePackages(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage0;

            dotnet.Restore().WorkingDirectory(Path.Combine(c.BuildContext.BuildDirectory, "src")).Execute().EnsureSuccessful();
            dotnet.Restore().WorkingDirectory(Path.Combine(c.BuildContext.BuildDirectory, "tools")).Execute().EnsureSuccessful();

            return c.Success();
        }
    }
}
