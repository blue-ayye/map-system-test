using UnityEngine;

namespace BP.MapSystem
{
    public interface IMapNodeView
    {
        void Initialize(MapNode node);

        Transform Transform { get; }
    }
}