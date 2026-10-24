using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Makes every Web build match what the site serves: gzipped player files and no Burst debug folder.
public class WebBuildPostprocess : IPostprocessBuildWithReport
{
    private static readonly string[] PlayerFileSuffixes = { ".data", ".framework.js", ".wasm" };

    public int callbackOrder => int.MaxValue;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;
        string outputPath = report.summary.outputPath;
        foreach (string dir in Directory.GetDirectories(outputPath, "*_BurstDebugInformation_DoNotShip"))
            Directory.Delete(dir, true);
        if ((report.summary.options & BuildOptions.Development) != 0)
            GzipPlayerFiles(outputPath);
    }

    // Unity never compresses Development builds, so gzip them the way a release build would be.
    private static void GzipPlayerFiles(string outputPath)
    {
        string buildDir = Path.Combine(outputPath, "Build");
        string indexPath = Path.Combine(outputPath, "index.html");
        string index = File.Exists(indexPath) ? File.ReadAllText(indexPath) : null;
        foreach (string file in Directory.GetFiles(buildDir))
        {
            string name = Path.GetFileName(file);
            if (!System.Array.Exists(PlayerFileSuffixes, name.EndsWith))
                continue;
            using (FileStream source = File.OpenRead(file))
            using (FileStream target = File.Create(file + ".gz"))
            using (GZipStream gzip = new GZipStream(target, CompressionLevel.Optimal))
                source.CopyTo(gzip);
            File.Delete(file);
            if (index != null)
                index = index.Replace("/" + name + "\"", "/" + name + ".gz\"");
        }
        if (index != null)
            File.WriteAllText(indexPath, index);
    }
}
