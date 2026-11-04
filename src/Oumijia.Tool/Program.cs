using System;
using System.Threading;
using System.Windows.Forms;

namespace Oumijia.Tool;

public static class Program
{
    private static readonly object Gate = new object();
    private static bool _windowOpen;

    [STAThread]
    public static void Main()
    {
#if NET8_0_OR_GREATER
        ApplicationConfiguration.Initialize();
#else
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
#endif
        App.Initialize();
        Application.Run(new MainForm());
    }

    /// <summary>
    /// Opens the tool window. Safe to call from the EPLAN add-in: when no window is
    /// open yet it spawns a dedicated STA thread with its own message loop.
    /// </summary>
    public static void ShowMainWindow()
    {
        lock (Gate)
        {
            if (_windowOpen) return;
            _windowOpen = true;
        }

        var thread = new Thread(() =>
        {
            try
            {
                App.Initialize();
                var form = new MainForm();
                form.FormClosed += (_, __) =>
                {
                    lock (Gate) { _windowOpen = false; }
                };
                Application.Run(form);
            }
            catch
            {
                lock (Gate) { _windowOpen = false; }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
    }
}
