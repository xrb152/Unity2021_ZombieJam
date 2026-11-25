using System;
using DG.Tweening;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 替代 await Task.Delay 的延迟调用。
    ///
    /// 抖音 / WebGL 运行时是单线程的，没有线程池去跑 Timer，
    /// Task.Delay 的续体永远不会恢复 —— 后面的代码一行都不会执行。
    /// 项目里所有「延迟一下再继续」都必须走 DOTween 或协程，不能用 Task.Delay。
    ///
    /// DOTween 的延迟由 DontDestroyOnLoad 的 DOTweenComponent 驱动，
    /// 场景切换后依然有效，且默认忽略 timeScale（关卡加载时 timeScale 常常是 0）。
    /// </summary>
    public static class DelayedCall
    {
        /// <param name="milliseconds">延迟毫秒数，和原来 Task.Delay 的参数一致</param>
        /// <param name="action">延迟后执行</param>
        public static void After(float milliseconds, Action action)
        {
            if (action == null) return;

            if (milliseconds <= 0f)
            {
                action();
                return;
            }

            DOVirtual.DelayedCall(milliseconds / 1000f, () =>
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[延迟调用] 回调异常：{exception}");
                }
            }, true);
        }
    }
}
