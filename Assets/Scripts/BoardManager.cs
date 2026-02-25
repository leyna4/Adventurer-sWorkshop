using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BoardManager : MonoBehaviour
{
   
    [Header("UI")]
    public TextMeshProUGUI movesText;

    [Header("Tile Visuals")]
    public Sprite[] tileSprites;

    [Header("Board Settings")]
    public int width = 7;
    public int height = 7;
    public GameObject tilePrefab;
    public float tileSize = 70f;

    [Header("Tutorial")]
    public TutorialManager tutorialManager;

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

    public Tile[,] tiles;
    [HideInInspector] public int currentLevel = 1;

    [HideInInspector] public bool inputLocked = false;

    bool isSwapping = false;
    OrderUI orderUI;


   
    void Start()
    {
        orderUI = FindObjectOfType<OrderUI>();
        
    }

    
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }

   
    public Tile GetTile(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            return tiles[x, y];
        return null;
    }

    
    public void SetGoals(List<GameFlowManager.LevelGoal> levelGoals, int moveLimitFromLevel)
    {
        goals.Clear();
        foreach (var lg in levelGoals)
        {
            goals.Add(new GoalData
            {
                goalSprite = lg.goalSprite,
                targetAmount = lg.targetAmount,
                collectedAmount = 0,
                matchColorType = lg.matchColorID
            });
        }
        moveLimit = moveLimitFromLevel;
        movesLeft = moveLimit;
        UpdateMovesUI();
    }

    public void ResetBoardForNextLevel()
    {
        
        if (tiles != null)
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (tiles[x, y] != null)
                        Destroy(tiles[x, y].gameObject);

        tiles = null;
        isSwapping = false;
        inputLocked = false;   
        movesLeft = moveLimit;
        UpdateMovesUI();

        GenerateBoard();
        PlaceSpecialItems();
        AddIceToBoard();

        
        if (orderUI == null) orderUI = FindObjectOfType<OrderUI>();
        if (orderUI != null) orderUI.RefreshGoals();
    }

    
    void GenerateBoard()
    {
        tiles = new Tile[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                SpawnTileAt(x, y);
    }

    void SpawnTileAt(int x, int y)
    {
        GameObject obj = Instantiate(tilePrefab, transform);

        
        RectTransform rt = obj.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = new Vector2(x * tileSize, y * tileSize);
        else
            obj.transform.localPosition = GetWorldPosition(x, y);

        Tile tile = obj.GetComponent<Tile>();
        tile.tileSprites = tileSprites;
        tile.board = this;
        tile.x = x;
        tile.y = y;
        tile.SetType(GetSafeRandomType(x, y));
        tiles[x, y] = tile;
    }

   
    int GetSafeRandomType(int x, int y)
    {
        List<int> forbidden = new List<int>();

        if (x >= 2 && tiles[x - 1, y] != null && tiles[x - 2, y] != null &&
            tiles[x - 1, y].tileType == tiles[x - 2, y].tileType)
            forbidden.Add(tiles[x - 1, y].tileType);

        if (y >= 2 && tiles[x, y - 1] != null && tiles[x, y - 2] != null &&
            tiles[x, y - 1].tileType == tiles[x, y - 2].tileType)
            forbidden.Add(tiles[x, y - 1].tileType);

        int t; int safety = 0;
        do { t = Random.Range(0, tileSprites.Length); safety++; }
        while (forbidden.Contains(t) && safety < 100);
        return t;
    }

   
    Vector3 GetWorldPosition(int x, int y)
    {
        float offX = (width - 1) * tileSize / 2f;
        float offY = (height - 1) * tileSize / 2f;
        return new Vector3(x * tileSize - offX, y * tileSize - offY, 0f);
    }

    void PlaceSpecialItems()
    {
        foreach (var goal in goals)
        {
            for (int s = 0; s < goal.targetAmount; s++)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);
                Tile tile = tiles[x, y];
                tile.SetType(goal.matchColorType);
                tile.isSpecialItem = true;
            }
        }
    }

    void AddIceToBoard()
    {
        // Buz engeli eklemek istersen burayý doldur
        // Örn: belirli pozisonlara tile.SetIce(2);
    }

    
    public void SwapTiles(Tile a, Tile b)
    {
       
        if (tutorialManager != null && tutorialManager.isTutorialActive)
        {
            if (!tutorialManager.ValidateSwap(a, b))
                return;
        }

        
        if (inputLocked) return;

        
        if (isSwapping || movesLeft <= 0 || a == null || b == null || tiles == null)
            return;

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
            yield return StartCoroutine(ProcessMatches());
        else
            SwapData(a, b);   

        CheckLoseCondition();
        isSwapping = false;
    }

    void SwapData(Tile a, Tile b)
    {
        tiles[a.x, a.y] = b;
        tiles[b.x, b.y] = a;

        int tempX = a.x; int tempY = a.y;
        a.x = b.x; a.y = b.y;
        b.x = tempX; b.y = tempY;

        
        RectTransform rta = a.GetComponent<RectTransform>();
        RectTransform rtb = b.GetComponent<RectTransform>();

        if (rta != null && rtb != null)
        {
            rta.anchoredPosition = new Vector2(a.x * tileSize, a.y * tileSize);
            rtb.anchoredPosition = new Vector2(b.x * tileSize, b.y * tileSize);
        }
        else
        {
            a.transform.localPosition = GetWorldPosition(a.x, a.y);
            b.transform.localPosition = GetWorldPosition(b.x, b.y);
        }
    }

    
    public bool HasMatch()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (tiles[x, y] == null || tiles[x, y].isObstacle) continue;
                int t = tiles[x, y].tileType;

                if (x + 2 < width &&
                    tiles[x + 1, y] != null && tiles[x + 2, y] != null &&
                    tiles[x + 1, y].tileType == t && tiles[x + 2, y].tileType == t)
                    return true;

                if (y + 2 < height &&
                    tiles[x, y + 1] != null && tiles[x, y + 2] != null &&
                    tiles[x, y + 1].tileType == t && tiles[x, y + 2].tileType == t)
                    return true;
            }
        return false;
    }

    IEnumerator ProcessMatches()
    {
        bool found = true;
        while (found)
        {
            found = false;
            HashSet<Tile> toDestroy = new HashSet<Tile>();

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (tiles[x, y] == null || tiles[x, y].isObstacle) continue;
                    int t = tiles[x, y].tileType;

                    
                    if (x + 2 < width &&
                        tiles[x + 1, y] != null && tiles[x + 2, y] != null &&
                        tiles[x + 1, y].tileType == t && tiles[x + 2, y].tileType == t)
                    {
                        toDestroy.Add(tiles[x, y]);
                        toDestroy.Add(tiles[x + 1, y]);
                        toDestroy.Add(tiles[x + 2, y]);
                    }

                    
                    if (y + 2 < height &&
                        tiles[x, y + 1] != null && tiles[x, y + 2] != null &&
                        tiles[x, y + 1].tileType == t && tiles[x, y + 2].tileType == t)
                    {
                        toDestroy.Add(tiles[x, y]);
                        toDestroy.Add(tiles[x, y + 1]);
                        toDestroy.Add(tiles[x, y + 2]);
                    }
                }

            if (toDestroy.Count > 0)
            {
                found = true;

                foreach (Tile tile in toDestroy)
                {
                   
                    if (tile.hasIce)
                    {
                        tile.iceHitPoints--;
                        if (tile.iceHitPoints <= 0)
                            tile.ClearIce();
                        else
                            tile.UpdateIceVisual();
                       
                        continue;
                    }

                   
                    CollectGoal(tile.tileType);

                    yield return StartCoroutine(tile.PlayDestroyAnimation());
                    tiles[tile.x, tile.y] = null;
                    Destroy(tile.gameObject);
                }

                yield return new WaitForSeconds(0.1f);
                ApplyGravity();
                yield return new WaitForSeconds(0.2f);
                SpawnNewTiles();
                yield return new WaitForSeconds(0.3f);
                CheckLevelComplete();
            }
        }
    }

    void CollectGoal(int tileType)
    {
        foreach (var goal in goals)
        {
            if (goal.matchColorType == tileType && goal.collectedAmount < goal.targetAmount)
            {
                goal.collectedAmount++;
                
                break;
            }
        }
    }

    
    void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != null) continue;

                for (int ny = y + 1; ny < height; ny++)
                {
                    if (tiles[x, ny] == null) continue;

                    tiles[x, y] = tiles[x, ny];
                    tiles[x, ny] = null;
                    tiles[x, y].y = y;

                    RectTransform rt = tiles[x, y].GetComponent<RectTransform>();
                    if (rt != null)
                        rt.anchoredPosition = new Vector2(x * tileSize, y * tileSize);
                    else
                        tiles[x, y].transform.localPosition = GetWorldPosition(x, y);

                    break;
                }
            }
        }
    }

    void SpawnNewTiles()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (tiles[x, y] == null)
                    SpawnTileAt(x, y);
    }

    
    void CheckLevelComplete()
    {
        foreach (var goal in goals)
            if (goal.collectedAmount < goal.targetAmount) return;

        GameFlowManager gfm = FindObjectOfType<GameFlowManager>();
        if (gfm != null) gfm.OnLevelCompleted();
    }

    void CheckLoseCondition()
    {
        if (movesLeft > 0) return;

        bool allDone = true;
        foreach (var goal in goals)
            if (goal.collectedAmount < goal.targetAmount) { allDone = false; break; }

        GameFlowManager gfm = FindObjectOfType<GameFlowManager>();
        if (gfm == null) return;

        if (allDone) gfm.OnLevelCompleted();
        else gfm.OnOutOfMoves();
    }

    void UpdateMovesUI()
    {
        if (movesText != null)
            movesText.text = movesLeft.ToString();
    }
}
