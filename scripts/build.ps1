#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
if (!$env:DOTNET_INSTALL_DIR)
{
    $env:DOTNET_INSTALL_DIR="$PSScriptRoot\..\.dotnet_stage0\Windows"
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR))
{
    mkdir $env:DOTNET_INSTALL_DIR | Out-Null
}

# Install a stage 0
Write-Host "Installing .NET Core CLI Stage 0"
& "$PSScriptRoot\obtain\install.ps1" -Channel $env:Channel

# Put the stage0 on the path
$env:PATH = "$env:DOTNET_INSTALL_DIR\cli\bin;$env:PATH"

# Restore the build scripts
Write-Host "Compiling Build Scripts..."
pushd $PSScriptRoot
$result = dotnet restore
if($LASTEXITCODE -ne 0) { $result | ForEach-Object { Write-Host $_ }; throw "Failed to restore" }
popd

# Build the builder
$result = dotnet build "$PSScriptRoot\dotnet-cli-build"
if($LASTEXITCODE -ne 0) { $result | ForEach-Object { Write-Host $_ }; throw "Failed to compile build scripts" }

# Run the builder
$env:DOTNET_HOME="$env:DOTNET_INSTALL_DIR\cli"
& "$PSScriptRoot\dotnet-cli-build\bin\Debug\dnxcore50\dotnet-cli-build.exe" @args
