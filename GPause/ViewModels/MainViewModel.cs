using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPause.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PMan;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace GPause.ViewModels;

public class MainViewModel : ObservableRecipient
{
    private ObservableCollection<ProcessModel>? _processesList;
    public ObservableCollection<ProcessModel>? ProcessesList
    {
        get => _processesList;
        set => SetProperty(ref _processesList, value);
    }

    private int _selectedProcessIndex;
    public int SelectedProcessIndex
    {
        get => _selectedProcessIndex;
        set => SetProperty(ref _selectedProcessIndex, value);
    }

    public ICommand PopulateProcessesListCommand
    {
        get; private set;
    }
    /// <summary>
    /// Populate the processes list with entries (Window title, process name and process ID).
    /// </summary>
    private async Task<AsyncVoidMethodBuilder> PopulateProcessesList()
    {
        Debug.WriteLine("Populating Processes List...");
        var _ProcessesList = new ObservableCollection<ProcessModel>();
        foreach (var process in ProcessManager.RunningProcesses())
        {
            var windowText = ProcessManager.GetWindowTitleText(process);
            if (windowText == null)
            {
                continue;
            }
            var isSuspended = ProcessManager.HasSuspended(process);
            var filePath = ProcessManager.GetProcessLocation(process);
            if (filePath == null)
            {
                continue;
            }
            Debug.WriteLine($"Program: {windowText}\nSuspended: {isSuspended}\n");
            _ProcessesList.Add(new ProcessModel
            {
                Name = windowText,
                SystemName = process.ProcessName,
                Id = process.Id,
                Suspended = isSuspended,
                Process = process,
                ExecutablePath = filePath,
                AppIcon = await GetAppIcon(filePath)
            });
        }
        ProcessesList = _ProcessesList;
        var e = new AsyncVoidMethodBuilder();
        e.SetResult();
        return e;
    }

    public ICommand SuspendProcessCommand
    {
        get; private set;
    }
    private async void SuspendProcess()
    {
        if (SelectedProcessIndex <= -1)
        {
            return;
        }
        Process targetProcess;
        try
        {
            targetProcess = ProcessesList![SelectedProcessIndex].Process!;
        }
        catch (ArgumentOutOfRangeException)
        {
            await PopulateProcessesList();
            targetProcess = ProcessesList![SelectedProcessIndex].Process!;
        }
        Debug.WriteLine($"Minimizing window of process (Index {SelectedProcessIndex})");
        ProcessManager.MinimizeWindow(targetProcess);
        Debug.WriteLine($"Suspending process (Index {SelectedProcessIndex})");
        ProcessManager.Suspend(targetProcess);
        Debug.WriteLine("Re-populating list (suspended)");
        await PopulateProcessesList();
    }

    public ICommand ResumeProcessCommand
    {
        get; private set;
    }
    private async void ResumeProcess()
    {
        if (SelectedProcessIndex <= -1)
        {
            return;
        }
        Process targetProcess;
        Debug.WriteLine($"Resuming process (Index {SelectedProcessIndex})");
        try
        {
            targetProcess = ProcessesList![SelectedProcessIndex].Process!;
        }
        catch (ArgumentOutOfRangeException)
        {
            await PopulateProcessesList();
            targetProcess = ProcessesList![SelectedProcessIndex].Process!;
        }
        ProcessManager.Resume(targetProcess);
        Debug.WriteLine("Re-populating list (resumed)");
        await PopulateProcessesList();
        Debug.WriteLine($"Restoring window of process (Index {SelectedProcessIndex})");
        ProcessManager.RestoreWindow(targetProcess);
    }

    public ICommand OpenInFileExplorerCommand
    {
        get; private set;
    }
    private async void OpenInFileExplorer()
    {
        if (SelectedProcessIndex <= -1)
        {
            return;
        }
        string targetProcessPath;
        try
        {
            targetProcessPath = ProcessesList![SelectedProcessIndex].ExecutablePath!;
        }
        catch (ArgumentOutOfRangeException)
        {
            await PopulateProcessesList();
            targetProcessPath = ProcessesList![SelectedProcessIndex].ExecutablePath!;
        }
        Process.Start("explorer.exe", "/select, \"" + targetProcessPath + "\"");
    }

    private async void InitializeProcessList()
    {
        await Task.Delay(200);
        await PopulateProcessesList();
    }

    private static async Task<BitmapImage> GetAppIcon(string processPath)
    {
        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(processPath);
        var iconThumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 32);
        var bi = new BitmapImage();
        bi.SetSource(iconThumbnail);
        return bi;
    }

    public MainViewModel()
    {
        ProcessesList = new ObservableCollection<ProcessModel>();
        PopulateProcessesListCommand = new AsyncRelayCommand(PopulateProcessesList);
        SuspendProcessCommand = new RelayCommand(SuspendProcess);
        ResumeProcessCommand = new RelayCommand(ResumeProcess);
        OpenInFileExplorerCommand = new RelayCommand(OpenInFileExplorer);
        SelectedProcessIndex = 0;
        InitializeProcessList();
    }
}
