using Gameplay.Enums;
using Gameplay.Units;
using UnityEngine;

namespace Infrastructure.Input
{
    public struct TutorialSwipeInfo
    {
        public ESwipeDirection SwipeDirection;
        public ESwipeSide SwipeSide;
        public GameObject SwipeGameObject;
        public UnitSwipe UnitSwipe;

        public void Reset()
        {
            SwipeDirection = ESwipeDirection.None;
            SwipeSide = ESwipeSide.None;   // 原来这行写成了 SwipeDirection，导致上一笔的方向残留
            SwipeGameObject = null;
            UnitSwipe = null;
        }
    }
}