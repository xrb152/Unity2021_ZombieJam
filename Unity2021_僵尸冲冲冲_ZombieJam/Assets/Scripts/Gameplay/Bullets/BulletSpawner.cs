using System.Collections.Generic;
using System.Linq;
using Gameplay.Enemies;
using UnityEngine;
using Utils;

namespace Gameplay.Bullets
{
    public class BulletSpawner : MonoBehaviour
    {
        [SerializeField] private Bullet _bullet;
        [SerializeField] private Transform _spawnPosition;

        private List<Bullet> _bullets = new();

        public void Shot(Transform target, float speed)
        {
            foreach (var bullet in _bullets.Where(bullet => !bullet.gameObject.activeSelf))
            {
                bullet.gameObject.SetActive(true);
                bullet.Shot(_spawnPosition, target, speed);
                return;
            }

            CreateBullet(target, speed);
        }

        private void CreateBullet(Transform target, float speed)
        {
            var newBullet = Instantiate(_bullet, transform);
            newBullet.Shot(_spawnPosition, target, speed);
            newBullet.Hit += OnHit;
            _bullets.Add(newBullet);
        }

        // 原 async void + await Task.Delay(250)：抖音 / WebGL 下续体永不恢复，
        // 子弹永远不会被回收，射两枪就没有弹药了。
        private void OnHit(Bullet bullet)
        {
            if (bullet == null || !bullet.gameObject.activeSelf) return;

            DelayedCall.After(250, () =>
            {
                if (bullet == null || !bullet.gameObject.activeSelf) return;

                bullet.transform.localPosition = Vector3.zero;
                bullet.gameObject.SetActive(false);
            });
        }
    }
}