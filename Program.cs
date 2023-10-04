using System;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Console.WriteLine($"tracking windows: {string.Join(",", args)}");
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.Run(new TrayAppContext(args));
    }
}

public class TrayAppContext : ApplicationContext
{
    private NotifyIcon trayIcon;

    public TrayAppContext(string[] args)
    {
        MonitorChanger.SetDpiAwareness();
        trayIcon = new NotifyIcon()
        {
            Icon = Icon.ExtractAssociatedIcon("Resources/icon.ico"),
            ContextMenuStrip = new ContextMenuStrip()
            {
                Items = { new ToolStripMenuItem("Exit", null, Exit) }
            },
            Visible = true
        };

        MinimizedTracker.ListenToWindows(args);
        MinimizedTracker.Init();
    }

    void Exit(object? sender, EventArgs e)
    {
        trayIcon.Visible = false;
        MinimizedTracker.Discard();
        Application.Exit();
    }
}
