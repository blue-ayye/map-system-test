using PrimeTween;
using System;
using UnityEngine;

namespace BP.MapSystem
{
    public interface IMapNodeView
    {
        event Action<MapNode> OnNodeClicked;
        event Action<NodeState> OnStateChanged;

        Transform Transform { get; }

        void Initialize(MapNode node);
        void SetState(NodeState state);
        Tween AnimateSpawn(float nodeSpawnDuration);
    }
}