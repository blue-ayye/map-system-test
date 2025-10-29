namespace BP.MapSystem
{
    [System.Serializable]
    public class NodeTypeFloatValue
    {
        public MapNodeTypeSO NodeType;
        public float Value;
        public NodeTypeFloatValue(MapNodeTypeSO nodeType, float value)
        {
            NodeType = nodeType;
            Value = value;
        }
    }
}