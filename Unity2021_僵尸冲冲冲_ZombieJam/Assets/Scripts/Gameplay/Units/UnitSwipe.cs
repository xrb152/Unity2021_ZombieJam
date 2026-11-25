using System;
using System.Collections.Generic;
using Gameplay.Enums;
using Infrastructure.Input;
using UnityEngine;
using Utils;

namespace Gameplay.Units
{
    public class UnitSwipe : MonoBehaviour, ISwipeObject
    {
        [SerializeField] private Unit _unit;
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private Material _material;
        [SerializeField] private List<SkinnedMeshRenderer> _skinnedMeshRenderers;
        [SerializeField] private List<MeshRenderer> _meshRenderers;

        public ESwipeDirection SwipeDirection { get; set; }

        public void Swipe(ESwipeSide swipe)
        {
            _unit.Swipe(swipe);
        }

        public void SetSwipeObject()
        {
            SwitchMaterial(_outlineMaterial);
        }

        // 原 async void + await Task.Delay(250)：WebGL 下续体不恢复，
        // 选中高亮材质会一直挂着不还原。
        public void ResetSwipeObject()
        {
            DelayedCall.After(250, () =>
            {
                if (this == null) return;

                SwitchMaterial(_material);
            });
        }

        private void SwitchMaterial(Material material)
        {
            if (_skinnedMeshRenderers.Count > 0)
            {
                foreach (var mesh in _skinnedMeshRenderers)
                {
                    mesh.material = material;
                }
            }
            else
            {
                foreach (var mesh in _meshRenderers)
                {
                    mesh.material = material;
                }
            }
        }
    }
}