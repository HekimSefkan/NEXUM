using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// NEXUM Android build + BuildReport ölçümü.
// Batch: Unity.exe -batchmode -nographics -quit -projectPath <proje>
//                  -executeMethod NexumBuild.BuildAndroidApk -logFile <log>
//
// Player ayarlarını DEĞİŞTİRMEZ; yalnızca mevcut ayarlarla build alır ve raporlar.
// (IL2CPP / hedef mimari / paket adı Editor'de kullanıcı tarafından ayarlanır.)
public static class NexumBuild
{
    private const string Tag = "NEXUM_BUILD";
    private const string OutputDir = "Builds";
    private const string ApkName = "NEXUM_release_prep.apk";
    private const string AabName = "NEXUM_release_prep.aab";

    // Hem APK hem AAB üretir. Yayın paketi AAB'dir; APK ölçüm ve cihaza kurulum içindir.
    public static void BuildAndroidApk()
    {
        LogSettings();

        bool okApk = BuildOne(ApkName, false);
        bool okAab = BuildOne(AabName, true);

        Debug.Log(Tag + (okApk && okAab ? ": OK" : ": FAIL"));
        if (!(okApk && okAab)) EditorApplication.Exit(1);
    }

    private static void LogSettings()
    {
        // Enum değerleri sayı olarak da yazılır; ProjectSettings'teki ham değerle karşılaştırılabilsin
        Debug.Log(string.Format(
            "{0}_SETTINGS: scripting={1} mimari={2} ({3}) targetSdk={4} ({5}) minSdk={6} ({7}) il2cppConfig={8} stripping={9} development={10}",
            Tag,
            PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android),
            PlayerSettings.Android.targetArchitectures, (int)PlayerSettings.Android.targetArchitectures,
            PlayerSettings.Android.targetSdkVersion, (int)PlayerSettings.Android.targetSdkVersion,
            PlayerSettings.Android.minSdkVersion, (int)PlayerSettings.Android.minSdkVersion,
            PlayerSettings.GetIl2CppCompilerConfiguration(BuildTargetGroup.Android),
            PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android),
            EditorUserBuildSettings.development));
    }

    private static bool BuildOne(string fileName, bool appBundle)
    {
        if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);
        string path = Path.Combine(OutputDir, fileName).Replace('\\', '/');

        EditorUserBuildSettings.buildAppBundle = appBundle;

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Debug.Log(string.Format("{0}: {1} sahne -> {2} (appBundle={3})", Tag, scenes.Length, path, appBundle));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,   // Development Build kapalı
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log(string.Format("{0}: {1} sonuc={2} sure={3:F1} sn hata={4} uyari={5}",
            Tag, fileName, summary.result, summary.totalTime.TotalSeconds, summary.totalErrors, summary.totalWarnings));

        if (summary.result != BuildResult.Succeeded)
        {
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage msg in step.messages)
                {
                    if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        Debug.Log(string.Format("{0}_ERROR: {1}", Tag, msg.content.Replace("\n", " ")));
                }
            }
            return false;
        }

        long bytes = File.Exists(path) ? new FileInfo(path).Length : (long)summary.totalSize;
        Debug.Log(string.Format("{0}_PACKAGE: {1} {2:F2} MiB ({3} bayt)", Tag, fileName, bytes / 1048576f, bytes));

        if (!appBundle) ReportAssets(report);   // asset dökümü bir kez yeter
        return true;
    }

    private static void ReportAssets(BuildReport report)
    {
        var all = new List<KeyValuePair<string, ulong>>();
        foreach (PackedAssets packed in report.packedAssets)
        {
            foreach (PackedAssetInfo info in packed.contents)
            {
                string source = string.IsNullOrEmpty(info.sourceAssetPath) ? "<built-in>" : info.sourceAssetPath;
                all.Add(new KeyValuePair<string, ulong>(source, info.packedSize));
            }
        }

        // Kategori dağılımı
        var categories = new Dictionary<string, ulong>();
        foreach (var item in all)
        {
            string cat = Categorize(item.Key);
            categories.TryGetValue(cat, out ulong sum);
            categories[cat] = sum + item.Value;
        }

        ulong total = 0;
        foreach (var c in categories) total += c.Value;
        Debug.Log(string.Format("{0}_PACKED_TOTAL: {1:F2} MB", Tag, total / 1048576f));

        foreach (var c in categories.OrderByDescending(c => c.Value))
        {
            Debug.Log(string.Format("{0}_CATEGORY: {1,-12} {2,8:F2} MB  %{3:F1}",
                Tag, c.Key, c.Value / 1048576f, total == 0 ? 0f : c.Value * 100f / total));
        }

        // Aynı asset birden fazla parçaya bölünebildiği için yol bazında toplanır
        var byAsset = all.GroupBy(a => a.Key)
                         .Select(g => new KeyValuePair<string, ulong>(g.Key, (ulong)g.Sum(x => (decimal)x.Value)))
                         .OrderByDescending(a => a.Value)
                         .Take(20);
        int rank = 1;
        foreach (var a in byAsset)
        {
            Debug.Log(string.Format("{0}_TOP{1:D2}: {2,8:F2} MB  {3}", Tag, rank++, a.Value / 1048576f, a.Key));
        }
    }

    private static string Categorize(string path)
    {
        // "<built-in>" gibi dosya olmayan yollar Path.GetExtension'ı patlatır
        int dot = path.LastIndexOf('.');
        int slash = path.LastIndexOf('/');
        string ext = (dot > slash && dot >= 0) ? path.Substring(dot).ToLowerInvariant() : string.Empty;
        switch (ext)
        {
            case ".png": case ".jpg": case ".jpeg": case ".psd": case ".tga":
            case ".spriteatlasv2": case ".spriteatlas":
                return "Doku";
            case ".mp3": case ".wav": case ".ogg": case ".aif":
                return "Ses";
            case ".ttf": case ".otf":
                return "Font";
            case ".unity":
                return "Sahne";
            case ".asset":
                return "Asset";
            case ".prefab":
                return "Prefab";
            case ".cs": case ".dll":
                return "Kod";
            case ".shader": case ".shadergraph": case ".mat":
                return "Shader";
            default:
                return string.IsNullOrEmpty(ext) ? "Diger" : ext;
        }
    }
}
