@echo off
cd Build/

copy "Linux64\SynergyLauncherUI" "_Compiled\Linux64_SynergyLauncher"
copy "LinuxARM\SynergyLauncherUI" "_Compiled\LinuxARM_SynergyLauncher"
copy "LinuxARM64\SynergyLauncherUI" "_Compiled\LinuxARM64_SynergyLauncher"
copy "Mac64\SynergyLauncherUI" "_Compiled\Mac64_SynergyLauncher"
copy "MacARM\SynergyLauncherUI" "_Compiled\MacARM_SynergyLauncher"
copy "Win32\SynergyLauncherUI.exe" "_Compiled\Win32_SynergyLauncher.exe"
copy "Win64\SynergyLauncherUI.exe" "_Compiled\Win64_SynergyLauncher.exe"
copy "WinARM64\SynergyLauncherUI.exe" "_Compiled\WinARM64_SynergyLauncher.exe"

timeout /t 5