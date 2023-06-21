using System.Diagnostics;
using GPause.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Input.Preview.Injection;

namespace GPause.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }
    /*
        private static bool RAA()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }*/

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        DataContext = new MainViewModel();
        InitializeComponent();
        RefreshProcessesEntries();
        /*        if (RAA())
                {
                    Environment.Exit(0);
                }*/
    }

    private async void RefreshProcessesEntries()
    {
        StartLoading();
        await Task.Delay(2000);

        ProcessesListBox.ItemsSource = null;
        Debug.WriteLine("Nulled list");
        ProcessesListBox.ItemsSource = ViewModel.ProcessesList;
        Debug.WriteLine("Binded list");
        StopLoading();
    }

    private void RefreshProcessesEntiresButton_Click(object? sender = null, Microsoft.UI.Xaml.RoutedEventArgs? e = null)
    {
        RefreshProcessesEntries();
    }

    private void StartLoading()
    {
        LoadingArea.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        MainArea.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void StopLoading()
    {
        LoadingArea.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        MainArea.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    }

    private static void CtrlR()
    {
        // Create an instance of the InputInjector
        var inputInjector = InputInjector.TryCreate();

        // Simulate pressing the Ctrl key
        inputInjector.InjectKeyboardInput(new[] { new InjectedInputKeyboardInfo
        {
            VirtualKey = (ushort)Windows.System.VirtualKey.Control,
            KeyOptions = InjectedInputKeyOptions.None,
        }});

        // Simulate pressing the R key
        inputInjector.InjectKeyboardInput(new[] { new InjectedInputKeyboardInfo
        {
            VirtualKey = (ushort)Windows.System.VirtualKey.R,
            KeyOptions = InjectedInputKeyOptions.None,
        }});

        // Simulate releasing the Ctrl key
        inputInjector.InjectKeyboardInput(new[] { new InjectedInputKeyboardInfo
        {
            VirtualKey = (ushort)Windows.System.VirtualKey.Control,
            KeyOptions = InjectedInputKeyOptions.KeyUp,
        }});
    }

    private void PauseProcessButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Pause button clicked!");
        CtrlR();
        Debug.WriteLine("Refreshed list (paused)");
    }

    private void ResumeProcessButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("Resume button clicked!");
        CtrlR();
        Debug.WriteLine("Refreshed list (resumed)");
    }
}
