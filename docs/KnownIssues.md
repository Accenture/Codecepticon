# Codecepticon Known Issues

## C# Obfuscation

### Solutions with Multiple Projects

Codecepticon does not work when a solution has more than 1 project included. An example of this is the [SharpDPAPI](https://github.com/GhostPack/SharpDPAPI) project which also includes `SharpChrome`. In order to obfuscate it, you will have to remove one or the other. Codecepticon will catch this and will prompt you for action (continue/exit).

### Roslyn Version

Currently C# obfuscation only works with Roslyn v3.9.0, as there is a but which affects renaming namespaces/functions: https://github.com/dotnet/roslyn/issues/58463

When this issue is resolved, Codecepticon and its documentation will be updated. Codecepticon will catch this and will prompt you for action (continue/exit).

## PowerShell Obfuscation

Due to the nature and complexity of PowerShell scripts, obfuscation of large/complex scripts may or may not work.

## VBA/VB6

### Documents

Codecepticon cannot obfuscate an office document such as Word/Excel. You will have to obfuscate each `*.bas` file and then manually insert into your document.