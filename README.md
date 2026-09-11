# BP Map System 🗺️

BP Map System is a highly customizable, procedural map generator inspired by the node-based progression of games like Slay the Spire. It handles everything from mathematical grid generation and pathing logic to rule-based node assignment, traversal state management, and data persistence.

The architecture strictly separates map logic from visual representation, allowing the exact same underlying system to drive both 2D UI (RectTransforms) and 3D World (Transforms/LineRenderers) maps out of the box.

🎮 Play the Web Demo on Itch.io: [Web Demo](https://nodevilsallowed.itch.io/sts-map-generation)

## 📸 Media

| 2D UI Map Example | 3D World Map Example |
| --- | --- |
| (Link to GIF/Image) | (Link to GIF/Image) |

## 🚀 Features
- Customizable Grid: Define exact horizontal rows (levels) and nodes per level.
- Organic Layouts: Apply dynamic X/Y spatial "jitter" so the map feels hand-drawn rather than rigidly mathematical.
- Intelligent Pathing: Guarantees a minimum number of unique starting paths and prevents overlapping/crossing over other paths.
- Procedural Rule Engine: Use ScriptableObjects to define probabilistic weights for node types (Combats, Shops, Bosses) while enforcing sibling constraints (no identical forks) and consecutive reductions (preventing 3 shops in a row).
- Traversal Management: Tracks player movement, restricts invalid moves, and natively supports saving/loading local progress via JSON.
- UI & 3D Support: Implements IMapNodeView and IMapPathView interfaces so you can skin the logic however your game requires.

## 📦 Dependencies
This project relies on two free, highly performant plugins:
1. PrimeTween: Used heavily for optimized, zero-allocation animations (node spawning and path reveals).
2. SerializedCollections (AYellowpaper): Used to expose standard C# Dictionaries in the Unity Inspector for configuring Node Type rules and weights.

## 🧠 How It Works (Step-by-Step)
The generation of a map follows a strict, logical pipeline orchestrated by the MapSystemManager.

### 1. Mathematical Grid Generation (MapNodeGenerator)
First, the system determines the boundaries of the map container. It then generates a raw 2D array of generic MapNode objects based on your configured _maxLevels and _nodesPerLevel. During this phase, mathematical "jitter" is applied to each node's position to slightly offset it from a perfect grid, creating an organic look. A guaranteed Initial Node (bottom) and Final Node (boss/top) are also instantiated.

### 2. Path Generation & Connection (MapPathGenerator)
With the points laid out, the path generator takes over:
- Starting Nodes: It randomly selects guaranteed unique starting points for the bottom level, then fulfills the remaining quota of _totalPaths.
- Traversing Upward: It evaluates valid children on the next level up. To prevent visual mess, it enforces a CanOverlapPath rule—paths are not allowed to cross each other (e.g., node 1 going to node 3 while node 2 goes to node 0).
- Culling: Any node that wasn't connected to a path is destroyed and removed from the grid to keep the map clean.

### 3. Procedural Rule Assignment (MapNodeTypeAssigner)
Nodes are assigned their gameplay identities (e.g., Shop, Elite, Rest) based on a list of NodeTypeRulesSO:
- Exclusivity: Rules can be set to ignore constraints (useful for forcing a Treasure node on exactly level 5).
- Consecutive Constraints: The system dynamically lowers the probability weight of a node type if the parent or child node shares that type (e.g., lowering the chance of getting back-to-back Elites).
- Sibling Constraints: Checks horizontal forks to optionally prevent a player from being presented with the illusion of choice (e.g., a path splitting into two identical Shop nodes).

### 4. Validation & Fallback
If custom seeds are disabled, the system attempts to generate the map multiple times (_generationAttempts). It checks for any procedural rule violations (like inescapable consecutive constraints). If it fails to find a perfect map, it falls back to the seed that had the fewest violations.

### 5. Visual Instantiation & Animation
Once the data graph is perfectly valid, the system iterates through the generic map data and spawns the physical prefabs (IMapNodeView and IMapPathView). It chains these spawns using PrimeTween to create a satisfying, cascading map reveal animation.

### 6. Traversal & State Management (MapTraversalController)
When a player clicks a node, the controller validates the move:
- Is it a direct child of the current node?
- Is _canTraverseVisitedNodes enabled, and are they clicking backward? If valid, the system updates the NodeState (Locked, Reachable, Current, Visited) which automatically updates the visual colors and icons of the node prefabs.

## 🛠️ Core Components Breakdown
| Script | Responsibility |
| ------ | -------------- |
| MapSystemManager | The central hub. Calls generation, tracks seeds, triggers animations, and handles Save/Load operations. |
| MapNodeGenerator | Handles grid bounds, dimensions, physical spacing, organic jitter, and populates the raw data array. |
| MapPathGenerator | Generates logical graph edges (connections) between nodes and prevents paths from crossing. |
| MapNodeTypeAssigner | Ingests NodeTypeRulesSO to probabilistically assign identities (Combat, Event, Shop) while enforcing constraints. |
| MapTraversalController | Validates player input, updates node states, and animates the player's path traversal. |
| MapDataHandler | Serializes the seed and traversal history to JSON, saving it to Application.persistentDataPath. |


