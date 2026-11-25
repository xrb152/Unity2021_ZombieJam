using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Effects
{
    public class SetColorEffect : MonoBehaviour
    {
        [SerializeField] private Color _color;
        [SerializeField] private List<ParticleSystem> _particles;

        private void Start()
        {
            foreach (var particle in _particles)
            {
                if (particle == null) continue;

                // ParticleSystem.startColor 已废弃，改用 main.startColor（MainModule 是结构体，
                // 内部持有 native 引用，所以拿局部副本赋值同样会生效）
                var main = particle.main;
                main.startColor = _color;
            }
        }
    }
}