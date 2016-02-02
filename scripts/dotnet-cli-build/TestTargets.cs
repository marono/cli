using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;
using static Microsoft.DotNet.Cli.Build.FS;

namespace Microsoft.DotNet.Cli.Build
{
    public class TestTargets
    {
        public static readonly string[] TestPackageProjects = new[]
        {
            "dotnet-hello/v1/dotnet-hello",
            "dotnet-hello/v2/dotnet-hello"
        };

        public static readonly string[] TestProjects = new[]
        {
            "E2E",
            "StreamForwarderTests",
            "dotnet-publish.Tests",
            "dotnet-compile.Tests",
            "dotnet-build.Tests",
            "Compiler.Common.Tests"
        };

        [Target("SetupTests,RestoreTests,BuildTests,RunTests")]
        public static BuildTargetResult Test(BuildTargetContext c) => c.Success();

        [Target("RestoreTestPrerequisites,BuildTestPrerequisites")]
        public static BuildTargetResult SetupTests(BuildTargetContext c) => c.Success();

        [Target]
        public static BuildTargetResult RestoreTestPrerequisites(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage2;
            dotnet.Restore().WorkingDirectory(Path.Combine(c.BuildContext.BuildDirectory, "test", "TestPackages")).Execute().EnsureSuccessful();

            // The 'testapp' directory contains intentionally-unresolved dependencies, so don't check for success
            dotnet.Restore().WorkingDirectory(Path.Combine(c.BuildContext.BuildDirectory, "testapp")).Execute();

            return c.Success();
        }

        [Target]
        public static BuildTargetResult BuildTestPrerequisites(BuildTargetContext c)
        {
            var dotnet = DotNetCli.Stage2;

            Rmdir(Dirs.TestPackages);
            Mkdirp(Dirs.TestPackages);

            foreach (var relativePath in TestPackageProjects)
            {
                var fullPath = Path.Combine(c.BuildContext.BuildDirectory, "test", "TestPackages", relativePath.Replace('/', Path.DirectorySeparatorChar));
                dotnet.Pack("--output", Dirs.TestPackages)
                    .WorkingDirectory(fullPath)
                    .Execute()
                    .EnsureSuccessful();
            }

            return c.Success();
        }

        [Target]
        public static BuildTargetResult RestoreTests(BuildTargetContext c)
        {
            var configuration = (string)c.BuildContext["Configuration"];
            DotNetCli.Stage2.Restore("--fallbacksource", Path.Combine(Dirs.TestPackages, configuration))
                .WorkingDirectory(Path.Combine(c.BuildContext.BuildDirectory, "test"))
                .Execute()
                .EnsureSuccessful();
            return c.Success();
        }

        [Target]
        public static BuildTargetResult BuildTests(BuildTargetContext c)
        {
            var configuration = (string)c.BuildContext["Configuration"];
            var dotnet = DotNetCli.Stage2;
            foreach (var testProject in TestProjects)
            {
                dotnet.Publish("--output", Dirs.TestBase, "--configuration", configuration)
                    .WorkingDirectory(Path.Combine(c.BuildContext.BuildDirectory, "test", testProject))
                    .Execute()
                    .EnsureSuccessful();
            }
            return c.Success();
        }

        [Target("RunXUnitTests,RunPackageCommandTests,RunArgumentForwardingTests")]
        public static BuildTargetResult RunTests(BuildTargetContext c) => c.Success();

        [Target]
        public static BuildTargetResult RunXUnitTests(BuildTargetContext c)
        {
            // Need to load up the VS Vars
            var vsvars = LoadVsVars();

            // Copy the test projects
            var testProjectsDir = Path.Combine(Dirs.Base, "TestProjects");
            Rmdir(testProjectsDir);
            Mkdirp(testProjectsDir);
            CopyRecursive(Path.Combine(c.BuildContext.BuildDirectory, "test", "TestProjects"), testProjectsDir);

            // Run the tests and set the VS vars in the environment when running them
            var corerun = Path.Combine(Dirs.Base, $"corehost{Constants.ExeSuffix}");
            var failingTests = new List<string>();
            foreach (var project in TestProjects)
            {
                var result = Cmd(corerun, "xunit.console.netcore.exe", $"{project}.dll", "-xml", $"{project}-testResults.xml", "-notrait", "category=failing")
                    .WorkingDirectory(Dirs.Base)
                    .Environment(vsvars)
                    .Execute();
                if (result.ExitCode != 0)
                {
                    failingTests.Add(project);
                }
            }

            if (failingTests.Any())
            {
                foreach (var project in failingTests)
                {
                    c.Error($"{project} failed");
                }
                return c.Failed("Tests failed!");
            }

            return c.Success();
        }

        [Target]
        public static BuildTargetResult RunPackageCommandTests(BuildTargetContext c)
        {
            return c.Failed("Not yet implemented");
        }

        [Target]
        public static BuildTargetResult RunArgumentForwardingTests(BuildTargetContext c)
        {
            return c.Failed("Not yet implemented");
        }

        private static Dictionary<string, string> LoadVsVars()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Dictionary<string, string>();
            }

            var vsvarsPath = Path.GetFullPath(Path.Combine(Environment.GetEnvironmentVariable("VS140COMNTOOLS"), "..", "..", "VC"));

            var result = Cmd(Environment.GetEnvironmentVariable("COMSPEC"), "/c", "vcvarsall.bat", "x64", "&set")
                .WorkingDirectory(vsvarsPath)
                .CaptureStdOut()
                .Execute();
            result.EnsureSuccessful();
            var vars = new Dictionary<string, string>();
            foreach (var line in result.StdOut.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var splat = line.Split(new[] { '=' }, 2);
                vars[splat[0]] = splat[1];
            }
            return vars;
        }
    }
}
