using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using Color = UnityEngine.Color;

public class GridManager : MonoBehaviour
{
    private static GridManager _instance;
    public static GridManager Instance { get { return _instance; } }
    
    public enum FOWEnum
    {
        NeverSeen,
        CurrentSeeing,
        PrevSeen
    }
    
    public class TileInfo
    {
        //Line of Sight
        public bool visible;
        public FOWEnum sightValue;

    }
    
    // Stores our traversable tiles
    [SerializeField]
    private Tilemap traversable;

    // Stores our non-traversable tiles
    [SerializeField]
    private Tilemap notTraversable;
    //Store our Fog of War tiles
    [SerializeField]
    private Tilemap fowTiles;
    
    private SortedSet<DijkstrasNodeInfo> sortedSet;
    public Dictionary<Vector2Int, DijkstrasNodeInfo> PlayerDijkstra;
    //Player Data
   public GameObject player; 
   public Dictionary<DijkstrasNodeInfo, DijkstrasNodeInfo> playerRange;
   public Dictionary<Vector2Int, TileInfo> fowVision;
   
   //Tile Color Data
   public Dictionary<Vector2Int, Color> fowTintedTiles;
   public bool displayFOWTiles;
   
   
   
   private void Awake()
   {
       if (_instance != null && _instance != this)
       {
           fowTiles.CompressBounds();

           Destroy(this.gameObject);
       }
       else
       {
           _instance = this;
           fowTiles.CompressBounds();
       }

       fowTintedTiles = new Dictionary<Vector2Int, Color>();
       fowVision = new Dictionary<Vector2Int, TileInfo>();
       player = GameObject.FindWithTag("Player");
       sortedSet = new SortedSet<DijkstrasNodeInfo>();
       playerRange = new Dictionary<DijkstrasNodeInfo, DijkstrasNodeInfo>();
       if(displayFOWTiles)
       {
           fowTiles.GetComponent<TilemapRenderer>().enabled = true;
       }
       else if(!displayFOWTiles)
       {
           fowTiles.GetComponent<TilemapRenderer>().enabled = false;
       }
       CreateGrid();
   }
   
   public void AddFOWTile(Vector2Int pos, Color tint)
   {
       if (!fowTintedTiles.ContainsKey(pos) && (displayFOWTiles))
       {
           fowTintedTiles.Add(pos, tint);
       }
       else
       {
           Debug.Log(pos + "already exists in Dictonary");
       }
   }
   
   public Vector2Int GetCellPosition(Vector3 worldPos)
   {
       Vector3Int pos3 = traversable.WorldToCell(worldPos);
       Vector2Int pos = new(pos3.x, pos3.y);
       return pos;
   }
   
   
     // utility class for running dijkstras
    public class DijkstrasNodeInfo : IComparable<DijkstrasNodeInfo>
    {
        // which position on the map does this correspond to
        public Vector2Int position;

        // parent node
        public DijkstrasNodeInfo parent;

        // distance from origin in moves
        public int rawDist;

        public int CompareTo(DijkstrasNodeInfo other)
        {
            int dist = rawDist - other.rawDist;
            if (dist == 0)
            {
                dist = position.x - other.position.x;
                if (dist == 0)
                {
                    return position.y - other.position.y;
                }
                else
                {
                    return dist;
                }
            }
            else
            {
                return dist;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;

            if (!(obj is DijkstrasNodeInfo))
                return false;

            DijkstrasNodeInfo info = (DijkstrasNodeInfo)obj;
            // compare elements here
            return info.position == this.position;
        }

        public override int GetHashCode()
        {
            return (int)position.GetHashCode();
        }

        public List<DijkstrasNodeInfo> NeighborsToNodeInfos(List<Vector2Int> neighbors, DijkstrasNodeInfo parent)
        {
            List<DijkstrasNodeInfo> nodeInfos = new List<DijkstrasNodeInfo>();

            foreach (Vector2Int neighbor in neighbors)
            {
                DijkstrasNodeInfo current = new DijkstrasNodeInfo();
                current.position = neighbor;
                current.rawDist = rawDist + 1;
                current.parent = parent;
                nodeInfos.Add(current);
            }

            return nodeInfos;
        }

    }
    
