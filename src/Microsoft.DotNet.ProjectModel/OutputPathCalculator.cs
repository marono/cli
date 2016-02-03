// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NuGet.Frameworks;

namespace Microsoft.DotNet.ProjectModel
{
    public class OutputPathCalculator
    {
        private const string ObjDirectoryName = "obj";
        private const string BinDirectoryName = "bin";

        private readonly Project _project;
        private readonly NuGetFramework _framework;

        private readonly string _runtimeIdentifier;

        private string BaseBuildPath { get; }
        private string OutputPath { get; }

        public OutputPathCalculator(
            Project project,
            NuGetFramework framework,
            string runtimeIdentifier,
            string baseBuildPath,
            string outputPath)
        {
            _project = project;
            _framework = framework;
            _runtimeIdentifier = runtimeIdentifier;

            BaseBuildPath = string.IsNullOrEmpty(baseBuildPath)
                ? _project.ProjectDirectory
                : Path.Combine(baseBuildPath, _project.Name);

            OutputPath = outputPath;
        }

        public string GetCompilationOutputPath(string buildConfiguration)
        {
            var outDir = Path.GetFullPath(Path.Combine(BaseBuildPath,
                BinDirectoryName,
                buildConfiguration,
                _framework.GetShortFolderName()));

            return outDir;
        }

        public string GetFinalOutputPath(string buildConfiguration)
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                return Path.Combine(GetCompilationOutputPath(buildConfiguration), _runtimeIdentifier);
            }
            return OutputPath;
        }

        public string GetIntermediateOutputDirectoryPath(string buildConfiguration)
        {
            return Path.Combine(
                BaseBuildPath,
                ObjDirectoryName,
                buildConfiguration,
                _framework.GetTwoDigitShortFolderName());
        }

        public string GetAssemblyPath(string buildConfiguration, bool runtime = false)
        {
            var compilationOptions = _project.GetCompilerOptions(_framework, buildConfiguration);
            var outputExtension = FileNameSuffixes.DotNet.DynamicLib;

            if (_framework.IsDesktop() && compilationOptions.EmitEntryPoint.GetValueOrDefault())
            {
                outputExtension = FileNameSuffixes.DotNet.Exe;
            }

            return Path.Combine(
                runtime ? GetFinalOutputPath(buildConfiguration) : GetCompilationOutputPath(buildConfiguration),
                _project.Name + outputExtension);
        }

        public IEnumerable<string> GetBuildOutputs(string buildConfiguration, bool runtime = false)
        {
            var assemblyPath = GetAssemblyPath(buildConfiguration, runtime);

            yield return assemblyPath;
            yield return Path.ChangeExtension(assemblyPath, "pdb");

            var compilationOptions = _project.GetCompilerOptions(_framework, buildConfiguration);

            if (compilationOptions.GenerateXmlDocumentation == true)
            {
                yield return Path.ChangeExtension(assemblyPath, "xml");
            }

            if (runtime)
            {
                // This should only exist in desktop framework
                var configFile = assemblyPath + ".config";

                if (File.Exists(configFile))
                {
                    yield return configFile;
                }

                // Deps file
                var depsFile = GetDepsPath(buildConfiguration);

                if (File.Exists(depsFile))
                {
                    yield return depsFile;
                }
            }
        }

        public string GetDepsPath(string buildConfiguration)
        {
            return Path.Combine(
                GetFinalOutputPath(buildConfiguration),
                _project.Name + FileNameSuffixes.Deps);
        }

        public string GetExecutablePath(string buildConfiguration)
        {
            var extension = FileNameSuffixes.CurrentPlatform.Exe;

            // This is the check for mono, if we're not on windows and producing outputs for
            // the desktop framework then it's an exe
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _framework.IsDesktop())
            {
                extension = FileNameSuffixes.DotNet.Exe;
            }

            return Path.Combine(
                GetFinalOutputPath(buildConfiguration),
                _project.Name + extension);
        }

        public string GetPdbPath(string buildConfiguration)
        {
            return Path.Combine(
                GetCompilationOutputPath(buildConfiguration),
                _project.Name + FileNameSuffixes.DotNet.ProgramDatabase);
        }
    }
}
