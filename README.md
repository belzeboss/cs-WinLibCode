# cs-WinLibCode
This code is used as member-information in my powershell session as follows:
```
$libs = "System.Drawing.Primitives.dll", "System.Drawing.Common.dll", "Microsoft.Win32.Registry.dll", "System.Console.dll"
```
and the code from the repository: 
```
$winLibCode = "$(gc ...\winLibCode.cs -Raw)"
```
then add the type to your session as follows:
```
Add-Type -MemberDefinition $winLibCode -Name "U32" -Namespace WinLib -Refere
