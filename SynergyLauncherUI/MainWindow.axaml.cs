using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CmlLib.Core;
using CmlLib.Core.Installers;
using CmlLib.Core.ProcessBuilder;
using SynergyLauncherCore;

namespace SynergyLauncherUI;

public partial class MainWindow : Window
{
    public ObservableCollection<LogData> Logs { get; } = [];
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private Action? _stopAction;

    private bool GameRunning { get; set; }

    private MinecraftLaunchHandler.ModLoader SelectedModLoader => ModLoaderDropdown.SelectedIndex switch
    {
        0 => MinecraftLaunchHandler.ModLoader.Vanilla,
        1 => MinecraftLaunchHandler.ModLoader.Fabric,
        2 => MinecraftLaunchHandler.ModLoader.Forge,
        _ => throw new ArgumentOutOfRangeException()
    };

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        Storage.Logger = new LogHandler(this);
        
        Logs.CollectionChanged += (_, _) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                LogItemsControl.UpdateLayout();
                LogScrollView.ScrollToEnd();
            }, DispatcherPriority.Background);
        };
        
        Closing += OnClosing;
        
        Storage.Logger.Log("Launcher", "[INFO] Welcome to Synergy Launcher!");
        Storage.Logger.Log("Launcher", "[INFO] Press the play button to start minecraft!");
        
        SignIn();
    }

    private async void SignIn()
    {
        try
        {
            Storage.Logger!.Log("Launcher", "[INFO] Signing in...");
            Storage.Session = await SignInHandler.SignIn();
            Storage.Logger.Log("Launcher", "[INFO] Signed in!");
            await Dispatcher.UIThread.InvokeAsync(() => PlayButton.IsEnabled = true);
        }
        catch (Exception e)
        {
            Storage.Logger!.Log("Launcher", $"[ERROR] {e.Message}");
            await Dispatcher.UIThread.InvokeAsync(() => PlayButton.IsEnabled = false);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        Stop();
    }

    // ReSharper disable once AsyncVoidMethod
    private async void PlayButton_OnClick(object? sender, RoutedEventArgs e)
    {
        GameRunning = !GameRunning;

        if (GameRunning)
        {
            PlayButton.Content = "Stop";
            ProgressBar.IsVisible = true;
            ProgressText.IsVisible = true;
            ProcessWrapper process;
            try
            {
                process = await MinecraftLaunchHandler.Start(Storage.Session!,
                    SelectedModLoader,
                    new ProgressHandler(ProgressBar),
                    Storage.Logger!,
                    new SyncProgress<InstallerProgressChangedEventArgs>(fileProgress =>
                        Dispatcher.UIThread.InvokeAsync(() =>
                            ProgressText.Text =
                                $"[{fileProgress.ProgressedTasks}/{fileProgress.TotalTasks}] Downloaded {fileProgress.Name}")),
                    _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Storage.Logger!.Log("Launcher", $"[ERROR] {ex.Message}. Please try again.");
                Reset();
                return;
            }

            _stopAction = () => process.Process.Kill();
            process.Exited += (_, _) => Reset();
            ProgressBar.IsVisible = false;
            ProgressText.IsVisible = false;
        }
        else
        {
            Stop();
            Reset();
        }
    }

    private void Stop()
    {
        _cancellationTokenSource.CancelAsync();
        _stopAction?.Invoke();
    }

    private void Reset()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _cancellationTokenSource = new CancellationTokenSource();
            GameRunning = false;
            PlayButton.Content = "Play";
            ProgressBar.Value = 0;
            ProgressBar.IsVisible = false;
            ProgressText.IsVisible = false;
        });
    }

    private class ProgressHandler(ProgressBar progressBarControl) : IProgress<double>
    {
        public void Report(double value)
        {
            Dispatcher.UIThread.InvokeAsync(() => progressBarControl.Value = value);
        }
    }

    private void OpenFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        MinecraftPath path = MinecraftLaunchHandler.GetMinecraftPath(SelectedModLoader);
        
        if (!Directory.Exists(path.BasePath))
            Directory.CreateDirectory(path.BasePath);

        if (SelectedModLoader != MinecraftLaunchHandler.ModLoader.Vanilla)
            Directory.CreateDirectory(Path.Combine(path.BasePath, "mods"));
        
        System.Diagnostics.Process.Start("explorer.exe", path.BasePath);
    }
}