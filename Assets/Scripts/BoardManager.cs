using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

    [HideInInspector] public int currentLevel = 1;

    bool isSwapping = false;
    bool isProcessingMatches = false;

    void Start()
    {
        movesLeft = moveLimit;
        UpdateMovesUI();
        GenerateBoard();
        PlaceSpecialItems();
        //AddIceToBoard();
    }

    #region Setup

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
            {
                for (int y = 0; y < height; y++)
                {
                    if (tiles[x, y] != null)
                    {
                        Destroy(tiles[x, y].gameObject);
                    }
                }
            }
        }

        tiles = null;
        isSwapping = false;

        movesLeft = moveLimit;
        UpdateMovesUI();

        GenerateBoard();
        PlaceSpecialItems();
        AddIceToBoard();
        isProcessingMatches = false;
        Debug.Log("Level Started. isSwapping: " + isSwapping);
    }

    void UpdateMovesUI()
    {
        if (movesText != null)
            movesText.text = "Moves: " + movesLeft;
    }

    #endregion

    #region Board Generation

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
                SpawnTileAt(x, y);
            }
        }
    }

    void SpawnTileAt(int x, int y)
    {
        GameObject obj = Instantiate(tilePrefab, transform);
        Tile tile = obj.GetComponent<Tile>();

        tile.hasIce = false;
        if (tile.iceOverlay != null) tile.iceOverlay.gameObject.SetActive(false);
        tile.tileSprites = tileSprites;
        tile.SetType(Random.Range(0, 5));

        obj.transform.localPosition = GetWorldPosition(x, y);

        tiles[x, y] = tile;
        tile.x = x;
        tile.y = y;
        tile.board = this;
    }

    #endregion

    #region Special + Ice

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

               
                if (currentLevel >= 4)
                {
                    tile.SetIce(2);
                }
            }
        }
    }

    float GetIceChance()
    {
        if (currentLevel < 4) return 0f;
        if (currentLevel >= 10) return 0.8f;
        return Mathf.Lerp(0.5f, 0.8f, (currentLevel - 4) / 6f);
    }

    #endregion

    #region Swap System

    public void SwapTiles(Tile a, Tile b)
    {
        if (isSwapping) return;
        if (movesLeft <= 0) return;
        if (a == null || b == null) return;
        if (tiles == null) return;

        StartCoroutine(SwapRoutine(a, b));
    }

    IEnumerator SwapRoutine(Tile a, Tile b)
    {
        isSwapping = true;
        SwapData(a, b);

        movesLeft--;
        UpdateMovesUI();

        yield return new WaitForSeconds(0.2f);

        if (HasMatch())
        {
            yield return StartCoroutine(ProcessMatches());
        }
        else
        {
            SwapData(a, b);
        }

        CheckLoseCondition();
        isSwapping = false;
    }

    void SwapData(Tile a, Tile b)
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
    }

    #endregion

    #region Match System

    bool HasMatch()
    {
        return GetAllMatches().Count > 0;
    }

    List<Tile> GetAllMatches()
    {
        List<Tile> result = new List<Tile>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] == null) continue;

                int type = tiles[x, y].tileType;

                if (x < width - 2 &&
                    tiles[x + 1, y] != null &&
                    tiles[x + 2, y] != null &&
                    tiles[x + 1, y].tileType == type &&
                    tiles[x + 2, y].tileType == type)
                {
                    result.Add(tiles[x, y]);
                    result.Add(tiles[x + 1, y]);
                    result.Add(tiles[x + 2, y]);
                }

                if (y < height - 2 &&
                    tiles[x, y + 1] != null &&
                    tiles[x, y + 2] != null &&
                    tiles[x, y + 1].tileType == type &&
                    tiles[x, y + 2].tileType == type)
                {
                    result.Add(tiles[x, y]);
                    result.Add(tiles[x, y + 1]);
                    result.Add(tiles[x, y + 2]);
                }
            }
        }
        return result;
    }

    IEnumerator ProcessMatches()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

            List<Tile> matches = GetAllMatches();
            if (matches.Count == 0)
                break;

            foreach (Tile tile in matches)
            {
                if (tile == null) continue;

               
                if (tile.hasIce)
                {
                    tile.iceHitPoints--;

                    if (tile.iceHitPoints <= 0)
                    {
                        tile.ClearIce();
                    }
                    else
                    {
                       
                        tile.UpdateIceVisual();
                    }
                   
                    continue;
                }
                
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

                tiles[tile.x, tile.y] = null;
                Destroy(tile.gameObject);
            }

            yield return new WaitForSeconds(0.1f);
            ApplyGravity();
            yield return new WaitForSeconds(0.1f);
            SpawnNewTiles();
            yield return new WaitForSeconds(0.2f);
        }

        CheckLevelComplete();
        CheckLoseCondition();
    }

    #endregion

    #region Gravity

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
                    SpawnTileAt(x, y);
                }
            }
        }
    }

    #endregion

    #region Win/Lose

    void CheckLevelComplete()
    {
        foreach (var goal in goals)
            if (goal.collectedAmount < goal.targetAmount)
                return;

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

    void AddIceToBoard()
    {
        if (currentLevel < 4) return;

        float iceRatio = GetIceRatio();
        int totalTiles = width * height;
        int iceCount = Mathf.RoundToInt(totalTiles * iceRatio);

        int placed = 0;
        int safety = 0;

        while (placed < iceCount && safety < 500)
        {
            safety++;
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            Tile tile = tiles[x, y];

            if (tile == null || tile.hasIce || tile.isSpecialItem) continue;

            tile.SetIce(2);
            placed++;
        }
    }

    float GetIceRatio()
    {
        if (currentLevel < 4) return 0f;
        if (currentLevel >= 10) return 0.22f;
        return Mathf.Lerp(0.08f, 0.22f, (currentLevel - 4) / 6f);
    }

    #endregion
}