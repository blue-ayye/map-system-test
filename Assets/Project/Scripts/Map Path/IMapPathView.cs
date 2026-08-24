using PrimeTween;
using UnityEngine;

namespace BP.MapSystem
{
    public interface IMapPathView
    {
        MapNode FromNode { get; }
        MapNode ToNode { get; }
        void DrawPath(MapNode fromNode, MapNode toNode, Color? pathColor = null);

        /// <summary>
        /// You can use this to change material, image, sprite color, etc. depending on implementation.
        /// </summary>
        /// <param name="newColor"></param>
        void ChangePathColor(Color newColor);
        Tween AnimateDraw(float duration);
    }
}