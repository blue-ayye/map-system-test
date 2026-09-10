using PrimeTween;

namespace BP.MapSystem
{
    public interface IMapPathView
    {
        MapNode FromNode { get; }
        MapNode ToNode { get; }

        void SetupPath(MapNode fromNode, MapNode toNode);

        Tween AnimateInitialDraw(float duration);

        Tween AnimateTraversal(float duration);

        void SetInstantlyTraversed();

        void ResetToDefault();
    }
}