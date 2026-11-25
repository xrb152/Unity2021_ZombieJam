using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using _Project.Scripts.Infrastructure.Bootstrap;

namespace _Project.Scripts.EditorTools
{
    /// <summary>
    /// 一次性生成 Logo 展示场景（Assets/Scenes/Logo.unity），并把它放到 Build Settings 第 0 位。
    /// 场景由代码生成而不是手写 YAML，避免文件 GUID / 组件引用出错。
    /// </summary>
    public static class LogoSceneBuilder
    {
        private const string LogoScenePath = "Assets/Scenes/Logo.unity";
        private const string LogoTexturePath = "Assets/AdSDK/Resources/logo.png";
        private const string NextSceneName = "Bootstrap";

        private static string LogoSceneFullPath => Path.Combine(Application.dataPath, "Scenes/Logo.unity");

        private static readonly Color BackgroundColor = new Color(0.082f, 0.047f, 0.141f, 1f);

        [MenuItem("Tools/僵尸冲冲冲/重建 Logo 展示场景", false, 200)]
        private static void RebuildFromMenu()
        {
            Build(true);
        }

        [MenuItem("Tools/僵尸冲冲冲/把 Logo 场景设为启动场景", false, 201)]
        private static void SetAsFirstSceneFromMenu()
        {
            EnsureLogoIsFirstScene();
            Debug.Log("[Logo] 已把 Logo 场景放到 Build Settings 第 0 位。");
        }

        /// <summary>
        /// 场景不存在时自动生成一次（幂等，不会覆盖已有场景）。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void AutoBuildOnce()
        {
            if (File.Exists(LogoSceneFullPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (File.Exists(LogoSceneFullPath))
                {
                    return;
                }

                Build(false);
            };
        }

        private static void Build(bool overwrite)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Logo] 编辑器正处于 Play 模式，退出 Play 后再生成 Logo 场景。");
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                return;
            }

            if (!overwrite && File.Exists(LogoSceneFullPath))
            {
                return;
            }

            var logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoTexturePath);

            if (logoSprite == null)
            {
                Debug.LogError($"[Logo] 找不到 Sprite：{LogoTexturePath}，请确认该图片的 Texture Type 为 Sprite (2D and UI)。");
                return;
            }

            // 用 Additive 新建，避免打断用户当前打开的场景
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            try
            {
                // ---------- 相机 ----------
                var cameraGo = new GameObject("Main Camera");
                MoveToScene(cameraGo, scene);
                cameraGo.tag = "MainCamera";

                var camera = cameraGo.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = BackgroundColor;
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                cameraGo.AddComponent<AudioListener>();

                // ---------- Canvas ----------
                var canvasGo = new GameObject("LogoCanvas", typeof(RectTransform));
                MoveToScene(canvasGo, scene);

                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = false;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                var canvasGroup = canvasGo.AddComponent<CanvasGroup>();

                // ---------- 背景 ----------
                var backgroundGo = new GameObject("Background", typeof(RectTransform));
                backgroundGo.transform.SetParent(canvasGo.transform, false);

                var background = backgroundGo.AddComponent<Image>();
                background.color = BackgroundColor;
                background.raycastTarget = false;

                Stretch(backgroundGo.GetComponent<RectTransform>());

                // ---------- Logo ----------
                var logoGo = new GameObject("Logo", typeof(RectTransform));
                logoGo.transform.SetParent(canvasGo.transform, false);

                var logoImage = logoGo.AddComponent<Image>();
                logoImage.sprite = logoSprite;
                logoImage.preserveAspect = true;
                logoImage.raycastTarget = false;

                var logoRect = logoGo.GetComponent<RectTransform>();
                logoRect.anchorMin = new Vector2(0.5f, 0.5f);
                logoRect.anchorMax = new Vector2(0.5f, 0.5f);
                logoRect.pivot = new Vector2(0.5f, 0.5f);
                logoRect.anchoredPosition = Vector2.zero;
                logoRect.sizeDelta = new Vector2(980f, 980f);

                // ---------- 控制器 ----------
                var launcher = canvasGo.AddComponent<LogoSceneLauncher>();

                var serialized = new SerializedObject(launcher);
                SetRef(serialized, "_canvasGroup", canvasGroup);
                SetRef(serialized, "_logoRect", logoRect);
                SetRef(serialized, "_logoImage", logoImage);

                var nextSceneProperty = serialized.FindProperty("_nextSceneName");

                if (nextSceneProperty != null)
                {
                    nextSceneProperty.stringValue = NextSceneName;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);

                string directory = Path.GetDirectoryName(LogoScenePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!EditorSceneManager.SaveScene(scene, LogoScenePath))
                {
                    Debug.LogError($"[Logo] 保存场景失败：{LogoScenePath}");
                    return;
                }
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            AssetDatabase.Refresh();

            EnsureLogoIsFirstScene();

            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            var order = new System.Text.StringBuilder();

            for (int i = 0; i < scenes.Length; i++)
            {
                order.Append($"\n  [{i}] {(scenes[i].enabled ? "启用" : "禁用")} {scenes[i].path}");
            }

            Debug.Log($"[Logo] Logo 展示场景已生成：{LogoScenePath}\n已放到 Build Settings 第 0 位（展示后进入 {NextSceneName}）。当前场景顺序：{order}");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (!File.Exists(LogoSceneFullPath))
            {
                Build(false);
            }
        }

        /// <summary>
        /// 把 Logo 场景插到 Build Settings 最前面，其余场景保持原有顺序与启用状态。
        /// </summary>
        private static void EnsureLogoIsFirstScene()
        {
            if (!File.Exists(LogoSceneFullPath))
            {
                return;
            }

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            var result = new List<EditorBuildSettingsScene>(existing.Length + 1)
            {
                new EditorBuildSettingsScene(LogoScenePath, true)
            };

            foreach (EditorBuildSettingsScene scene in existing)
            {
                if (scene.path == LogoScenePath)
                {
                    continue;
                }

                result.Add(scene);
            }

            EditorBuildSettings.scenes = result.ToArray();
        }

        private static void MoveToScene(GameObject gameObject, Scene scene)
        {
            if (gameObject.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(gameObject, scene);
            }
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetRef(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);

            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
