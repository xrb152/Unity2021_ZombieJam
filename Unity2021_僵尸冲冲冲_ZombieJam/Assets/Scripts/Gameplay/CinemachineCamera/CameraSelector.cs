using UnityEngine;

namespace Gameplay.CinemachineCamera
{
    public class CameraSelector : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        public void ChangeCamera(ECameraType cameraType)
        {
            if (_animator == null)
            {
                _animator = gameObject.GetComponent<Animator>();
            }

            if (_animator == null)
            {
                Debug.LogError($"[相机] {name} 上找不到 Animator，镜头切不到 {cameraType}");
                return;
            }

            // 打包后 Animator 上没有对应 Trigger 时 SetTrigger 只会刷一条静默警告，
            // 镜头纹丝不动，很难查。这里把「有没有这个参数」打出来。
            var hasParameter = false;
            try
            {
                foreach (var parameter in _animator.parameters)
                {
                    if (parameter.name == cameraType.ToString() &&
                        parameter.type == AnimatorControllerParameterType.Trigger)
                    {
                        hasParameter = true;
                        break;
                    }
                }

                Debug.Log($"[相机] ChangeCamera({cameraType}) animator={_animator.name} " +
                          $"触发器存在={hasParameter} 激活={_animator.isActiveAndEnabled} " +
                          $"过渡中={_animator.IsInTransition(0)}");

                if (!hasParameter)
                {
                    Debug.LogError($"[相机] Animator 上没有名为 {cameraType} 的 Trigger，镜头不会切换");
                    return;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[相机] 读取 Animator 参数失败：{exception.Message}");
            }

            _animator.SetTrigger(cameraType.ToString());
        }
    }
}