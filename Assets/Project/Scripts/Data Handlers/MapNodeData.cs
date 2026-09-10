namespace BP.MapSystem
{
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