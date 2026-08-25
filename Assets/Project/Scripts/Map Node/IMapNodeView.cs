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
        void SetState(NodeState state);
        Tween AnimateSpawn(float nodeSpawnDuration);
    }
}