using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPause.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using PMan;
using Windows.Storage.FileProperties;

namespace GPause.ViewModels;

public class MainViewModel : ObservableRecipient, INotifyPropertyChanged
{
    private int indexOfProcessToOperate = -1;

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
            Debug.WriteLine($"Selected index: {SelectedProcessIndex}");
        }
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

    private int _listPopulateProgress;
    public int ListPopulateProgress
    {
        get => _listPopulateProgress;
        set => SetProperty(ref _listPopulateProgress, value);
    }

    private int _listProcessesCount;
    public int ListProcessesCount
    {
        get => _listProcessesCount;
        set => SetProperty(ref _listProcessesCount, value);
    }

    private bool _isLoading = false;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Populate the processes list with entries (Window title, process name, process ID, app icon and suspend/resume status).
    /// </summary>
    private async Task<int> PopulateProcessesList()
    {
        IsLoading = true;
        ProcessesList!.Clear();
        ListPopulateProgress = 0;
        Debug.WriteLine("Populating Processes List...");
        var runningProcesses = ProcessManager.RunningProcesses();
        ListProcessesCount = runningProcesses.Length;
        foreach (var process in runningProcesses)
        {
            ListPopulateProgress += 1;
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

            // if program complains about not being able to fetch icon fast enough first time,
            // just fetch again, then it works because it already tried fetching before so the
            // second time the fetching is fast enough to catch up with the UI.
            // very very dumb way to do this but works flawlessly (somehow).
            var appIcon = await GetAppIcon(filePath) ?? await GetAppIcon(filePath);

            Debug.WriteLine($"Program: {windowText}\nSuspended: {isSuspended}\n");
            ProcessesList.Add(new ProcessModel
            {
                Name = windowText,
                SystemName = process.ProcessName,
                Id = process.Id,
                Suspended = isSuspended,
                Process = process,
                ExecutablePath = filePath,
                AppIcon = appIcon
            });
        }
        IsLoading = false;
        return 0;
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
        indexOfProcessToOperate = SelectedProcessIndex;
        await PopulateProcessesList();
        var targetProcess = ProcessesList![indexOfProcessToOperate].Process!;
        Debug.WriteLine($"Minimizing window of process (Index {indexOfProcessToOperate})");
        ProcessManager.MinimizeWindow(targetProcess);
        Debug.WriteLine($"Suspending process (Index {indexOfProcessToOperate})");
        ProcessManager.Suspend(targetProcess);
        Debug.WriteLine("Re-populating list (suspended)");
        await PopulateProcessesList();
    }

    public ICommand RefreshProcessesListCommand
    {
        get; private set;
    }
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
        indexOfProcessToOperate = SelectedProcessIndex;
        Debug.WriteLine($"Resuming process (Index {indexOfProcessToOperate})");
        await PopulateProcessesList();
        var targetProcess = ProcessesList![indexOfProcessToOperate].Process!;
        ProcessManager.Resume(targetProcess);
        Debug.WriteLine("Re-populating list (resumed)");
        await PopulateProcessesList();
        Debug.WriteLine($"Restoring window of process (Index {indexOfProcessToOperate})");
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
        indexOfProcessToOperate = SelectedProcessIndex;
        await PopulateProcessesList();
        Process.Start("explorer.exe", "/select, \"" + ProcessesList![indexOfProcessToOperate].ExecutablePath + "\"");
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
        indexOfProcessToOperate = SelectedProcessIndex;
        await PopulateProcessesList();
        Debug.WriteLine(indexOfProcessToOperate);
        ProcessManager.Terminate(ProcessesList![indexOfProcessToOperate].Process!);
        await PopulateProcessesList();
    }

    private async void InitializeProcessList()
    {
        await PopulateProcessesList();
    }

    private static async Task<BitmapImage?> GetAppIcon(string processPath)
    {
        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(processPath);
        StorageItemThumbnail iconThumbnail;
        try
        {
            iconThumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, 32);
        }
        catch
        {
            return null;
        }
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
        SelectedProcessIndex = -1;
        AnyEntrySelected = SelectedProcessIndex >= 0;
        InitializeProcessList();
    }
}
