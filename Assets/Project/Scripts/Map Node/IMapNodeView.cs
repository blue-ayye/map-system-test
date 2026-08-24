using PrimeTween;
using System;
using UnityEngine;

namespace BP.MapSystem
{
    public interface IMapNodeView
    {
        event Action<MapNode> OnNodeClicked;
        Transform Transform { get; }

        void Initialize(MapNode node);
        void SetActiveVisitedState(bool state);
        void SetActiveSelectedState(bool state);
        Tween AnimateSpawn(float nodeSpawnDuration);
    }
}