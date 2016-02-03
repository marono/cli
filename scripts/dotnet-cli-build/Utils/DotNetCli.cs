using Microsoft.DotNet.Cli.Build.Framework;
using System.IO;
using System.Linq;
using System;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.DotNet.Cli.Build
{
    internal class DotNetCli
    {
        public static readonly DotNetCli Stage0 = new DotNetCli(Path.Combine(Directory.GetCurrentDirectory(), ".dotnet_stage0", PlatformServices.Default.Runtime.OperatingSystemPlatform.ToString(), "cli", "bin"));
        public static readonly DotNetCli Stage1 = new DotNetCli(Path.Combine(Dirs.Stage1, "bin"));
        public static readonly DotNetCli Stage2 = new DotNetCli(Path.Combine(Dirs.Stage2, "bin"));

        public string BinPath { get; }

        public DotNetCli(string binPath)
        {
            BinPath = binPath;
        }

        public void SetDotNetHome()
        {
            Environment.SetEnvironmentVariable("DOTNET_HOME", Path.GetDirectoryName(BinPath));
        }

        public Command Exec(string command, params string[] args)
        {
            return Command.Create(Path.Combine(BinPath, "dotnet.exe"), Enumerable.Concat(new[] { command }, args));
        }

        public Command Restore(params string[] args) => Exec("restore", args);
        public Command Build(params string[] args) => Exec("build", args);
        public Command Pack(params string[] args) => Exec("pack", args);
        public Command Test(params string[] args) => Exec("test", args);
        public Command Publish(params string[] args) => Exec("publish", args);
    }
}