using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

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

    public List<GoalData> goals = new List<GoalData>();

    public int moveLimit = 20;
    public int movesLeft;

    public int width = 7;
    public int height = 7;

    public GameObject tilePrefab;
    public float tileSize = 70f;

    public Tile[,] tiles;

    List<Tile> matchedTiles = new List<Tile>();

    [HideInInspector] public int currentLevel = 1;

    bool isProcessingMatches = false;

    void Start()
    {
        movesLeft = moveLimit;
        UpdateMovesUI();
        GenerateBoard();
        PlaceSpecialItems();
    }

    // =========================
    // LEVEL SETUP (IMPORTANT)
    // =========================

    public void SetGoals(List<GameFlowManager.LevelGoal> levelGoals, int moveLimitFromLevel)
    {
        goals.Clear();

        foreach (var lg in levelGoals)
        {
            GoalData newGoal = new GoalData();
            newGoal.goalSprite = lg.goalSprite;
            newGoal.targetAmount = lg.targetAmount;
            newGoal.collectedAmount = 0;
            newGoal.matchColorType = lg.matchColorID;

            goals.Add(newGoal);
        }

        moveLimit = moveLimitFromLevel;
        movesLeft = moveLimit;

        UpdateMovesUI();
    }

    public void ResetBoardForNextLevel()
    {
        if (tiles != null)
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (tiles[x, y] != null)
                        Destroy(tiles[x, y].gameObject);
        }

        foreach (GoalData goal in goals)
            goal.collectedAmount = 0;

        movesLeft = moveLimit;
        UpdateMovesUI();

        GenerateBoard();
        PlaceSpecialItems();
    }

    // =========================
    // UI
    // =========================

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

    // =========================
    // BOARD GENERATION
    // =========================

    void GenerateBoard()
    {
        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject obj = Instantiate(tilePrefab, transform);
                Tile tile = obj.GetComponent<Tile>();

                tile.tileSprites = tileSprites;
                tile.SetType(Random.Range(0, 5));

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
        foreach (var goal in goals)
        {
            for (int s = 0; s < goal.targetAmount; s++)
            {
                int x, y;

                do
                {
                    x = Random.Range(0, width);
                    y = Random.Range(0, height);
                }
                while (tiles[x, y].isSpecialItem);

                Tile tile = tiles[x, y];

                tile.SetType(goal.matchColorType);
                tile.image.sprite = goal.goalSprite;
                tile.isSpecialItem = true;
            }
        }
    }

    // =========================
    // SWAP
    // =========================

    public void SwapTiles(Tile a, Tile b, bool isReverting = false)
    {
        if (movesLeft <= 0 && !isReverting)
            return;

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
            movesLeft--;
            if (movesLeft < 0) movesLeft = 0;
            UpdateMovesUI();

            if (HasMatch())
                CheckMatches();
            else
                StartCoroutine(RevertSwap(a, b));
        }
    }

    IEnumerator RevertSwap(Tile a, Tile b)
    {
        yield return new WaitForSeconds(0.2f);
        SwapTiles(a, b, true);
    }

    // =========================
    // MATCH CHECK
    // =========================

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
        if (tiles[x, y] == null) return false;

        int type = tiles[x, y].tileType;

        int horizontal = 1;
        if (x > 0 && tiles[x - 1, y] != null && tiles[x - 1, y].tileType == type) horizontal++;
        if (x < width - 1 && tiles[x + 1, y] != null && tiles[x + 1, y].tileType == type) horizontal++;
        if (horizontal >= 3) return true;

        int vertical = 1;
        if (y > 0 && tiles[x, y - 1] != null && tiles[x, y - 1].tileType == type) vertical++;
        if (y < height - 1 && tiles[x, y + 1] != null && tiles[x, y + 1].tileType == type) vertical++;
        return vertical >= 3;
    }

    void CheckMatches()
    {
        matchedTiles.Clear();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                if (tiles[x, y] == null ||
                    tiles[x + 1, y] == null ||
                    tiles[x + 2, y] == null)
                    continue;

                int type = tiles[x, y].tileType;

                if (tiles[x + 1, y].tileType == type &&
                    tiles[x + 2, y].tileType == type)
                {
                    matchedTiles.Add(tiles[x, y]);
                    matchedTiles.Add(tiles[x + 1, y]);
                    matchedTiles.Add(tiles[x + 2, y]);
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                if (tiles[x, y] == null ||
                    tiles[x, y + 1] == null ||
                    tiles[x, y + 2] == null)
                    continue;

                int type = tiles[x, y].tileType;

                if (tiles[x, y + 1].tileType == type &&
                    tiles[x, y + 2].tileType == type)
                {
                    matchedTiles.Add(tiles[x, y]);
                    matchedTiles.Add(tiles[x, y + 1]);
                    matchedTiles.Add(tiles[x, y + 2]);
                }
            }
        }

        if (matchedTiles.Count > 0)
            StartCoroutine(ClearMatchesRoutine());
        else
            CheckLoseCondition();
    }

    IEnumerator ClearMatchesRoutine()
    {
        isProcessingMatches = true;

        yield return new WaitForSeconds(0.15f);

        foreach (Tile tile in matchedTiles)
        {
            if (tile == null) continue;

            if (tile.isSpecialItem)
            {
                foreach (var goal in goals)
                {
                    if (goal.matchColorType == tile.tileType)
                    {
                        goal.collectedAmount++;
                        break;
                    }
                }
            }

            int x = tile.x;
            int y = tile.y;

            tiles[x, y] = null;

            yield return StartCoroutine(tile.PlayDestroyAnimation());

            if (tile != null)
                Destroy(tile.gameObject);
        }

        matchedTiles.Clear();

        yield return new WaitForSeconds(0.1f);

        ApplyGravity();
        yield return new WaitForSeconds(0.1f);

        SpawnNewTiles();
        yield return new WaitForSeconds(0.2f);

        isProcessingMatches = false;

        CheckLevelComplete();
        CheckLoseCondition();
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

                    tile.tileSprites = tileSprites;
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
        foreach (var goal in goals)
        {
            if (goal.collectedAmount < goal.targetAmount)
                return;
        }

        FindObjectOfType<GameFlowManager>().OnLevelCompleted();
    }

    void CheckLoseCondition()
    {
        if (movesLeft > 0) return;

        foreach (var goal in goals)
        {
            if (goal.collectedAmount < goal.targetAmount)
            {
                FindObjectOfType<GameFlowManager>().OnOutOfMoves();
                return;
            }
        }
    }
}
