using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    [Header("Level Goal (LEGACY - not used)")]
    public int targetTileType = 0;
    public int targetAmount = 5;
    public int collectedAmount = 0;

    [System.Serializable]
    public class GoalData
    {
        public int tileType;
        public int targetAmount;
        public int collectedAmount;
    }

    [Header("Multi Goals")]
    public List<GoalData> goals = new List<GoalData>();

    [Header("Moves")]
    public int moveLimit = 20;
    public int movesLeft;

    public Tile[,] tiles;

    public int width = 7;
    public int height = 7;

    public GameObject tilePrefab;
    public float tileSize = 70f;

    Tile selectedTile;
    List<Tile> matchedTiles = new List<Tile>();

    void Start()
    {
        movesLeft = moveLimit;
        GenerateBoard();
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
                GameObject tile = Instantiate(tilePrefab, transform);
                Tile tileScript = tile.GetComponent<Tile>();

                int randomType = Random.Range(0, 5);
                tileScript.SetType(randomType);

                if (Random.Range(0, 10) < 2)
                    tileScript.SetObstacle(2);

                tile.transform.localPosition = new Vector3(
                    x * tileSize - offsetX,
                    y * tileSize - offsetY,
                    0
                );

                tiles[x, y] = tileScript;
                tileScript.x = x;
                tileScript.y = y;
                tileScript.board = this;
            }
        }

        CheckMatches();
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
                        AddMatchedTile(current);
                        AddMatchedTile(t1);
                        AddMatchedTile(t2);
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
                        AddMatchedTile(current);
                        AddMatchedTile(t1);
                        AddMatchedTile(t2);
                    }
                }
            }
        }

        ClearMatches();
    }

    void AddMatchedTile(Tile tile)
    {
        if (!matchedTiles.Contains(tile))
            matchedTiles.Add(tile);
    }

    void ClearMatches()
    {
        if (matchedTiles.Count == 0) return;

        foreach (Tile tile in matchedTiles)
        {
            TryCollectTile(tile);
            DamageAdjacentObstacles(tile);

            tiles[tile.x, tile.y] = null;
            Destroy(tile.gameObject);
        }

        ApplyGravity();
        SpawnNewTiles();
        CheckMatches();
    }

    void TryCollectTile(Tile tile)
    {
        if (!tile.isCollectible) return;

        foreach (GoalData goal in goals)
        {
            if (goal.tileType == tile.tileType &&
                goal.collectedAmount < goal.targetAmount)
            {
                goal.collectedAmount++;
                Debug.Log($"Collected {goal.tileType}: {goal.collectedAmount}/{goal.targetAmount}");
                CheckLevelComplete();
                return;
            }
        }
    }

    void DamageAdjacentObstacles(Tile tile)
    {
        TryDamageObstacle(tile.x + 1, tile.y);
        TryDamageObstacle(tile.x - 1, tile.y);
        TryDamageObstacle(tile.x, tile.y + 1);
        TryDamageObstacle(tile.x, tile.y - 1);
    }

    void TryDamageObstacle(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        Tile tile = tiles[x, y];
        if (tile == null || !tile.isObstacle) return;

        tile.hitPoints--;
        tile.image.color = tile.hitPoints == 1 ? Color.white : Color.gray;

        if (tile.hitPoints <= 0)
        {
            tiles[x, y] = null;
            Destroy(tile.gameObject);
        }
    }

    void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] == null)
                {
                    for (int aboveY = y + 1; aboveY < height; aboveY++)
                    {
                        if (tiles[x, aboveY] != null && !tiles[x, aboveY].isObstacle)
                        {
                            tiles[x, y] = tiles[x, aboveY];
                            tiles[x, aboveY] = null;

                            tiles[x, y].y = y;
                            tiles[x, y].transform.localPosition +=
                                new Vector3(0, -(aboveY - y) * tileSize, 0);
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
                    GameObject tile = Instantiate(tilePrefab, transform);
                    Tile tileScript = tile.GetComponent<Tile>();

                    tileScript.SetType(Random.Range(0, 5));
                    tile.transform.localPosition = new Vector3(
                        x * tileSize - offsetX,
                        (height + 1) * tileSize - offsetY,
                        0
                    );

                    tiles[x, y] = tileScript;
                    tileScript.x = x;
                    tileScript.y = y;
                    tileScript.board = this;

                    tile.transform.localPosition = new Vector3(
                        x * tileSize - offsetX,
                        y * tileSize - offsetY,
                        0
                    );
                }
            }
        }
    }

    public void SelectTile(Tile tile)
    {
        if (selectedTile == null)
        {
            selectedTile = tile;
            return;
        }

        if (selectedTile == tile)
        {
            selectedTile = null;
            return;
        }

        if (IsAdjacent(selectedTile, tile))
            SwapTiles(selectedTile, tile);

        selectedTile = null;
    }

    bool IsAdjacent(Tile a, Tile b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
    }

    void SwapTiles(Tile a, Tile b)
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

    void CheckLevelComplete()
    {
        foreach (GoalData goal in goals)
        {
            if (goal.collectedAmount < goal.targetAmount)
                return;
        }

        Debug.Log("LEVEL COMPLETE - ORDER FULFILLED");
    }

    void CheckOutOfMoves()
    {
        if (movesLeft <= 0)
            Debug.Log("OUT OF MOVES - LEVEL FAILED");
    }
}
