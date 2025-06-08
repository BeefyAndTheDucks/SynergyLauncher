# SynergyLauncher
A Minecraft launcher that can automatically detect Minecraft version of a server and connect to it.

## Features
- [x] Automatically detects the version of Minecraft that the server is using
- [x] Automatically connects to the server.
- [x] Supports Mods (Fabric only for now) and Vanilla Minecraft.
- [ ] Modrinth mod downloading
- [ ] Support for other servers
- [ ] Support for other mod-loaders

### Framework
It is developed using .NET 8.0 and uses CmlCore for launching Minecraft.

# Building

Clone or download the Source Code using either the Code dropdown - Download ZIP, Using GitHub Desktop (or other Git GUI), or using the command line: 
```
git clone https://github.com/BeefyAndTheDucks/SynergyLauncher.git
```

Then open the project using JetBrains Rider or VS 2022 and let it import.

You should now be ready! Start it by launching the `SynergyLauncherUI` configuration.
On JetBrains Rider you can build an EXE by launching the `Build` configuration and checking the `Build` folder in the root of the solution, I'm unsure how to do this in VS2022.

Happy coding!
