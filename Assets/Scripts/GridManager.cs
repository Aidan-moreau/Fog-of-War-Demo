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
    
    //Player Data
   public GameObject player; 
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
       if(displayFOWTiles)
       {
           fowTiles.GetComponent<TilemapRenderer>().enabled = true;
       }
       else if(!displayFOWTiles)
       {
           fowTiles.GetComponent<TilemapRenderer>().enabled = false;
       }
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
                fowVision.Add(new Vector2Int(x, y));
            }
        }

    }
    
    private List<Vector2Int> GetNeighbors(Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        if (map.ContainsKey(new Vector2Int(position.x - 1, position.y)))
        {
            neighbors.Add(new Vector2Int(position.x - 1, position.y));
        }

        if (map.ContainsKey(new Vector2Int(position.x + 1, position.y)))
        {
            neighbors.Add(new Vector2Int(position.x + 1, position.y));
        }

        if (map.ContainsKey(new Vector2Int(position.x, position.y - 1)))
        {
            neighbors.Add(new Vector2Int(position.x, position.y - 1));
        }

        if (map.ContainsKey(new Vector2Int(position.x, position.y + 1)))
        {
            neighbors.Add(new Vector2Int(position.x, position.y + 1));
        }

        return neighbors;

    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
