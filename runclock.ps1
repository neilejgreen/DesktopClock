# Stop any running DesktopClock process
$clockProc = Get-Process DesktopClock -ErrorAction SilentlyContinue
if( $clockProc ){
	"🕛🔴👍🏻"
    $clockProc | Stop-Process
}
 
# Publish to ~/tools
$toolsDir = Join-Path $env:USERPROFILE "tools"
New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null

# The build command
$publishCmd = "dotnet publish .\DesktopClock\DesktopClock.csproj -c Release -r win-x64 -o `"$toolsDir`""

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
