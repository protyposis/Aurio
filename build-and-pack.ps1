Write-Host "Setting up VS dev env...";
$vsPath = &(Join-Path ${env:ProgramFiles(x86)} "\Microsoft Visual Studio\Installer\vswhere.exe") -latest -property installationpath
Import-Module (Join-Path $vsPath "Common7\Tools\Microsoft.VisualStudio.DevShell.dll")
Enter-VsDevShell -VsInstallPath $vsPath -SkipAutomaticLocation -DevCmdArguments '-arch=x64'

Write-Host "Building native source for Windows...";
./install-deps.ps1
cmake nativesrc --preset x64-debug
cmake --build nativesrc\out\build\x64-debug
cmake nativesrc --preset x64-release
cmake --build nativesrc\out\build\x64-release

Write-Host "Building native source for Linux...";
docker build -f Dockerfile.libaurioffmpeglinuxbuild --tag libaurioffmpegproxybuilder .
docker run -it --rm -v .:/aurio libaurioffmpegproxybuilder

Write-Host "Packing...";
dotnet pack src -c NugetPackRelease -o dist/nuget

Write-Host "Done :)";
