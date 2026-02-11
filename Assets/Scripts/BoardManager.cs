using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Tile Visuals")]
    public Sprite[] tileSprites;

    [System.Serializable]
    public class GoalData
    {
        public Sprite goalSprite;     
        public int targetAmount;
        public int collectedAmount;
        public int matchColorType;
    }


    [Header("Multi Goals")]
    public List<GoalData> goals = new List<GoalData>();

    [Header("Level 1 Special Items")]
    public Sprite[] specialItemSprites;  
    public int specialItemCount = 3;

    [Header("Moves")]
    public int moveLimit = 20;
    public int movesLeft;

    public int width = 7;
    public int height = 7;

    public GameObject tilePrefab;
    public float tileSize = 70f;

    public Tile[,] tiles;

    List<Tile> matchedTiles = new List<Tile>();

    void Start()
    {
        movesLeft = moveLimit;
        GenerateBoard();
        PlaceSpecialItems();   
    }

    void GenerateBoard()
    {
        float offsetX = (width - 1) * tileSize / 2f;
        float offsetY = (height - 1) * tileSize / 2f;

        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject obj = Instantiate(tilePrefab, transform);
                Tile tile = obj.GetComponent<Tile>();

                int randomType;
                do
                {
                    randomType = Random.Range(0, 5);
                }
                while (CreatesMatchAt(x, y, randomType));

                tile.SetType(randomType);

                obj.transform.localPosition = new Vector3(
                    x * tileSize - offsetX,
                    y * tileSize - offsetY,
                    0
                );

                tiles[x, y] = tile;
                tile.x = x;
                tile.y = y;
                tile.board = this;
            }
        }
    }


    void PlaceSpecialItems()
    {
        for (int i = 0; i < goals.Count; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            Tile tile = tiles[x, y];

            
            tile.image.sprite = goals[i].goalSprite;

        
            tile.tileType = goals[i].matchColorType;

            tile.isSpecialItem = true;
            tile.isCollectible = true;

            tile.SetIce(2);
        }
    }



    bool CreatesMatchAt(int x, int y, int type)
    {
        if (x >= 2)
        {
            if (tiles[x - 1, y] != null &&
                tiles[x - 2, y] != null &&
                tiles[x - 1, y].tileType == type &&
                tiles[x - 2, y].tileType == type)
                return true;
        }

        if (y >= 2)
        {
            if (tiles[x, y - 1] != null &&
                tiles[x, y - 2] != null &&
                tiles[x, y - 1].tileType == type &&
                tiles[x, y - 2].tileType == type)
                return true;
        }

        return false;
    }

    void CheckMatches()
    {
        matchedTiles.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile current = tiles[x, y];
                if (current == null || current.isObstacle) continue;

                if (x <= width - 3)
                {
                    Tile t1 = tiles[x + 1, y];
                    Tile t2 = tiles[x + 2, y];

                    if (t1 && t2 &&
                        !t1.isObstacle && !t2.isObstacle &&
                        t1.tileType == current.tileType &&
                        t2.tileType == current.tileType)
                    {
                        AddMatched(current, t1, t2);
                    }
                }

                if (y <= height - 3)
                {
                    Tile t1 = tiles[x, y + 1];
                    Tile t2 = tiles[x, y + 2];

                    if (t1 && t2 &&
                        !t1.isObstacle && !t2.isObstacle &&
                        t1.tileType == current.tileType &&
                        t2.tileType == current.tileType)
                    {
                        AddMatched(current, t1, t2);
                    }
                }
            }
        }

        ClearMatches();
    }

    void AddMatched(params Tile[] group)
    {
        foreach (Tile t in group)
            if (!matchedTiles.Contains(t))
                matchedTiles.Add(t);
    }

    void ClearMatches()
    {
        if (matchedTiles.Count == 0) return;

        bool anyRealDestroyed = false;

        foreach (Tile tile in matchedTiles)
        {
            if (tile == null) continue;

            if (tile.hasIce)
            {
                tile.iceHitPoints--;

                if (tile.iceHitPoints <= 0)
                {
                    tile.hasIce = false;
                    tile.iceOverlay.gameObject.SetActive(false);
                }

                continue;
            }

            anyRealDestroyed = true;

            TryCollectTile(tile);
            tiles[tile.x, tile.y] = null;
            Destroy(tile.gameObject);
        }

        if (!anyRealDestroyed)
            return;

        ApplyGravity();
        SpawnNewTiles();
        CheckMatches();
    }

    void TryCollectTile(Tile tile)
    {
        if (!tile.isSpecialItem) return;

        foreach (GoalData goal in goals)
        {
            if (goal.collectedAmount < goal.targetAmount)
            {
                goal.collectedAmount++;
                CheckLevelComplete();
                return;
            }
        }
    }


    public void SwapTiles(Tile a, Tile b)
    {
        movesLeft--;
        CheckOutOfMoves();

        tiles[a.x, a.y] = b;
        tiles[b.x, b.y] = a;

        (a.x, b.x) = (b.x, a.x);
        (a.y, b.y) = (b.y, a.y);

        Vector3 pos = a.transform.localPosition;
        a.transform.localPosition = b.transform.localPosition;
        b.transform.localPosition = pos;

        CheckMatches();
    }

    void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] == null)
                {
                    for (int above = y + 1; above < height; above++)
                    {
                        if (tiles[x, above] != null && !tiles[x, above].isObstacle)
                        {
                            tiles[x, y] = tiles[x, above];
                            tiles[x, above] = null;

                            tiles[x, y].y = y;
                            tiles[x, y].transform.localPosition -=
                                new Vector3(0, (above - y) * tileSize, 0);

                            break;
                        }
                    }
                }
            }
        }
    }

    void SpawnNewTiles()
    {
        float offsetX = (width - 1) * tileSize / 2f;
        float offsetY = (height - 1) * tileSize / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] == null)
                {
                    GameObject obj = Instantiate(tilePrefab, transform);
                    Tile tile = obj.GetComponent<Tile>();

                    tile.SetType(Random.Range(0, 5));

                    obj.transform.localPosition = new Vector3(
                        x * tileSize - offsetX,
                        y * tileSize - offsetY,
                        0
                    );

                    tiles[x, y] = tile;
                    tile.x = x;
                    tile.y = y;
                    tile.board = this;
                }
            }
        }
    }

    void CheckLevelComplete()
    {
        foreach (GoalData goal in goals)
            if (goal.collectedAmount < goal.targetAmount)
                return;

        Debug.Log("LEVEL COMPLETE");
    }

    void CheckOutOfMoves()
    {
        if (movesLeft <= 0)
            Debug.Log("OUT OF MOVES");
    }
}
