using UnityEngine;

namespace BP.MapSystem
{
    public interface IMapPathView
    {
        MapNode FromNode { get; }
        MapNode ToNode { get; }
        void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null);
        void ChangePathColor(Color newColor);
    }
}