    public void Dijkstras(ref Dictionary<DijkstrasNodeInfo, DijkstrasNodeInfo> searched, ref SortedSet<DijkstrasNodeInfo> toSearch, Vector2Int startingSquare, int range)
    {
        toSearch.Clear();
        searched.Clear();
        DijkstrasNodeInfo start = new DijkstrasNodeInfo();
        start.position = startingSquare;
        start.rawDist = 0;
        start.parent = null;
        toSearch.Add(start);

        while (toSearch.Count > 0)
        {
            DijkstrasNodeInfo current = toSearch.Min;
            int currentDist = current.rawDist;
            toSearch.Remove(current);
            searched.Add(current, current.parent);

            List<DijkstrasNodeInfo> neighbors = current.NeighborsToNodeInfos(GetNeighbors(current.position), current);

            foreach (DijkstrasNodeInfo neighbor in neighbors)
            {

                // if the node isnt on the map, ignore it
                if (!fowVision.ContainsKey(neighbor.position))
                {

                    continue;
                }
                // if already in searched list, dont add
                if (searched.ContainsKey(neighbor))
                {
                    continue;
                }

                bool inSearch = toSearch.Contains(neighbor);

                if (!inSearch)
                {
                    toSearch.Add(neighbor);
                }

            }

        }
    }
    private void CreateGrid()
    {
        for (int x = fowTiles.cellBounds.xMin - 20; x < fowTiles.cellBounds.xMax + 20; x++)
        {
            for (int y = fowTiles.cellBounds.yMin - 20; y < fowTiles.cellBounds.yMax + 20; y++)
            {
                Vector3 worldPosition = fowTiles.CellToWorld(new Vector3Int(x, y, 0));
                fowVision.Add(new Vector2Int(x, y), new TileInfo());
            }
        }

    }
    
    private List<Vector2Int> GetNeighbors(Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        if (fowVision.ContainsKey(new Vector2Int(position.x - 1, position.y)))
        {
            neighbors.Add(new Vector2Int(position.x - 1, position.y));
        }

        if (fowVision.ContainsKey(new Vector2Int(position.x + 1, position.y)))
        {
            neighbors.Add(new Vector2Int(position.x + 1, position.y));
        }

        if (fowVision.ContainsKey(new Vector2Int(position.x, position.y - 1)))
        {
            neighbors.Add(new Vector2Int(position.x, position.y - 1));
        }

        if (fowVision.ContainsKey(new Vector2Int(position.x, position.y + 1)))
        {
            neighbors.Add(new Vector2Int(position.x, position.y + 1));
        }

        return neighbors;

    }
    public Vector2 GetTileCenter(Vector2Int gridPos)
    {
        TileInfo tile;
        bool exists = fowVision.TryGetValue(gridPos, out tile);
        Vector3Int posn = new Vector3Int(gridPos.x, gridPos.y, 0);

        if (!exists)
        {
            throw new ArgumentException("tile does not exist on grid");
        }

        return (Vector2)fowTiles.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
    }
    
    /*
    * @brief Converts the current grid map to a sorted set for Dijkstra
    *
       private Dictionary<Vector2Int, TileInfo> map;
    */
    public SortedSet<DijkstrasNodeInfo> MapToSortedSet()
    {
        sortedSet.Clear();
        DijkstrasNodeInfo currentNode;

        foreach (KeyValuePair<Vector2Int, TileInfo> tile in fowVision)
        {
            currentNode = new DijkstrasNodeInfo();
            currentNode.position = tile.Key;
            currentNode.parent = null;
            currentNode.rawDist = 1;
            sortedSet.Add(currentNode);
        }
        return sortedSet;
    }
    public void PlayerDijkstras()
    {
        Debug.Log("Running PlayerDijkstras");
        fowTintedTiles.Clear();
        //fowVision.Clear();
        playerRange.Clear();
        SortedSet<DijkstrasNodeInfo> toSearch;
        toSearch = MapToSortedSet();
        Vector2 playerVector2 = new Vector2(player.transform.position.x, player.transform.position.y);
        Vector2Int playerTransform = Vector2Int.RoundToInt(playerVector2);
        Dijkstras(ref playerRange, ref toSearch, playerTransform, -1);
        if (PlayerDijkstra == null)
        {
            PlayerDijkstra = new Dictionary<Vector2Int, DijkstrasNodeInfo>();
        }
        else
        {
            PlayerDijkstra.Clear();
        }
        foreach (var entry in playerRange)
        {
            if (entry.Key != null)
            {
                Debug.Log(entry.Key);
                PlayerDijkstra.Add(entry.Key.position, entry.Key);
                ChangeFOWValue(entry.Key);
            }
        }
        if (displayFOWTiles)
            ColorFOWTiles();
    }
    
