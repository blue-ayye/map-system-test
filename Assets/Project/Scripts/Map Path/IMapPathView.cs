using UnityEngine;

namespace BP.MapSystem
{
    public interface IMapPathView
    {
        void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null);
        void ChangePathColor(Color newColor);
    }
}