namespace BP.MapSystem
{
    /// <summary>
    /// Represents the data structure for storing information about a specific map node, including its level and index within the map.
    /// </summary>
    [System.Serializable]
    public class MapNodeData
    {
        public int Level;
        public int Index;

        public MapNodeData(MapNode node)
        {
            Level = node.Level;
            Index = node.Index;
        }
    }
}