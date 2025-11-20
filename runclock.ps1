# Stop any running DesktopClock process
$clockProc = Get-Process DesktopClock -ErrorAction SilentlyContinue
if( $clockProc ){
	"🕛🔴👍🏻"
    $clockProc | Stop-Process
}
 

# Publish to ~/tools
$toolsDir = Join-Path $env:USERPROFILE "tools"
New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
try {
    dotnet publish .\DesktopClock\DesktopClock.csproj -o $toolsDir -c Release -r win-x64 | Out-Null
} catch {
    "🏗️👎🏻"
    exit 1
}

"🕛🏗️👍🏻"

# Run the executable
& "$toolsDir\DesktopClock.exe"

"🕛🟢👍🏻"
