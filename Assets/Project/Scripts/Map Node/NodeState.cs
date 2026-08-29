namespace BP.MapSystem
{
    public enum NodeState
    {
        Locked,     // Cannot be clicked
        Reachable,  // Next valid move
        Current,    // Where the player is right now
        Visited     // Previously traversed
    }
}