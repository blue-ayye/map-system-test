using UnityEngine;

namespace BP.MapSystem
{
    public interface IMapNodeView
    {
        void SetNodeType(MapNodeTypeSO nodeType);

        Vector3 Position { get; }
    }
}