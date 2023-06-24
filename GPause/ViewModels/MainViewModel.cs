using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPause.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using PMan;
using Windows.Storage.FileProperties;

namespace GPause.ViewModels;

public class MainViewModel : ObservableRecipient, INotifyPropertyChanged
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
        set
        {
            SetProperty(ref _selectedProcessIndex, value);
            AnyEntrySelected = SelectedProcessIndex >= 0;
        }
    }

    private string _appHeadingText;
    public string AppHeadingText
    {
        get => _appHeadingText;
        set => SetProperty(ref _appHeadingText, value);
    }

    private bool _anyEntrySelected;
    public bool AnyEntrySelected
    {
        get => _anyEntrySelected;
        set => SetProperty(ref _anyEntrySelected, value);
    }

    private bool _lackPermissions = false;
    public bool LackPermissions
    {
        get => _lackPermissions;
        set => SetProperty(ref _lackPermissions, value);
    }

    // Used to trigger list UI update
    private bool _listUpdates = false;
    public bool ListUpdates
    {
        get => _listUpdates;
        set => SetProperty(ref _listUpdates, value);
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
        var _processesList = new ObservableCollection<ProcessModel>();
        foreach (var process in ProcessManager.RunningProcesses())
        {
            var matchedSystemProcess = false;
            foreach (var i in ProcessManager.KnownWindowsSystemProcesses)
            {
                if (i == process.ProcessName) { matchedSystemProcess = true; break; }
            }
            if (matchedSystemProcess) { continue; }
            var windowText = ProcessManager.GetWindowTitleText(process);
            if (windowText == null)
            {
                continue;
            }
            var isSuspended = ProcessManager.HasSuspended(process);
            var filePath = ProcessManager.GetProcessLocation(process);
            if (filePath == null)
            {
                LackPermissions = true;
                continue;
            }
            Debug.WriteLine($"Program: {windowText}\nSuspended: {isSuspended}\n");
            _processesList.Add(new ProcessModel
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
        ProcessesList.Clear();
        foreach (var processEntry in _processesList)
        {
            ProcessesList.Add(processEntry);
        }
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

    public ICommand RefreshProcessesListCommand
    {
    get; private set; }
    private async Task<int> RefreshProcessesList()
    {
        await PopulateProcessesList();
        return 0;
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
        await PopulateProcessesList();
        Process.Start("explorer.exe", "/select, \"" + ProcessesList![SelectedProcessIndex].ExecutablePath + "\"");
    }

    public ICommand KillSelectedProcessCommand
    {
        get; private set;
    }
    private async void KillSelectedProcess()
    {
        if (SelectedProcessIndex <= -1)
        {
            return;
        }
        await PopulateProcessesList();
        ProcessManager.Terminate(ProcessesList![SelectedProcessIndex].Process!);
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
        RefreshProcessesListCommand = new AsyncRelayCommand(RefreshProcessesList);
        SuspendProcessCommand = new RelayCommand(SuspendProcess);
        ResumeProcessCommand = new RelayCommand(ResumeProcess);
        OpenInFileExplorerCommand = new RelayCommand(OpenInFileExplorer);
        KillSelectedProcessCommand = new RelayCommand(KillSelectedProcess);
        SelectedProcessIndex = 0;
        AnyEntrySelected = SelectedProcessIndex >= 0;
        InitializeProcessList();
    }
}
