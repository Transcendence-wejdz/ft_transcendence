using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Makes every Web build match what the site serves: gzipped player files and no Burst debug folder.
public class WebBuildPostprocess : IPostprocessBuildWithReport
{
    private const string BurstDebugPattern = "*_BurstDebugInformation_DoNotShip";
    private const string GzipSuffix = ".gz";

    private static readonly string[] PlayerFileSuffixes = { ".data", ".framework.js", ".wasm" };

    public int callbackOrder => int.MaxValue;

    private static bool IsPlayerFile(string name)
    {
        foreach (string suffix in PlayerFileSuffixes)
        {
            if (name.EndsWith(suffix))
                return true;
        }
        return false;
    }

    // Replaces file by file.gz.
    private static void GzipFile(string file)
    {
        using (FileStream source = File.OpenRead(file))
        using (FileStream target = File.Create(file + GzipSuffix))
        using (GZipStream gzip = new GZipStream(target, CompressionLevel.Optimal))
            source.CopyTo(gzip);
        File.Delete(file);
    }

    private static void DeleteBurstDebug(string outputPath)
    {
        foreach (string dir in Directory.GetDirectories(outputPath, BurstDebugPattern))
            Directory.Delete(dir, true);
    }

    // Unity never compresses Development builds, so gzip them the way a release build would be.
    private static void GzipPlayerFiles(string outputPath)
    {
        string buildDir = Path.Combine(outputPath, "Build");
        string indexPath = Path.Combine(outputPath, "index.html");
        string index = null;
        string name;

        if (File.Exists(indexPath))
            index = File.ReadAllText(indexPath);
        foreach (string file in Directory.GetFiles(buildDir))
        {
            name = Path.GetFileName(file);
            if (!IsPlayerFile(name))
                continue;
            GzipFile(file);
            if (index != null)
                index = index.Replace("/" + name + "\"", "/" + name + GzipSuffix + "\"");
        }
        if (index != null)
            File.WriteAllText(indexPath, index);
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        string outputPath = report.summary.outputPath;

        if (report.summary.platform != BuildTarget.WebGL)
            return;
        DeleteBurstDebug(outputPath);
        if ((report.summary.options & BuildOptions.Development) != 0)
            GzipPlayerFiles(outputPath);
    }
}