    public void AddFOWDebugTile(Vector2Int pos, Color tint)
    {
        if (!fowTintedTiles.ContainsKey(pos) && (displayFOWTiles))
        {
            fowTintedTiles.Add(pos, tint);
        }
        else
        {
            Debug.Log(pos + "already exists in Dictonary");
        }
    }
    void ChangeFOWValue(DijkstrasNodeInfo node)
    {
        TileInfo tile;
        bool exists = fowVision.TryGetValue(node.position, out tile);
        Debug.Log("Tile sight value" + tile.sightValue);
        int visionRange = player.GetComponent<PlayerMovement>().visionDistance;
        if (!exists)
        {
            //throw new ArgumentException("tile does not exist on grid");
        }
        
            if (node.rawDist <= visionRange)
            {
                tile.sightValue = FOWEnum.CurrentSeeing;
            }
        
        else if (node.rawDist > visionRange)
        {
            if (tile.sightValue == FOWEnum.CurrentSeeing)
            {
                tile.sightValue = FOWEnum.PrevSeen;
            }
        }

    }
    void ColorFOWTiles()
    {
        TileInfo currentTile;
        Color tileTint = Color.white;
        int visionRange = player.GetComponent<PlayerMovement>().visionDistance;
        //ConvertPlayerRangetoFOWVision();

        if (displayFOWTiles)
        {
            foreach (var entry in fowVision)
            {
                currentTile = entry.Value;
                if (currentTile.sightValue == FOWEnum.NeverSeen)
                {
                    tileTint = new Color(0, 0, 0, 1);
                }
                else if (currentTile.sightValue == FOWEnum.PrevSeen)
                {
                    tileTint = new Color(0, 0, 0, .25f);
                }
                else if (currentTile.sightValue == FOWEnum.CurrentSeeing)
                {
                    tileTint = new Color(0, 0, 0, 0);
                }
                //Debug.Log("Key Value: " + map.FirstOrDefault(x => x.Value == currentTile).Key);
                AddFOWDebugTile(fowVision.FirstOrDefault(x => x.Value == currentTile).Key, tileTint);
            }
            TintFOWTiles();
        }
    }
    public void TintTile(Vector2Int gridPos, Color color, Tilemap _tileMap)
    {
        TileInfo tile;
        bool exists = fowVision.TryGetValue(gridPos, out tile);
        Vector3Int posn = new Vector3Int(gridPos.x, gridPos.y, 0);

        if (!exists)
        {
            throw new ArgumentException("tile does not exist on grid");
        }
        else
        {
            _tileMap.SetTileFlags(posn, TileFlags.None);
            _tileMap.SetColor(posn, color);
            Debug.Log(color);
        }

    }
    private void TintFOWTiles()
    {
        Debug.Log("FOW Debug Tiles Count: " + fowTintedTiles.Count());
        //TintDebugTiles(fowTintedTiles);
        if (fowTintedTiles.Count() >= 1)
        {
            foreach (var tile in fowTintedTiles)
            {
                if (fowTintedTiles.ContainsKey(tile.Key))
                {
                    //TintTile(tile.Key, fowTintedTiles[tile.Key]);
                    TintTile(tile.Key, fowTintedTiles[tile.Key], fowTiles);
                }
                else
                {
                    TintTile(tile.Key, Color.white, fowTiles);
                }
            }
        }
        else
        {
            return;
        }
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(displayFOWTiles)
        {
            fowTiles.GetComponent<TilemapRenderer>().enabled = true;
        }
        else if(!displayFOWTiles)
        {
            fowTiles.GetComponent<TilemapRenderer>().enabled = false;
        }
    }
}
