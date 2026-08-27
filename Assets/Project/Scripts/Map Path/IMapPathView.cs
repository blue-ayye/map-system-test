using PrimeTween;

namespace BP.MapSystem
{
    public interface IMapPathView
    {
        MapNode FromNode { get; }
        MapNode ToNode { get; }

        void SetupPath(MapNode fromNode, MapNode toNode);

        /// <summary> Animates the initial base line when the map is generated. </summary>
        Tween AnimateInitialDraw(float duration);

        /// <summary> Animates the colored traversal line layered on top. </summary>
        Tween AnimateTraversal(float duration);

        /// <summary> Snaps the traversal line to complete (used when loading a save file). </summary>
        void SetInstantlyTraversed();

        /// <summary> Snaps the traversal line back to zero (used when resetting the map). </summary>
        void ResetToDefault();
    }
}