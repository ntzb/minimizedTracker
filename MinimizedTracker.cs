using System;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

class MinimizedTracker
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags
    );

    [DllImport("user32.dll")]
    static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    static uint primaryMonitor = MonitorChanger.GetMonitor(true);
    static uint secMonitor = MonitorChanger.GetMonitor(false);

    static IntPtr hhook;

    static string[] windowsToWorkOn = Array.Empty<string>();

    public static bool GetIsWindowVisible(IntPtr hWnd)
    {
        return !IsIconic(hWnd);
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var length = GetWindowTextLength(hWnd) + 1;
        var title = new StringBuilder(length);
        GetWindowText(hWnd, title, length);
        return title.ToString();
    }

    delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime
    );

    // Constants from winuser.h
    // const uint EVENT_SYSTEM_MINIMIZESTART = 16;
    // const uint EVENT_SYSTEM_MINIMIZEEND = 17;
    const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
    const uint EVENT_OBJECT_DESTROY = 0x8001;

    // const uint EVENT_SYSTEM_FOREGROUND = 3;
    const uint WINEVENT_OUTOFCONTEXT = 0;

    // const uint SW_SHOWMAXIMIZED = 3;

    // Need to ensure delegate is not collected while we're using it,
    // storing it in a class field is simplest way to do this.
    static WinEventDelegate procDelegate;

    public static void ListenToWindows(string[] args = null)
    {
        procDelegate = new WinEventDelegate(WinEventProc);
        // Listen for window location changes across all processes/threads on current desktop...
        hhook = SetWinEventHook(
            EVENT_OBJECT_LOCATIONCHANGE,
            EVENT_OBJECT_LOCATIONCHANGE,
            IntPtr.Zero,
            procDelegate,
            0,
            0,
            WINEVENT_OUTOFCONTEXT
        );

        if (args != null)
        {
            windowsToWorkOn = args;
        }

        // MessageBox provides the necessary mesage loop that SetWinEventHook requires.
        // MessageBox.Show("Tracking focus, close message box to exit.");
        // can also use:
        // Application.Run();

        // UnhookWinEvent(hhook);
    }

    static void WinEventProc(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime
    )
    {
        string windowName = GetWindowTitle(hwnd).Trim();
        if (windowName.Equals("") || !windowsToWorkOn.Contains(windowName))
            return;
        // Console.WriteLine("Foreground changed to {0}", windowName);

        bool vsbl = GetIsWindowVisible(hwnd);
        string msg = (vsbl ? "" : "NOT ") + "visible";
        uint monitor = vsbl ? secMonitor : primaryMonitor;
        Console.WriteLine("{0} is {1} (handle {2})", windowName, msg, hwnd);
        MonitorChanger.SetAsPrimaryMonitor(Convert.ToUInt32(monitor));
        Thread.Sleep(3000);
        UnhookWinEvent(hhook);
        ListenToWindows();
    }

    private static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            Console.WriteLine("session unlocked, getting monitor IDs");
            Thread.Sleep(5000);
            primaryMonitor = MonitorChanger.GetMonitor(true);
            secMonitor = MonitorChanger.GetMonitor(false);
        }
    }

    private static void SystemEvents_OnPowerChange(object s, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Console.WriteLine("resumed from suspended power state, getting monitor IDs");
            Thread.Sleep(5000);
            primaryMonitor = MonitorChanger.GetMonitor(true);
            secMonitor = MonitorChanger.GetMonitor(false);
        }
    }

    public static void Init()
    {
        SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        SystemEvents.PowerModeChanged += SystemEvents_OnPowerChange;
    }

    public static void Discard()
    {
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
        SystemEvents.PowerModeChanged -= SystemEvents_OnPowerChange;
    }
}
