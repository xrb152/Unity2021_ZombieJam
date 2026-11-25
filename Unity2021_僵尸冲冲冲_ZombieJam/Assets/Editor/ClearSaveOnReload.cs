#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一次性清空存档工具（编辑器专用）。
/// 只要工程根目录存在 .workbuddy/clear-save-on-reload.flag，
/// 就会在域重载（脚本编译完成 / 进入 Play 模式）的瞬间，在 Unity 进程内清空 PlayerPrefs，
/// 这样既清了内存缓存，也写了磁盘（注册表），不会被编辑器退出时的 flush 又写回去。
/// 清空后自动删除标记文件，只生效一次。
/// </summary>
[InitializeOnLoad]
public static class ClearSaveOnReload
{
    private const string SavesKey = "Saves";
    private const string FlagRelativePath = ".workbuddy/clear-save-on-reload.flag";

    static ClearSaveOnReload()
    {
        string flagPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FlagRelativePath));
        if (!File.Exists(flagPath)) return;

        try
        {
            bool hadSave = PlayerPrefs.HasKey(SavesKey);
            PlayerPrefs.DeleteKey(SavesKey);
            PlayerPrefs.Save();

            File.Delete(flagPath);

            Debug.Log(hadSave
                ? "[存档工具] 已在域重载时清空存档（PlayerPrefs.DeleteKey(\"Saves\") + Save），下次 Play 从全新存档开始：CurrentTutorialState = 0 (Swipe)，TutorialNumberStep = 0。"
                : "[存档工具] 未找到 Saves 键，本就是全新存档状态；标记已清除。");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[存档工具] 清空存档失败：" + e);
        }
    }

    [MenuItem("Tools/僵尸冲冲冲/仅清空游戏存档（保留其它 PlayerPrefs）", false, 1)]
    public static void ClearSaveOnly()
    {
        bool hadSave = PlayerPrefs.HasKey(SavesKey);
        PlayerPrefs.DeleteKey(SavesKey);
        PlayerPrefs.Save();
        Debug.Log(hadSave
            ? "[存档工具] 已清空 Saves，重启 Play 会重新走引导流程。"
            : "[存档工具] 当前没有 Saves，无需清空。");
    }

    [MenuItem("Tools/僵尸冲冲冲/打印当前存档 JSON", false, 2)]
    public static void PrintSave()
    {
        if (!PlayerPrefs.HasKey(SavesKey))
        {
            Debug.Log("[存档工具] 当前没有存档（全新状态）。");
            return;
        }

        Debug.Log("[存档工具] 当前存档：\n" + PlayerPrefs.GetString(SavesKey));
    }
}
#endif
