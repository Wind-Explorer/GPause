using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace PMan;
// TODO: Implement proper exception handling.
public static partial class ProcessManager
{
    [Flags]
    private enum ThreadAccess : int
    {
        TERMINATE = 0x0001,
        SUSPEND_RESUME = 0x0002,
        GET_CONTEXT = 0x0008,
        SET_CONTEXT = 0x0010,
        SET_INFORMATION = 0x0020,
        QUERY_INFORMATION = 0x0040,
        SET_THREAD_TOKEN = 0x0080,
        IMPERSONATE = 0x0100,
        DIRECT_IMPERSONATION = 0x0200
    }

    public static string[] KnownWindowsSystemProcesses = new string[] {"GPause", "TextInputHost", "ApplicationFrameHost", "perfmon", "system", "System", "winlogon", "Winlogon", "services", "Services", "smss", "Smss", "lsass", "LSass", "svchost", "Svchost", "spoolsv", "Spoolsv", "csrss", "Csrss", "explorer", "Explorer", "taskhost", "Taskhost", "dwm", "Dwm", "wininit", "Wininit"};

    [SuppressUnmanagedCodeSecurity]
    private static partial class NativeMethods
    {
        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CloseHandle(IntPtr hObject);

        [LibraryImport("kernel32.dll")]
        public static partial IntPtr OpenThread(ThreadAccess dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwThreadId);

        [LibraryImport("kernel32.dll")]
        public static partial uint ResumeThread(IntPtr hThread);

        [LibraryImport("kernel32.dll")]
        public static partial uint SuspendThread(IntPtr hThread);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }

    /// <summary>
    /// Gets the window title of specified process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns>The title of the window of the process as a string. Returns null if no windows or title is found.</returns>
    public static string? GetWindowTitleText(Process process)
    {
        if (string.IsNullOrEmpty(process.MainWindowTitle))
        {
            return null;
        }
        return process.MainWindowTitle;
    }

    /// <summary>
    /// Identify the processes running on the system.
    /// </summary>
    /// <returns>A list (array) of <c>Process</c>es.</returns>
    public static Process[] RunningProcesses()
    {
        return Process.GetProcesses();
    }

    /// <summary>
    /// Identify the name (executable) of specified process.
    /// </summary>
    /// <param name="process"></param>
    /// <returns>The name ("notepad" for example) as a string.</returns>
    public static string GetProcessName(Process process)
    {
        return process.ProcessName;
    }

    /// <summary>
    /// Identify the names of processes running on the system.
    /// </summary>
    /// <returns>A list (array) of names as strings. (Stripped repeated entries)</returns>
    public static string[] RunningProcessesNames()
    {
        List<string> uniqueProcesses = new();
        foreach (var i in Process.GetProcesses())
        {
            if (uniqueProcesses.Contains(i.ProcessName))
            {
                continue;
            }
            uniqueProcesses.Add(i.ProcessName);
        }
        return uniqueProcesses.ToArray();
    }

    /// <summary>
    /// Identify whether a process is suspended by checking if the specified process has any suspended threads
    /// </summary>
    /// <param name="process"></param>
    /// <returns>A boolean that determines whether a suspended thread is found.</returns>
    public static bool HasSuspended(Process process)
    {
        var isSuspended = false;
        foreach (ProcessThread thread in process.Threads)
        {
            if (thread.ThreadState == System.Diagnostics.ThreadState.Wait && thread.WaitReason == ThreadWaitReason.Suspended)
            {
                isSuspended = true;
                break;
            }
        }
        return isSuspended;
    }

    /// <summary>
    /// Terminates specified process.
    /// </summary>
    /// <param name="process"></param>
    public static void Terminate(Process process)
    {
        process.Kill();
    }

    /// <summary>
    /// Suspends the specified process.
    /// </summary>
    /// <param name="process"></param>
    public static void Suspend(this Process process)
    {
        Debug.WriteLine($"Suspending process ({process.ProcessName})");
        static void suspend(ProcessThread pt)
        {
            var threadHandle = NativeMethods.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pt.Id);

            if (threadHandle != IntPtr.Zero)
            {
                try { _ = NativeMethods.SuspendThread(threadHandle); }
                finally { _ = NativeMethods.CloseHandle(threadHandle); }
            };
        }

        List<ProcessThread> threads = new();
        foreach (ProcessThread thread in process.Threads)
        {
            threads.Add(thread);
        }

        if (threads.Count > 1)
        {
            Parallel.ForEach(threads,
                new ParallelOptions { MaxDegreeOfParallelism = threads.Count },
                pt => suspend(pt));
        }
        else
        {
            suspend(threads[0]);
        }
    }

    /// <summary>
    /// Resumes the specified (suspended) thread.
    /// </summary>
    /// <param name="process"></param>
    public static void Resume(this Process process)
    {
        Debug.WriteLine($"Resuming process ({process.ProcessName})");
        static void resume(ProcessThread pt)
        {
            var threadHandle = NativeMethods.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pt.Id);
            if (threadHandle != IntPtr.Zero)
            {
                try { _ = NativeMethods.ResumeThread(threadHandle); }
                finally { _ = NativeMethods.CloseHandle(threadHandle); }
            }
        }

        List<ProcessThread> threads = new();
        foreach (ProcessThread thread in process.Threads)
        {
            threads.Add(thread);
        }

        if (threads.Count > 1)
        {
            Parallel.ForEach(threads,
                new ParallelOptions { MaxDegreeOfParallelism = threads.Count },
                pt => resume(pt));
        }
        else
        {
            resume(threads[0]);
        }
    }

    /// <summary>
    /// Minimizes any existing window belonging to specified process.
    /// </summary>
    /// <param name="process"></param>
    public static void MinimizeWindow(Process process)
    {
        var processes = Process.GetProcessesByName(process.ProcessName);
        if (processes.Length > 0)
        {
            var targetProcess = processes[0];
            NativeMethods.ShowWindow(targetProcess.MainWindowHandle, 6); // 6 is the SW_MINIMIZE constant
        }
    }

    /// <summary>
    /// Minimizes any existing window belonging to specified process.
    /// </summary>
    /// <param name="process"></param>
    public static void RestoreWindow(Process process)
    {
        var processes = Process.GetProcessesByName(process.ProcessName);
        if (processes.Length > 0)
        {
            var targetProcess = processes[0];
            NativeMethods.ShowWindow(targetProcess.MainWindowHandle, 9); // 9 is the SW_RESTORE constant
        }
    }

    public static string? GetProcessLocation(Process process)
    {
        try
        {
            return process.MainModule!.FileName;
        }
        catch
        {
            return null;
        }
    }
}
