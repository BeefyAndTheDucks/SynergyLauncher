﻿using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Files;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installers;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;

namespace SynergyLauncherCore;

public static class MinecraftLaunchHandler
{
    public static MinecraftPath GetMinecraftPath(ModLoader modLoader)
    {
        return new MinecraftPath(Path.Combine(Directory.GetCurrentDirectory(), "MinecraftInstallation" + modLoader));
    }
    
    public static async Task<ProcessWrapper> Start(MSession session, ModLoader modLoader,
        IProgress<double> progressBar, ILogHandler logHandler,
        SyncProgress<InstallerProgressChangedEventArgs> fileProgress, CancellationToken cancellationToken = default)
    {
        const string serverIp = "synergyserver.net";
        
        logHandler.Log("Launcher", "[INFO] Starting Minecraft...");
        
        System.Net.ServicePointManager.DefaultConnectionLimit = 256;
        
        Stopwatch sw = new Stopwatch();
        
        InstallerProgressChangedSync installerProgressChangedSync = new InstallerProgressChangedSync(logHandler, fileProgress);
        ByteProgressChangedSync byteProgressChangedSync = new ByteProgressChangedSync(progressBar);
        
        // get minecraft version
        logHandler.Log("Launcher", "[INFO] Getting Minecraft Version of Synergy");
        MinecraftServerInfo serverInfo = await MinecraftServerInfo.FromServerAddress(serverIp);
        string minecraftVersion = serverInfo.Version.NameClean;
        logHandler.Log("Launcher", $"[INFO] Minecraft Version: {minecraftVersion}");

        // initialize launcher
        MinecraftPath path = GetMinecraftPath(modLoader);
        MinecraftLauncherParameters parameters = MinecraftLauncherParameters.CreateDefault();
        parameters.MinecraftPath = path;
        MinecraftLauncher launcher = new MinecraftLauncher(parameters);
        
        // install
        logHandler.Log("Launcher", "[INFO] Installing/Verifying Minecraft Installation...");
        sw.Start();
        
        string versionName = await InstallModLoader(logHandler, modLoader, minecraftVersion, path, launcher, installerProgressChangedSync, byteProgressChangedSync, cancellationToken);
        await launcher.GetAllVersionsAsync(cancellationToken);
        MinecraftVersion logOverridenVersion = await InjectCustomLogSettings(launcher, versionName, cancellationToken);
        await launcher.InstallAsync(logOverridenVersion, installerProgressChangedSync, byteProgressChangedSync, cancellationToken);
        
        sw.Stop();
        
        logHandler.Log("Launcher", "[INFO] Installation Complete! (Took " + sw.Elapsed.TotalSeconds + "s)");

        // build process
        logHandler.Log("Launcher", "[INFO] Creating Minecraft Process...");
        Process process = launcher.BuildProcess(logOverridenVersion, new MLaunchOption
        {
            Session = session,
            ServerIp = serverIp
        });

        ProcessWrapper processWrapper = new ProcessWrapper(process);
        processWrapper.OutputReceived += (s, e) => logHandler.Log("Minecraft", e);  
        processWrapper.StartWithEvents();
        logHandler.Log("Launcher", "[INFO] Started Minecraft!");
        return processWrapper;
    }

    private static async Task<MinecraftVersion> InjectCustomLogSettings(MinecraftLauncher launcher, string minecraftVersion, CancellationToken cancellationToken)
    {
        IVersion version = await launcher.GetVersionAsync(minecraftVersion, cancellationToken);
        MinecraftVersion mutableVersion = version.ToMutableVersion();
        mutableVersion.Logging = new MLogFileMetadata
        {
            Argument = "-Dlog4j.configurationFile=${path}",
            LogFile = new MFileMetadata
            {
                Id = "client-custom.xml",
                Sha1 = "08752f97e69973708650b54f889fadc0f9a9e271", // https://emn178.github.io/online-tools/sha1_checksum.html
                Url = "https://www.dropbox.com/scl/fi/s501uax5m8u8h14a199mk/client-custom.xml?rlkey=23ltrkiox0riohhfrehgbb6gn&st=cw52qspr&dl=1"
            },
            Type = "log4j2-xml"
        };

        return mutableVersion;
    }

    private static async Task<string> InstallModLoader(ILogHandler logHandler, ModLoader modLoader, string minecraftVersion, MinecraftPath path, MinecraftLauncher launcher, InstallerProgressChangedSync installerProgress, ByteProgressChangedSync byteProgress, CancellationToken cancellationToken = default)
    {
        switch (modLoader)
        {
            case ModLoader.Fabric:
                logHandler.Log("Launcher", "[INFO] Installing Fabric Mod Loader...");

                FabricInstaller fabricInstaller = new FabricInstaller(new HttpClient());
                IReadOnlyCollection<string> versions = await fabricInstaller.GetSupportedVersionNames();
                
                if (!versions.Contains(minecraftVersion))
                    logHandler.Log("Fabric", $"[ERROR] Fabric is not supported for version {minecraftVersion}! Using vanilla instead...");

                string versionName = await fabricInstaller.Install(minecraftVersion, path);
                
                logHandler.Log("Launcher", $"[INFO] Fabric Mod Loader Installed! (version {versionName})");

                return versionName;
            case ModLoader.Forge:
                logHandler.Log("Launcher", "[INFO] Installing Forge Mod Loader...");
                
                ForgeInstaller forgeInstaller = new ForgeInstaller(launcher);
                
                string forgeVersion = await forgeInstaller.Install(minecraftVersion, new ForgeInstallOptions
                {
                    FileProgress = installerProgress,
                    ByteProgress = byteProgress,
                    InstallerOutput = new SyncProgress<string>(e => logHandler.LogDebug("Forge", e))
                });

                return forgeVersion;
            case ModLoader.Vanilla:
                logHandler.Log("Launcher", "[INFO] Using Vanilla Minecraft...");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(modLoader), modLoader, null);
        }

        return minecraftVersion;
    }

    public enum ModLoader
    {
        Vanilla,
        Fabric,
        Forge
    }

    private class InstallerProgressChangedSync(ILogHandler logHandler, IProgress<InstallerProgressChangedEventArgs> uiFileProgress) : IProgress<InstallerProgressChangedEventArgs>
    {
        public void Report(InstallerProgressChangedEventArgs e)
        {
            switch (e.EventType)
            {
                case InstallerEventType.Queued:
                    logHandler.LogDebug("Launcher", $"Enqueued {e.Name} for processing. (Task number {e.TotalTasks})");
                    break;
                case InstallerEventType.Done:
                    logHandler.LogDebug("Launcher", $"Finished processing {e.Name} ({e.ProgressedTasks} / {e.TotalTasks})");
                    break;
                default:
                    logHandler.Log("Launcher", $"[WARN] Unknown event type: {e.EventType}");
                    break;
            }
                
            uiFileProgress.Report(e);
        }
    }
    
    private class ByteProgressChangedSync(IProgress<double> progressBar) : IProgress<ByteProgress>
    {
        public void Report(ByteProgress e)
        {
            progressBar.Report(e.ToRatio());
        }
    }
}
