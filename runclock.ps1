# Stop any running DesktopClock process
$clockProc = Get-Process DesktopClock -ErrorAction SilentlyContinue
if( $clockProc ){
	"🕛🔴👍🏻"
    $clockProc | Stop-Process
}
 

# Publish to ~/tools
$toolsDir = Join-Path $env:USERPROFILE "tools"
New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null


# Run publish and capture output only if it fails
$null = msbuild publish .\DesktopClock\DesktopClock.csproj -o $toolsDir -c Release -r win-x64
if ($LASTEXITCODE -ne 0) {
    "🏗️👎🏻"
    # Rerun to show error output with color
    msbuild publish .\DesktopClock\DesktopClock.csproj -o $toolsDir -c Release -r win-x64
    exit 1
}

"🕛🏗️👍🏻"

# Run the executable
& "$toolsDir\DesktopClock.exe"

"🕛🟢👍🏻"
