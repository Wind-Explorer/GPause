using System.Diagnostics;
using System.Reflection;
using Octokit;

namespace GPause.Helpers;
public static class GitHubInteraction
{
    public static Task<IReadOnlyList<Release>> GetReleases()
    {
        // Initiate new client connection
        var client = new GitHubClient(new ProductHeaderValue("GPause-Update-Module"));

        // Fetch latest release from GitHub
        var releases = client.Repository.Release.GetAll("Wind-Explorer", "GPause");

        return releases;
    }

    public static bool? CheckForUpdates()
    {
        // Fetch latest release from GitHub
        var releases = GetReleases();

        // Validate that latest release info is retrieved successfully
        if (releases.IsFaulted)
        {
            Console.WriteLine("Failed to retrieve latest release info!");
            return null;
        }

        // Build current version string
        var packageVersion = Windows.ApplicationModel.Package.Current.Id.Version;
        var appVersion = string.Format("{0}.{1}.{2}",
                    packageVersion.Major,
                    packageVersion.Minor,
                    packageVersion.Build);

        // Compare two versions for difference
        try
        {
            return appVersion != releases.Result[0].TagName;
        }
        catch
        {
            Console.WriteLine("Failed to compare app version with latest version!");
            return null;
        }
    }

    public static async void DownloadUpdate()
    {
        // Fetch latest release from GitHub
        var releases = GetReleases();

        // Get download link out of release
        var downloadLink = releases.Result[0].Assets[0].BrowserDownloadUrl;

        // Initiate download
        using var client = new HttpClient();
        using var streamData = await client.GetStreamAsync(downloadLink);

        // Get download location
        var dir = Path.Join(Path.GetTempPath(), $"gpause-{releases.Result[0].TagName}-installer.exe");

        // Write downloaded data to system's temp folder
        using var fileStream = new FileStream(dir, System.IO.FileMode.OpenOrCreate);
        await streamData.CopyToAsync(fileStream);


        // Initialize installer process information
        var proc = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = dir
        };

        // Start installer
        Process.Start(proc);

        // Close this instance of app
        App.Current.Exit();
    }
}
