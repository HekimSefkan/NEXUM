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
    private const string ApkName = "NEXUM_polish7.apk";

    public static void BuildAndroidApk()
    {
        if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);
        string path = Path.Combine(OutputDir, ApkName).Replace('\\', '/');

        Debug.Log(string.Format("{0}: scripting={1} mimari={2} il2cppConfig={3} stripping={4} appBundle={5} development={6}",
            Tag,
            PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android),
            PlayerSettings.Android.targetArchitectures,
            PlayerSettings.GetIl2CppCompilerConfiguration(BuildTargetGroup.Android),
            PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android),
            EditorUserBuildSettings.buildAppBundle,
            EditorUserBuildSettings.development));

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Debug.Log(string.Format("{0}: {1} sahne -> {2}", Tag, scenes.Length, path));

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

        Debug.Log(string.Format("{0}: sonuc={1} sure={2:F1} sn hata={3} uyari={4}",
            Tag, summary.result, summary.totalTime.TotalSeconds, summary.totalErrors, summary.totalWarnings));

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
            Debug.Log(Tag + ": FAIL");
            EditorApplication.Exit(1);
            return;
        }

        long apkBytes = File.Exists(path) ? new FileInfo(path).Length : (long)summary.totalSize;
        Debug.Log(string.Format("{0}_APK: {1:F2} MB ({2} bayt)", Tag, apkBytes / 1048576f, apkBytes));

        ReportAssets(report);
        Debug.Log(Tag + ": OK");
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
