using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Infrastructure.Bootstrap
{
    /// <summary>
    /// 兜底脚本：在 Gameplay / Region 等子场景里单独 Play（没有 GameBootstrapper）时，
    /// 自动回到 Bootstrap 场景重新走完整启动流程。
    /// 注意：必须按场景名加载，不能写 LoadScene(0) —— Build Settings 第 0 位现在是 Logo 展示场景。
    /// </summary>
    public class GameRunner : MonoBehaviour
    {
        private const string BootstrapSceneName = "Bootstrap";

        private void Awake()
        {
            var bootstrapper = FindObjectOfType<global::Infrastructure.Bootstrap.GameBootstrapper>();

            if (bootstrapper != null)
            {
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(BootstrapSceneName))
            {
                Debug.Log($"[GameRunner] 未找到 GameBootstrapper，回到 {BootstrapSceneName} 重新启动");
                SceneManager.LoadScene(BootstrapSceneName);
                return;
            }

            Debug.LogError($"[GameRunner] 未找到 GameBootstrapper，且场景 {BootstrapSceneName} 不在 Build Settings 中");
        }
    }
}
