using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using GPause.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Windows.UI.Input.Preview.Injection;

namespace GPause.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        DataContext = new MainViewModel();
        InitializeComponent();
        //RefreshProcessesEntries();
        //ViewModel.ProcessesList += RefreshProcessesEntries();
    }

    private enum AlertActions
    {
        OpenAsAdmin,
        GeneralAlert
    }

    private static async Task<AsyncVoidMethodBuilder> ShowAlert(UIElement xamlRoot, string? Title = null, string? MainButtonText = null, string? SecondButtonText = null, string? DismissButtonText = null, string? ContentText = null, AlertActions? action = AlertActions.GeneralAlert)
    {
        ContentDialog dialog = new();
        dialog.XamlRoot = xamlRoot.XamlRoot;
        dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
        if (Title != null) { dialog.Title = Title; }
        if (MainButtonText != null) { dialog.PrimaryButtonText = MainButtonText; }
        if (SecondButtonText != null) { dialog.SecondaryButtonText = SecondButtonText; }
        if (DismissButtonText != null) { dialog.CloseButtonText = DismissButtonText; }
        dialog.DefaultButton = ContentDialogButton.Primary;
        if (ContentText != null) { dialog.Content = ContentText; }


        if (action == AlertActions.OpenAsAdmin)
        {
            // Bind primary button action to re-open the app as admin.
            dialog.PrimaryButtonClick += (sender, args) => AdminRelauncher();
        }

        await dialog.ShowAsync();

        var e = new AsyncVoidMethodBuilder();
        return e;
    }

    private async void CheckForAdmin()
    {
        var identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        if (!isAdmin)
        {
            await ShowAlert(xamlRoot: this, Title: "Some apps can't be paused or resumed.", ContentText: "Re-open this app as administrator to gain access to the rest of the running apps.", MainButtonText: "Open as administrator", DismissButtonText: "Ignore", action: AlertActions.OpenAsAdmin);
        }
    }

    private static void AdminRelauncher()
    {
        Debug.WriteLine(Assembly.GetExecutingAssembly().Location.Replace(Assembly.GetExecutingAssembly().Location.Replace(AppDomain.CurrentDomain.BaseDirectory + "\\", string.Empty), Assembly.GetExecutingAssembly().Location.Replace(AppDomain.CurrentDomain.BaseDirectory + "\\", string.Empty).Replace("dll", "exe")));
        var proc = new ProcessStartInfo
        {
            UseShellExecute = true,
            WorkingDirectory = Environment.CurrentDirectory,
            FileName = Assembly.GetExecutingAssembly().Location.Replace(Assembly.GetExecutingAssembly().Location.Replace(AppDomain.CurrentDomain.BaseDirectory + "\\", string.Empty), Assembly.GetExecutingAssembly().Location.Replace(AppDomain.CurrentDomain.BaseDirectory + "\\", string.Empty).Replace("dll", "exe")),
            Verb = "runas"
        };
        Process.Start(proc);
        Environment.Exit(0);
    }

    private void RefreshProcessesEntiresButton_Click(object? sender = null, Microsoft.UI.Xaml.RoutedEventArgs? e = null)
    {
        //RefreshProcessesEntries();
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
/*
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
*/
    private async void ShellMenuBarSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        await ShowAlert(xamlRoot: this, Title: "Settings is yet to be implemented.", ContentText: "Look out for future updates!", DismissButtonText:"Okay");
    }

    private void RunAsAdminEventHandler_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        CheckForAdmin();
    }

    private void RunAsAdminEventHandler_Click(object sender, RoutedEventArgs e)
    {
        AdminRelauncher();
    }
}
