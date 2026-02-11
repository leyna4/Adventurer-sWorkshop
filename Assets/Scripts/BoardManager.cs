using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class BoardManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI movesText;

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
        UpdateMovesUI();
        GenerateBoard();
        PlaceSpecialItems();
    }

    void UpdateMovesUI()
    {
        if (movesText != null)
            movesText.text = "Moves: " + movesLeft;
    }

    Vector3 GetWorldPosition(int x, int y)
    {
        float offsetX = (width - 1) * tileSize / 2f;
        float offsetY = (height - 1) * tileSize / 2f;

        return new Vector3(
            x * tileSize - offsetX,
            y * tileSize - offsetY,
            0
        );
    }

    void GenerateBoard()
    {
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
                obj.transform.localPosition = GetWorldPosition(x, y);

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

    public void SwapTiles(Tile a, Tile b, bool isReverting = false)
    {
        tiles[a.x, a.y] = b;
        tiles[b.x, b.y] = a;

        int tempX = a.x;
        int tempY = a.y;

        a.x = b.x;
        a.y = b.y;

        b.x = tempX;
        b.y = tempY;

        a.transform.localPosition = GetWorldPosition(a.x, a.y);
        b.transform.localPosition = GetWorldPosition(b.x, b.y);

        if (!isReverting)
        {
            if (HasMatch())
            {
                movesLeft--;
                UpdateMovesUI();
                CheckOutOfMoves();
                CheckMatches();
            }
            else
            {
                StartCoroutine(RevertSwap(a, b));
            }
        }
    }

    IEnumerator RevertSwap(Tile a, Tile b)
    {
        yield return new WaitForSeconds(0.2f);
        SwapTiles(a, b, true);
    }

    bool HasMatch()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (CheckMatchAt(x, y))
                    return true;

        return false;
    }

    bool CheckMatchAt(int x, int y)
    {
        Tile tile = tiles[x, y];
        if (tile == null) return false;

        int matchType = tile.tileType;

        int horizontal = 1;

        if (x > 0 && tiles[x - 1, y] != null && tiles[x - 1, y].tileType == matchType)
            horizontal++;
        if (x < width - 1 && tiles[x + 1, y] != null && tiles[x + 1, y].tileType == matchType)
            horizontal++;

        if (horizontal >= 3)
            return true;

        int vertical = 1;

        if (y > 0 && tiles[x, y - 1] != null && tiles[x, y - 1].tileType == matchType)
            vertical++;
        if (y < height - 1 && tiles[x, y + 1] != null && tiles[x, y + 1].tileType == matchType)
            vertical++;

        if (vertical >= 3)
            return true;

        return false;
    }

    void CheckMatches()
    {
        matchedTiles.Clear();

       
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                Tile a = tiles[x, y];
                Tile b = tiles[x + 1, y];
                Tile c = tiles[x + 2, y];

                if (a != null && b != null && c != null)
                {
                    if (a.tileType == b.tileType &&
                        b.tileType == c.tileType)
                    {
                        if (!matchedTiles.Contains(a)) matchedTiles.Add(a);
                        if (!matchedTiles.Contains(b)) matchedTiles.Add(b);
                        if (!matchedTiles.Contains(c)) matchedTiles.Add(c);
                    }
                }
            }
        }

        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                Tile a = tiles[x, y];
                Tile b = tiles[x, y + 1];
                Tile c = tiles[x, y + 2];

                if (a != null && b != null && c != null)
                {
                    if (a.tileType == b.tileType &&
                        b.tileType == c.tileType)
                    {
                        if (!matchedTiles.Contains(a)) matchedTiles.Add(a);
                        if (!matchedTiles.Contains(b)) matchedTiles.Add(b);
                        if (!matchedTiles.Contains(c)) matchedTiles.Add(c);
                    }
                }
            }
        }

        ClearMatches();
    }


    void DamageAdjacentSpecialTiles(Tile matchedTile)
    {
        Vector2Int[] directions =
        {
        new Vector2Int(1,0),
        new Vector2Int(-1,0),
        new Vector2Int(0,1),
        new Vector2Int(0,-1)
    };

        foreach (var dir in directions)
        {
            int nx = matchedTile.x + dir.x;
            int ny = matchedTile.y + dir.y;

            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                Tile neighbor = tiles[nx, ny];

                if (neighbor != null &&
                    neighbor.isSpecialItem &&
                    neighbor.hasIce &&
                    neighbor.tileType == matchedTile.tileType)
                {
                    neighbor.iceHitPoints--;

                    if (neighbor.iceHitPoints <= 0)
                    {
                        neighbor.hasIce = false;
                        neighbor.iceOverlay.gameObject.SetActive(false);
                    }
                }
            }
        }
    }


    void ClearMatches()
    {
        if (matchedTiles.Count == 0) return;

        
        foreach (Tile tile in matchedTiles)
        {
            if (tile != null)
                DamageAdjacentSpecialTiles(tile);
        }

        foreach (Tile tile in matchedTiles)
        {
            if (tile == null) continue;

            
            if (tile.isSpecialItem)
            {
                
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
                else
                {
                    
                    TryCollectTile(tile);

                    tiles[tile.x, tile.y] = null;
                    Destroy(tile.gameObject);
                    continue;
                }
            }

            
            tiles[tile.x, tile.y] = null;
            Destroy(tile.gameObject);
        }

        ApplyGravity();
        SpawnNewTiles();
        CheckMatches();
    }


    void TryCollectTile(Tile tile)
    {
        if (!tile.isSpecialItem) return;

        foreach (GoalData goal in goals)
        {
            if (goal.matchColorType == tile.tileType &&
                goal.collectedAmount < goal.targetAmount)
            {
                goal.collectedAmount++;
                CheckLevelComplete();
                return;
            }
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
                    for (int above = y + 1; above < height; above++)
                    {
                        if (tiles[x, above] != null)
                        {
                            tiles[x, y] = tiles[x, above];
                            tiles[x, above] = null;

                            tiles[x, y].y = y;
                            tiles[x, y].transform.localPosition = GetWorldPosition(x, y);
                            break;
                        }
                    }
                }
            }
        }
    }

    void SpawnNewTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] == null)
                {
                    GameObject obj = Instantiate(tilePrefab, transform);
                    Tile tile = obj.GetComponent<Tile>();

                    tile.SetType(Random.Range(0, 5));
                    obj.transform.localPosition = GetWorldPosition(x, y);

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

        FindObjectOfType<GameFlowManager>().OnLevelCompleted();
    }

    void CheckOutOfMoves()
    {
        if (movesLeft <= 0)
        {
            FindObjectOfType<GameFlowManager>().OnOutOfMoves();
        }
    }
}
