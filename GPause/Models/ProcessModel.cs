using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GPause.Models;

public class ProcessModel : INotifyPropertyChanged
{
    private int _id;
    public int Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }
    }

    private string? _name;
    public string? Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    private string? _systemName;
    public string? SystemName
    {
        get => _systemName;
        set
        {
            if (_systemName != value)
            {
                _systemName = value;
                OnPropertyChanged(nameof(SystemName));
            }
        }
    }

    private bool? _suspended;
    public bool? Suspended
    {
        get => _suspended;
        set
        {
            if (_suspended != value)
            {
                _suspended = value;
                OnPropertyChanged(nameof(Suspended));
            }
        }
    }

    private string? _executablePath;
    public string? ExecutablePath
    {
        get => _executablePath;
        set
        {
            if (_executablePath != value)
            {
                _executablePath = value;
                OnPropertyChanged(nameof(ExecutablePath));
            }
        }
    }

    private Process? _process;
    public Process? Process
    {
        get => _process;
        set
        {
            if (_process != value)
            {
                _process = value;
                OnPropertyChanged(nameof(Process));
            }
        }
    }

    private BitmapImage? _appIcon;
    public BitmapImage? AppIcon
    {
        get => _appIcon;
        set
        {
            if (_appIcon != value)
            {
                _appIcon = value;
                OnPropertyChanged(nameof(AppIcon));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
