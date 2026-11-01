// Claude Tray: tray icon, Ctrl+Alt+C for the Claude Code window and Ctrl+Alt+V for voice typing.
// Built by build.ps1 with the C# 5 compiler that ships with .NET Framework.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using FormsTimer = System.Windows.Forms.Timer;

static class Native
{
    public const int WM_HOTKEY = 0x0312;
    public const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_NOREPEAT = 0x4000;
    public const int SW_HIDE = 0, SW_SHOW = 5, SW_RESTORE = 9, SW_SHOWNOACTIVATE = 4;
    public const uint SWP_NOSIZE = 1, SWP_NOZORDER = 4, SWP_NOACTIVATE = 0x10;

    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct SIZE { public int W, H; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BLENDFUNCTION { public byte Op, Flags, Alpha, Format; }
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public int Size, Width, Height;
        public short Planes, BitCount;
        public int Compression, SizeImage, XPelsPerMeter, YPelsPerMeter, ClrUsed, ClrImportant;
    }

    public delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);

    public const int WM_INPUTLANGCHANGEREQUEST = 0x50;
    public const int DWMWA_CLOAKED = 14;
    public const uint MOUSEEVENTF_LEFTDOWN = 2, MOUSEEVENTF_LEFTUP = 4;
    public const uint GA_ROOT = 2;

    public const int WM_MOUSEMOVE = 0x200, WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202;
    public const int GWL_EXSTYLE = -20, WS_EX_LAYERED = 0x80000;
    public const uint LWA_ALPHA = 2, SWP_NOMOVE = 2;

    [DllImport("user32")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32")] public static extern int GetWindowLong(IntPtr h, int index);
    [DllImport("user32")] public static extern int SetWindowLong(IntPtr h, int index, int value);
    [DllImport("user32")] public static extern bool SetLayeredWindowAttributes(IntPtr h, uint key, byte alpha, uint flags);
    [DllImport("user32")] public static extern int GetDpiForWindow(IntPtr h);
    [DllImport("user32")] public static extern void mouse_event(uint flags, int x, int y, int data, UIntPtr extra);
    [DllImport("user32")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32")] public static extern bool GetCursorPos(out POINT p);
    [DllImport("user32")] public static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32")] public static extern IntPtr GetAncestor(IntPtr h, uint flags);
    [DllImport("user32", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
    [DllImport("dwmapi")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out int value, int size);
    public const byte VK_LWIN = 0x5B, VK_H = 0x48;
    public const int VK_CONTROL = 0x11, VK_MENU = 0x12;
    public const uint KEYEVENTF_KEYUP = 2;

    [DllImport("user32")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32")] public static extern short GetAsyncKeyState(int vk);
    [DllImport("user32", CharSet = CharSet.Unicode)] public static extern IntPtr LoadKeyboardLayout(string id, uint flags);
    [DllImport("user32")] public static extern bool PostMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    [DllImport("user32")] public static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [DllImport("user32")] public static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("user32")] public static extern bool EnumWindows(EnumProc p, IntPtr l);
    [DllImport("user32", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32")] public static extern bool IsWindow(IntPtr h);
    [DllImport("user32")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32")] public static extern bool IsZoomed(IntPtr h);
    [DllImport("user32")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32")] public static extern bool SetProcessDPIAware();
    [DllImport("user32")] public static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
    [DllImport("user32")] public static extern bool DestroyIcon(IntPtr h);
    [DllImport("user32")]
    public static extern bool UpdateLayeredWindow(IntPtr h, IntPtr dst, ref POINT ptDst, ref SIZE size,
        IntPtr src, ref POINT ptSrc, int key, ref BLENDFUNCTION blend, int flags);
    [DllImport("dwmapi")] public static extern int DwmFlush();
    [DllImport("dwmapi")] public static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int value, int size);
    [DllImport("gdi32")]
    public static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER info, uint usage,
        out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32")] public static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
    [DllImport("gdi32")] public static extern bool DeleteObject(IntPtr o);
}

static class Art
{
    public static readonly Color Accent = Color.FromArgb(217, 119, 87);

    public static void DrawStar(Graphics g, float cx, float cy, float radius, float degrees, Color color)
    {
        const int Rays = 12;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var brush = new SolidBrush(color))
        {
            for (int i = 0; i < Rays; i++)
            {
                double a = (degrees + i * 360.0 / Rays) * Math.PI / 180;
                float len = radius * (i % 2 == 0 ? 1f : 0.78f);
                float w = radius * 0.11f;
                float dx = (float)Math.Cos(a), dy = (float)Math.Sin(a);
                float nx = -dy, ny = dx;
                g.FillPolygon(brush, new[]
                {
                    new PointF(cx + dx * radius * 0.08f + nx * w * 0.5f, cy + dy * radius * 0.08f + ny * w * 0.5f),
                    new PointF(cx + dx * len * 0.45f + nx * w, cy + dy * len * 0.45f + ny * w),
                    new PointF(cx + dx * len, cy + dy * len),
                    new PointF(cx + dx * len * 0.45f - nx * w, cy + dy * len * 0.45f - ny * w),
                    new PointF(cx + dx * radius * 0.08f - nx * w * 0.5f, cy + dy * radius * 0.08f - ny * w * 0.5f),
                });
            }
        }
    }

    public static List<Icon> MakeTrayFrames(int count)
    {
        var frames = new List<Icon>();
        Size s = SystemInformation.SmallIconSize;
        for (int i = 0; i < count; i++)
        {
            using (var bmp = new Bitmap(s.Width, s.Height))
            using (var g = Graphics.FromImage(bmp))
            {
                float pulse = 1f - 0.16f * (float)Math.Abs(Math.Sin(Math.PI * i / count));
                DrawStar(g, s.Width / 2f, s.Height / 2f, s.Width * 0.5f * pulse, i * 360f / count, Accent);
                IntPtr h = bmp.GetHicon();
                using (var temp = Icon.FromHandle(h)) frames.Add((Icon)temp.Clone());
                Native.DestroyIcon(h);
            }
        }
        return frames;
    }
}

// Transparent animated star above the screen: it takes neither the mouse nor the focus.
// It draws on its own thread, so starting the terminal or updating the icon cannot drop frames.
// DwmFlush sets the pace: one frame per screen refresh, at any refresh rate.
sealed class Overlay
{
    const int Dim = 200, FadeIn = 160, FadeOut = 300, LoopMs = 1400;
    readonly Control ui = new Control();
    Thread thread;
    volatile bool stopRequested;
    volatile int holdMs;
    // Called on the render thread.
    public event Action<double> Frame;
    // Called on the UI thread.
    public event Action Finished;

    public Overlay()
    {
        var unused = ui.Handle;
    }

    // Spins until Stop() is called, but for at least minMs.
    public void Play(Point center, int minMs)
    {
        if (thread != null) return;
        stopRequested = false;
        holdMs = minMs;
        thread = new Thread(() => Run(center)) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    public void Stop()
    {
        stopRequested = true;
    }

    // Goes out at once without waiting for minMs: the window is already leaving.
    public void Cancel()
    {
        holdMs = 0;
        stopRequested = true;
    }

    static void DrawGlow(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = new GraphicsPath())
        {
            path.AddEllipse(0, 0, Dim, Dim);
            using (var glow = new PathGradientBrush(path))
            {
                glow.CenterColor = Color.FromArgb(110, Art.Accent);
                glow.SurroundColors = new[] { Color.FromArgb(0, Art.Accent) };
                g.FillPath(glow, path);
            }
        }
    }

    void Run(Point center)
    {
        var window = new NativeWindow();
        window.CreateHandle(new CreateParams
        {
            Style = unchecked((int)0x80000000), // popup
            ExStyle = 0x80000 | 0x20 | 0x80 | 0x8000000 | 0x8, // layered, transparent, tool, noactivate, topmost
            X = center.X - Dim / 2,
            Y = center.Y - Dim / 2,
            Width = Dim,
            Height = Dim,
        });
        var dst = new Native.POINT { X = center.X - Dim / 2, Y = center.Y - Dim / 2 };
        var src = new Native.POINT();
        var size = new Native.SIZE { W = Dim, H = Dim };
        var header = new Native.BITMAPINFOHEADER { Size = 40, Width = Dim, Height = -Dim, Planes = 1, BitCount = 32 };

        IntPtr screen = Native.GetDC(IntPtr.Zero);
        IntPtr mem = Native.CreateCompatibleDC(screen);
        IntPtr bits;
        IntPtr dib = Native.CreateDIBSection(screen, ref header, 0, out bits, IntPtr.Zero, 0);
        IntPtr old = Native.SelectObject(mem, dib);
        using (var canvas = new Bitmap(Dim, Dim, Dim * 4, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, bits))
        using (var g = Graphics.FromImage(canvas))
        {
            DrawGlow(g);
            g.Flush(FlushIntention.Sync);
            var background = new byte[Dim * Dim * 4];
            Marshal.Copy(bits, background, 0, background.Length);

            var clock = Stopwatch.StartNew();
            double stopAt = -1;
            bool shown = false;
            while (true)
            {
                double t = clock.Elapsed.TotalMilliseconds;
                double alpha = Math.Min(1.0, t / FadeIn);
                if (stopRequested && stopAt < 0 && t >= holdMs) stopAt = t;
                if (stopAt >= 0)
                {
                    double leave = (t - stopAt) / FadeOut;
                    if (leave >= 1) break;
                    alpha *= 1 - leave;
                }
                Marshal.Copy(background, 0, bits, background.Length);
                // Star rays alternate, so a 60° turn closes the loop without a jump.
                Art.DrawStar(g, Dim / 2f, Dim / 2f, Dim * 0.33f, (float)(t % LoopMs * 60 / LoopMs), Art.Accent);
                g.Flush(FlushIntention.Sync);
                var blend = new Native.BLENDFUNCTION { Op = 0, Flags = 0, Alpha = (byte)(alpha * 255), Format = 1 };
                Native.UpdateLayeredWindow(window.Handle, screen, ref dst, ref size, mem, ref src, 0, ref blend, 2);
                if (!shown)
                {
                    Native.ShowWindow(window.Handle, Native.SW_SHOWNOACTIVATE);
                    shown = true;
                }
                if (Frame != null) Frame(t / 1000.0);
                Application.DoEvents();
                Native.DwmFlush();
            }
        }
        window.DestroyHandle();
        Native.SelectObject(mem, old);
        Native.DeleteObject(dib);
        Native.DeleteDC(mem);
        Native.ReleaseDC(IntPtr.Zero, screen);
        ui.BeginInvoke((Action)delegate
        {
            thread = null;
            if (Finished != null) Finished();
        });
    }
}

// Slides the window up and back. Moves it on its own thread, paced by DwmFlush like the star.
sealed class WindowSlider
{
    const int InMs = 230, OutMs = 170;
    const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;
    readonly Control ui = new Control();
    // Called on the motion thread: time in seconds and window position.
    public event Action<double, int> Frame;

    public WindowSlider()
    {
        var unused = ui.Handle;
    }

    // Windows' own show and hide animation would overlap the slide.
    static void DisableWindowsTransitions(IntPtr h)
    {
        int on = 1;
        Native.DwmSetWindowAttribute(h, DWMWA_TRANSITIONS_FORCEDISABLED, ref on, 4);
    }

    static void Move(IntPtr h, int x, int y)
    {
        Native.SetWindowPos(h, IntPtr.Zero, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
    }

    // done is called on the UI thread.
    public void In(IntPtr h, Action done)
    {
        Native.RECT r;
        Native.GetWindowRect(h, out r);
        if (Native.IsZoomed(h))
        {
            Native.ShowWindow(h, Native.SW_SHOW);
            done();
            return;
        }
        DisableWindowsTransitions(h);
        int from = Screen.FromHandle(h).Bounds.Bottom;
        Move(h, r.Left, from);
        Native.ShowWindow(h, Native.SW_SHOW);
        Native.SetForegroundWindow(h);
        Run(h, r.Left, from, r.Top, InMs, p => 1 - Math.Pow(1 - p, 3), done);
    }

    // A hidden window goes back to its place, so the next show knows where to slide to.
    public void Out(IntPtr h, Action done)
    {
        Native.RECT r;
        Native.GetWindowRect(h, out r);
        if (Native.IsZoomed(h))
        {
            Native.ShowWindow(h, Native.SW_HIDE);
            done();
            return;
        }
        DisableWindowsTransitions(h);
        int home = r.Top, x = r.Left;
        Run(h, x, home, Screen.FromHandle(h).Bounds.Bottom, OutMs, p => p * p * p, () =>
        {
            Native.ShowWindow(h, Native.SW_HIDE);
            Move(h, x, home);
            done();
        });
    }

    void Run(IntPtr h, int x, int from, int to, int ms, Func<double, double> ease, Action done)
    {
        var thread = new Thread(() =>
        {
            var clock = Stopwatch.StartNew();
            while (true)
            {
                double t = clock.Elapsed.TotalMilliseconds;
                double p = Math.Min(1.0, t / ms);
                int y = from + (int)Math.Round((to - from) * ease(p));
                Move(h, x, y);
                if (Frame != null) Frame(t / 1000.0, y);
                if (p >= 1) break;
                Native.DwmFlush();
            }
            ui.BeginInvoke(done);
        }) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
        thread.Start();
    }
}

sealed class HotkeyWindow : NativeWindow
{
    public event Action<int> Pressed;
    public HotkeyWindow() { CreateHandle(new CreateParams()); }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY && Pressed != null) Pressed(m.WParam.ToInt32());
        base.WndProc(ref m);
    }
}

// Signals from Claude Code hooks: a hook runs ClaudeTray.exe --signal done|attention.
static class TraySignal
{
    public const string Done = "done", Attention = "attention";
    const uint TH32CS_SNAPPROCESS = 2;
    const int MaxDepth = 16;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct PROCESSENTRY32
    {
        public uint Size, Usage, ProcessId;
        public IntPtr HeapId;
        public uint ModuleId, Threads, ParentProcessId;
        public int PriClassBase;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string ExeFile;
    }

    [DllImport("kernel32", SetLastError = true)] static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32", CharSet = CharSet.Unicode)] static extern bool Process32First(IntPtr snap, ref PROCESSENTRY32 e);
    [DllImport("kernel32", CharSet = CharSet.Unicode)] static extern bool Process32Next(IntPtr snap, ref PROCESSENTRY32 e);
    [DllImport("kernel32")] static extern bool CloseHandle(IntPtr h);

    public static string EventName(string kind) { return @"Local\ClaudeTray." + kind; }

    // Background Claude sessions run under their own daemon and have nothing to do with the tray window.
    static bool RunsInTerminal()
    {
        var parents = new Dictionary<uint, uint>();
        var names = new Dictionary<uint, string>();
        IntPtr snap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        var e = new PROCESSENTRY32 { Size = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32)) };
        for (bool more = Process32First(snap, ref e); more; more = Process32Next(snap, ref e))
        {
            parents[e.ProcessId] = e.ParentProcessId;
            names[e.ProcessId] = e.ExeFile;
        }
        CloseHandle(snap);
        uint pid = (uint)Process.GetCurrentProcess().Id;
        for (int i = 0; i < MaxDepth && parents.TryGetValue(pid, out pid); i++)
        {
            string name;
            if (names.TryGetValue(pid, out name) && string.Equals(name, "WindowsTerminal.exe", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    public static void Send(string kind)
    {
        if (!RunsInTerminal()) return;
        try
        {
            using (var signal = EventWaitHandle.OpenExisting(EventName(kind))) signal.Set();
        }
        catch (WaitHandleCannotBeOpenedException) { } // The tray is not running, so nobody needs a reminder.
    }
}

// Repeats the sound until the Claude window is opened: the hook plays the first one, this only repeats.
sealed class SoundReminder
{
    public const int DefaultRepeatMs = 60000;
    const int MaxRepeats = 30;
    readonly Func<bool> windowOpen;
    readonly int repeatMs;
    readonly FormsTimer timer = new FormsTimer { Interval = 500 };
    readonly EventWaitHandle done = new EventWaitHandle(false, EventResetMode.AutoReset, TraySignal.EventName(TraySignal.Done));
    readonly EventWaitHandle attention = new EventWaitHandle(false, EventResetMode.AutoReset, TraySignal.EventName(TraySignal.Attention));
    readonly Stopwatch sinceSound = new Stopwatch();
    readonly string sounds = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".claude\sounds");
    System.Media.SoundPlayer player;
    string sound;
    int repeatsLeft;

    // Called on every repeat, for testing.
    public event Action<string> Played;

    public SoundReminder(Func<bool> windowOpen, int repeatMs)
    {
        this.windowOpen = windowOpen;
        this.repeatMs = repeatMs;
        timer.Tick += delegate { Step(); };
        timer.Start();
    }

    void Step()
    {
        bool isDone = done.WaitOne(0), isAttention = attention.WaitOne(0);
        if (windowOpen()) { sound = null; return; }
        if (isAttention) Arm("notify-quiet.wav");
        if (isDone) Arm("coin-quiet.wav");
        if (sound == null || sinceSound.ElapsedMilliseconds < repeatMs) return;
        Play();
        sinceSound.Restart();
        if (--repeatsLeft == 0) sound = null;
    }

    void Arm(string file)
    {
        sound = file;
        repeatsLeft = MaxRepeats;
        sinceSound.Restart();
    }

    void Play()
    {
        try
        {
            if (player != null) player.Dispose();
            player = new System.Media.SoundPlayer(Path.Combine(sounds, sound));
            player.Play();
        }
        catch (IOException) { }
        catch (InvalidOperationException) { }
        if (Played != null) Played(sound);
    }
}

sealed class App : ApplicationContext
{
    const string WindowClass = "CASCADIA_HOSTING_WINDOW_CLASS";
    const string WindowTitle = "Claude";
    const string WtWindowName = "ClaudeTray";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "ClaudeTray";
    const string HotkeyText = "Ctrl+Alt+C";
    const string VoiceHotkeyText = "Ctrl+Alt+V";
    const int ToggleHotkeyId = 1, VoiceHotkeyId = 2;
    const int KeyReleaseWaitMs = 2000;
    const int VoicePanelMaxSize = 600;
    const int TrayFrameCount = 16;
    const int WindowMargin = 16;
    const int StarMs = 1200;

    readonly NotifyIcon tray = new NotifyIcon();
    readonly Overlay overlay = new Overlay();
    readonly HotkeyWindow hotkeys = new HotkeyWindow();
    readonly WindowSlider slider = new WindowSlider();
    readonly FormsTimer trayTimer = new FormsTimer { Interval = 125 };
    readonly SoundReminder reminder;
    readonly List<Icon> frames;
    readonly ToolStripMenuItem autostartItem;
    readonly string claudePath = FindClaude();
    IntPtr target = IntPtr.Zero;
    IntPtr previous = IntPtr.Zero;
    int trayFrame;
    bool busy;
    bool voicePending;
    bool voiceWaiting;

    public App()
    {
        frames = Art.MakeTrayFrames(TrayFrameCount);
        tray.Icon = frames[0];
        tray.Text = "Claude: " + HotkeyText + ", voice: " + VoiceHotkeyText;
        tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) TrayClick(); };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Show / hide  (" + HotkeyText + ")", null, delegate { Toggle(); });
        menu.Items.Add("Voice typing  (" + VoiceHotkeyText + ")", null, delegate { Voice(); });
        menu.Items.Add("New tab", null, delegate { NewTab(); });
        menu.Items.Add(new ToolStripSeparator());
        autostartItem = new ToolStripMenuItem("Start with Windows") { Checked = IsAutostart() };
        autostartItem.Click += delegate { SetAutostart(!autostartItem.Checked); autostartItem.Checked = IsAutostart(); };
        menu.Items.Add(autostartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, delegate { Quit(); });
        tray.ContextMenuStrip = menu;
        tray.Visible = true;

        // Tray icon updates go through the Windows shell and stall, so the icon changes rarely.
        trayTimer.Tick += delegate
        {
            trayFrame = (trayFrame + 1) % TrayFrameCount;
            tray.Icon = frames[trayFrame];
        };
        overlay.Finished += () => { trayTimer.Stop(); trayFrame = 0; tray.Icon = frames[0]; };

        hotkeys.Pressed += id => { if (id == VoiceHotkeyId) Voice(); else Toggle(); };
        bool ok = Native.RegisterHotKey(hotkeys.Handle, ToggleHotkeyId,
            Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT, (uint)Keys.C);
        if (!ok)
            tray.ShowBalloonTip(5000, "Claude", HotkeyText + " is taken by another program. Click the tray icon instead.", ToolTipIcon.Warning);
        bool voiceOk = Native.RegisterHotKey(hotkeys.Handle, VoiceHotkeyId,
            Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT, (uint)Keys.V);
        if (!voiceOk)
            tray.ShowBalloonTip(5000, "Claude", VoiceHotkeyText + " is taken by another program. Voice typing is in the tray menu.", ToolTipIcon.Warning);

        reminder = new SoundReminder(IsWindowOpenOrGone, SoundReminder.DefaultRepeatMs);
    }

    // No reminder is needed if the window is on screen or already closed.
    bool IsWindowOpenOrGone()
    {
        IntPtr h = FindTarget();
        return h == IntPtr.Zero || (!busy && Native.IsWindowVisible(h) && !Native.IsIconic(h));
    }

    // Voice typing panel: a small window without a title. A closed panel stays, hidden by the system.
    // EnumWindows does not list it, so it is found through FindWindowEx.
    static IntPtr FindVoicePanel()
    {
        IntPtr h = IntPtr.Zero;
        var title = new StringBuilder(8);
        while ((h = Native.FindWindowEx(IntPtr.Zero, h, "ApplicationFrameWindow", null)) != IntPtr.Zero)
        {
            title.Clear();
            Native.GetWindowText(h, title, title.Capacity);
            if (title.Length != 0 || !Native.IsWindowVisible(h)) continue;
            int cloaked;
            if (Native.DwmGetWindowAttribute(h, Native.DWMWA_CLOAKED, out cloaked, 4) != 0 || cloaked != 0) continue;
            Native.RECT r;
            if (!Native.GetWindowRect(h, out r)) continue;
            if (r.Right - r.Left < VoicePanelMaxSize && r.Bottom - r.Top < VoicePanelMaxSize) return h;
        }
        return IntPtr.Zero;
    }

    // Closes the panel by clicking its X, then puts the cursor back.
    // WM_CLOSE must not be used: the panel hides, but Win+H stops working until TextInputHost restarts.
    // The panel buttons are invisible to UI Automation, so the X position is computed from the window size.
    void CloseVoicePanel(IntPtr panel)
    {
        Native.RECT r;
        if (!Native.GetWindowRect(panel, out r)) return;
        int width = r.Right - r.Left;
        var button = new Native.POINT { X = r.Right - width / 8, Y = r.Top + width * 3 / 32 };
        if (Native.GetAncestor(Native.WindowFromPoint(button), Native.GA_ROOT) != panel)
        {
            tray.ShowBalloonTip(5000, "Claude", "The voice typing panel is covered by another window. Close it with its X.", ToolTipIcon.Warning);
            return;
        }
        Native.POINT cursor;
        Native.GetCursorPos(out cursor);
        Native.SetCursorPos(button.X, button.Y);
        Thread.Sleep(30);
        Native.mouse_event(Native.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        Native.mouse_event(Native.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(30);
        Native.SetCursorPos(cursor.X, cursor.Y);
    }

    // Shows the Claude window and starts Windows voice typing. A second call closes the panel.
    void Voice()
    {
        if (busy || voiceWaiting) return;
        IntPtr panel = FindVoicePanel();
        if (panel != IntPtr.Zero)
        {
            CloseVoicePanel(panel);
            return;
        }
        IntPtr h = FindTarget();
        IntPtr current = Native.GetForegroundWindow();
        if (current != h) previous = current;
        if (h == IntPtr.Zero) { voicePending = true; Launch(); }
        else if (!Native.IsWindowVisible(h)) { voicePending = true; ShowTarget(h); }
        else
        {
            if (Native.IsIconic(h)) Native.ShowWindow(h, Native.SW_RESTORE);
            StartDictation(h);
        }
    }

    // Win+H cannot be pressed while Ctrl+Alt is held: that would be another shortcut. So wait for the release.
    void StartDictation(IntPtr h)
    {
        voiceWaiting = true;
        var clock = Stopwatch.StartNew();
        var wait = new FormsTimer { Interval = 40 };
        wait.Tick += delegate
        {
            bool held = (Native.GetAsyncKeyState(Native.VK_CONTROL) & 0x8000) != 0
                || (Native.GetAsyncKeyState(Native.VK_MENU) & 0x8000) != 0;
            if (held && clock.ElapsedMilliseconds < KeyReleaseWaitMs) return;
            wait.Stop(); wait.Dispose();
            voiceWaiting = false;
            if (held)
            {
                tray.ShowBalloonTip(5000, "Claude", "Voice typing did not start: release " + VoiceHotkeyText + " and press it again.", ToolTipIcon.Warning);
                return;
            }
            PressWinH(h);
        };
        wait.Start();
    }

    // Voice typing listens in the language of the current keyboard layout.
    void PressWinH(IntPtr h)
    {
        Focus(h);
        if (Native.GetForegroundWindow() != h)
        {
            tray.ShowBalloonTip(5000, "Claude", "Could not focus the Claude window. Click it and press " + VoiceHotkeyText + ".", ToolTipIcon.Warning);
            return;
        }
        Native.keybd_event(Native.VK_LWIN, 0, 0, UIntPtr.Zero);
        Native.keybd_event(Native.VK_H, 0, 0, UIntPtr.Zero);
        Native.keybd_event(Native.VK_H, 0, Native.KEYEVENTF_KEYUP, UIntPtr.Zero);
        Native.keybd_event(Native.VK_LWIN, 0, Native.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    // The native installer's location comes first, then anything on PATH, such as an npm install.
    static string FindClaude()
    {
        string native = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".local\bin\claude.exe");
        if (File.Exists(native)) return native;
        string pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (string dir in pathVar.Split(';'))
        {
            if (dir.Trim().Length == 0) continue;
            foreach (string name in new[] { "claude.exe", "claude.cmd" })
            {
                try
                {
                    string candidate = Path.Combine(dir.Trim(), name);
                    if (File.Exists(candidate)) return candidate;
                }
                catch (ArgumentException) { } // A malformed PATH entry.
            }
        }
        return native;
    }

    static string WorkDir() { return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); }

    IntPtr FindTarget()
    {
        if (target != IntPtr.Zero && Native.IsWindow(target)) return target;
        IntPtr found = IntPtr.Zero;
        var cls = new StringBuilder(128);
        var title = new StringBuilder(128);
        Native.EnumWindows((h, l) =>
        {
            cls.Clear(); title.Clear();
            Native.GetClassName(h, cls, cls.Capacity);
            if (cls.ToString() != WindowClass) return true;
            Native.GetWindowText(h, title, title.Capacity);
            if (title.ToString() != WindowTitle) return true;
            found = h;
            return false;
        }, IntPtr.Zero);
        target = found;
        return found;
    }

    // A click on the icon takes focus from the window, so an open window is hidden without a focus check.
    void TrayClick()
    {
        if (busy) return;
        IntPtr h = FindTarget();
        if (h != IntPtr.Zero && Native.IsWindowVisible(h) && !Native.IsIconic(h)) HideTarget(h);
        else Toggle();
    }

    // Gives focus back to the previous window, otherwise input would go to the hidden one.
    void ReturnFocus()
    {
        IntPtr to = previous != IntPtr.Zero && Native.IsWindow(previous) && Native.IsWindowVisible(previous)
            ? previous : Native.FindWindow("Shell_TrayWnd", null);
        if (to != IntPtr.Zero) Native.SetForegroundWindow(to);
    }

    void Toggle()
    {
        if (busy) return;
        IntPtr h = FindTarget();
        IntPtr current = Native.GetForegroundWindow();
        if (current != h) previous = current;
        if (h == IntPtr.Zero) Launch();
        else if (!Native.IsWindowVisible(h)) ShowTarget(h);
        else if (Native.IsIconic(h)) { Native.ShowWindow(h, Native.SW_RESTORE); Focus(h); }
        else if (current == h) HideTarget(h);
        else Focus(h);
    }

    static void Focus(IntPtr h) { Native.SetForegroundWindow(h); }

    // A small window in the lower right corner, above the taskbar. A maximized window is left alone.
    static Rectangle TargetRect()
    {
        Rectangle area = Screen.PrimaryScreen.WorkingArea;
        int w = area.Width * 3 / 10, ht = area.Height * 2 / 5;
        return new Rectangle(area.Right - w - WindowMargin, area.Bottom - ht - WindowMargin, w, ht);
    }

    static void PlaceTarget(IntPtr h)
    {
        if (Native.IsZoomed(h)) return;
        Rectangle r = TargetRect();
        Native.SetWindowPos(h, IntPtr.Zero, r.Left, r.Top, r.Width, r.Height, Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
    }

    // The star shines in the center of the place the window slides to.
    void PlayStar(IntPtr h)
    {
        Rectangle r = h != IntPtr.Zero && Native.IsZoomed(h) ? Screen.PrimaryScreen.WorkingArea : TargetRect();
        overlay.Play(new Point(r.Left + r.Width / 2, r.Top + r.Height / 2), StarMs);
        trayTimer.Start();
    }

    void ShowTarget(IntPtr h)
    {
        busy = true;
        PlaceTarget(h);
        PlayStar(h);
        overlay.Stop();
        slider.In(h, () =>
        {
            Focus(h);
            busy = false;
            if (voicePending)
            {
                voicePending = false;
                StartDictation(h);
            }
        });
    }

    void HideTarget(IntPtr h)
    {
        busy = true;
        overlay.Cancel();
        ReturnFocus();
        slider.Out(h, () => { busy = false; });
    }

    void StartWt(string extra)
    {
        string args = "-w " + WtWindowName + " new-tab --title " + WindowTitle +
            " --suppressApplicationTitle -d \"" + WorkDir() + "\" \"" + claudePath + "\"";
        Process.Start(new ProcessStartInfo("wt.exe", args + extra) { UseShellExecute = true });
    }

    void Launch()
    {
        if (!File.Exists(claudePath))
        {
            voicePending = false;
            tray.ShowBalloonTip(5000, "Claude", "Not found: " + claudePath, ToolTipIcon.Error);
            return;
        }
        busy = true;
        PlayStar(IntPtr.Zero);
        try { StartWt(""); }
        catch (Exception ex)
        {
            overlay.Cancel(); busy = false; voicePending = false;
            tray.ShowBalloonTip(5000, "Claude", "Could not open Windows Terminal: " + ex.Message, ToolTipIcon.Error);
            return;
        }
        var clock = Stopwatch.StartNew();
        var poll = new FormsTimer { Interval = 120 };
        poll.Tick += delegate
        {
            IntPtr h = FindTarget();
            if (h != IntPtr.Zero)
            {
                poll.Stop(); poll.Dispose();
                ShowTarget(h);
            }
            else if (clock.ElapsedMilliseconds > 12000)
            {
                poll.Stop(); poll.Dispose();
                overlay.Stop();
                busy = false;
                voicePending = false;
                tray.ShowBalloonTip(5000, "Claude", "The terminal window did not appear within 12 seconds.", ToolTipIcon.Warning);
            }
        };
        poll.Start();
    }

    void NewTab()
    {
        IntPtr h = FindTarget();
        if (h == IntPtr.Zero) { Toggle(); return; }
        if (!Native.IsWindowVisible(h)) Native.ShowWindow(h, Native.SW_SHOW);
        StartWt("");
        Focus(h);
    }

    static bool IsAutostart()
    {
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
            return k != null && k.GetValue(RunName) != null;
    }

    static void SetAutostart(bool on)
    {
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (on) k.SetValue(RunName, "\"" + Application.ExecutablePath + "\"");
            else k.DeleteValue(RunName, false);
        }
    }

    void Quit()
    {
        // A hidden window must not stay invisible: the Claude session would be lost.
        IntPtr h = FindTarget();
        if (h != IntPtr.Zero && !Native.IsWindowVisible(h)) Native.ShowWindow(h, Native.SW_SHOW);
        Native.UnregisterHotKey(hotkeys.Handle, ToggleHotkeyId);
        Native.UnregisterHotKey(hotkeys.Handle, VoiceHotkeyId);
        tray.Visible = false;
        tray.Dispose();
        Application.Exit();
    }
}

static class Program
{
    // --test-star <file>: shows the star for 2 seconds and logs frame intervals in ms.
    static void TestStar(string logPath)
    {
        var overlay = new Overlay();
        var log = new StringBuilder();
        double last = -1;
        overlay.Frame += sec =>
        {
            if (last >= 0) log.AppendLine(((sec - last) * 1000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            last = sec;
        };
        overlay.Finished += () => { File.WriteAllText(logPath, log.ToString()); Application.Exit(); };
        var stop = new FormsTimer { Interval = 2000 };
        stop.Tick += delegate { stop.Stop(); overlay.Stop(); };
        Rectangle area = Screen.PrimaryScreen.WorkingArea;
        overlay.Play(new Point(area.Left + area.Width / 2, area.Top + area.Height / 2), 0);
        stop.Start();
        Application.Run();
    }

    // --test-slide <hwnd> <file>: slides the window down and back, logs the interval in ms and the position.
    static void TestSlide(IntPtr h, string logPath)
    {
        var slider = new WindowSlider();
        var log = new StringBuilder();
        double last = -1;
        slider.Frame += (sec, y) =>
        {
            if (last >= 0) log.AppendLine(((sec - last) * 1000).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " " + y);
            last = sec;
        };
        slider.Out(h, () =>
        {
            last = -1;
            slider.In(h, () => { File.WriteAllText(logPath, log.ToString()); Application.Exit(); });
        });
        Application.Run();
    }

    // --test-reminder <file>: the window counts as hidden for 6 seconds, repeats every 1.5 seconds, then open for 3 more.
    static void TestReminder(string logPath)
    {
        var clock = Stopwatch.StartNew();
        var log = new StringBuilder();
        var reminder = new SoundReminder(() => clock.ElapsedMilliseconds > 6000, 1500);
        reminder.Played += sound => log.AppendLine(clock.ElapsedMilliseconds + " " + sound);
        var stop = new FormsTimer { Interval = 9000 };
        stop.Tick += delegate { stop.Stop(); File.WriteAllText(logPath, log.ToString()); Application.Exit(); };
        stop.Start();
        Application.Run();
    }

    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--signal")
        {
            TraySignal.Send(args[1]);
            return;
        }
        if (args.Length == 2 && args[0] == "--test-reminder")
        {
            TestReminder(args[1]);
            return;
        }
        if (args.Length == 3 && args[0] == "--test-slide")
        {
            Native.SetProcessDPIAware();
            TestSlide(new IntPtr(long.Parse(args[1])), args[2]);
            return;
        }
        if (args.Length == 2 && args[0] == "--test-star")
        {
            Native.SetProcessDPIAware();
            TestStar(args[1]);
            return;
        }
        bool first;
        using (new Mutex(true, "ClaudeTray.SingleInstance", out first))
        {
            if (!first) return;
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new App());
        }
    }
}
