using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Threading.Tasks;
using Mimiq.Core;

namespace Mimiq;

public class App : Application
{
    private GEarthExtension? extension;
    private MainWindow? window;
    private IClassicDesktopStyleApplicationLifetime? desktop;
    private bool isRunning;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktop = desktopLifetime;
            Task.Run(StartExtension);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private async void StartExtension()
    {
        try
        {
            extension = new GEarthExtension();

            extension.Activated += () =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (window == null)
                    {
                        window = new MainWindow(extension);
                        window.Closing += OnWindowClosing;
                        if (desktop != null)
                            desktop.MainWindow = window;
                    }
                    window.Show();
                    window.Activate();
                });
            };

            isRunning = true;
            extension.Run();
        }
        catch { }
        finally
        {
            isRunning = false;
            await Task.Delay(2000);
            desktop?.Shutdown();
        }
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (e.CloseReason is not WindowCloseReason.WindowClosing)
            return;

        if (!isRunning)
        {
            desktop?.Shutdown();
        }
        else
        {
            e.Cancel = true;
            if (sender is Window w)
                w.Hide();
        }
    }
}
