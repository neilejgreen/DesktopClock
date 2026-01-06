# Stop any running DesktopClock process
$clockProc = Get-Process DesktopClock -ErrorAction SilentlyContinue
if( $clockProc ){
	"🕛🔴👍🏻"
    $clockProc | Stop-Process
}
 
# Publish to ~/tools
$toolsDir = Join-Path $env:USERPROFILE "tools"
New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null

# Restore first with the RID, then build and publish
$publishCmd = "msbuild .\DesktopClock\DesktopClock.csproj /t:Restore /p:RuntimeIdentifier=win-x64 && msbuild .\DesktopClock\DesktopClock.csproj /t:publish /p:PublishDir=`"$toolsDir`" /p:Configuration=Release /p:RuntimeIdentifier=win-x64"

# Run publish and capture output only if it fails
$null = iex $publishCmd

if ($LASTEXITCODE -ne 0) {
    "🏗️👎🏻"
    # Rerun to show error output with color
    iex $publishCmd
    exit 1
}

"🕛🏗️👍🏻"

# Run the executable
& "$toolsDir\DesktopClock.exe"

"🕛🟢👍🏻"
