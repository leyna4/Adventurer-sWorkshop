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

    [Header("Special Tile Sprites")]
    public Sprite[] specialTileSprites;

    [Header("Board Settings")]
    public int width = 7;
    public int height = 7;
    public GameObject tilePrefab;
    public float tileSize = 70f;

    [Header("Tutorial")]
    public TutorialManager tutorialManager;

    [Header("Match Feedback")]
    public TMPro.TextMeshProUGUI feedbackText;

    

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

    static readonly string[] feedbackMessages = { "Harika!", "Muthis!", "Super!", "Mukemmel!", "Enfes!" };
    int feedbackIndex = 0;
    Coroutine feedbackCoroutine = null;

    [System.Obsolete]
    void Start() { orderUI = FindObjectOfType<OrderUI>(); }

    public void SetInputLocked(bool locked) { inputLocked = locked; }

    public Tile GetTile(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height) return tiles[x, y];
        return null;
    }

    public void SetGoals(List<GameFlowManager.LevelGoal> levelGoals, int moveLimitFromLevel)
    {
        goals.Clear();
        foreach (var lg in levelGoals)
            goals.Add(new GoalData
            {
                goalSprite = lg.goalSprite,
                targetAmount = lg.targetAmount,
                collectedAmount = 0,
                matchColorType = lg.matchColorID
            });

        moveLimit = moveLimitFromLevel;
        movesLeft = moveLimit;
        UpdateMovesUI();
    }

    [System.Obsolete]
    public void ResetBoardForNextLevel()
    {
        StopAllCoroutines();
        HideFeedback();

        
        if (tiles != null)
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (tiles[x, y] != null) Destroy(tiles[x, y].gameObject);

        tiles = null;
        isSwapping = false;
        inputLocked = false;
        movesLeft = moveLimit;
        UpdateMovesUI();

       
        GenerateBoard();

        
        PlaceSpecialItems();

       
        AddIceToBoard();

       
        if (currentLevel == 1) SetupTutorialBoard();

       
        GameFlowManager gfm = FindObjectOfType<GameFlowManager>();
        if (gfm != null)
        {
            gfm.UpdateGoalUI();
        }
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
        if (rt != null) rt.anchoredPosition = new Vector2(x * tileSize, y * tileSize);

        Tile tile = obj.GetComponent<Tile>();
        tile.tileSprites = tileSprites;
        tile.specialSprites = specialTileSprites;
        tile.board = this;
        tile.x = x;
        tile.y = y;
        tile.SetType(GetSafeRandomType(x, y));
        tiles[x, y] = tile;
    }

    int GetSafeRandomType(int x, int y)
    {
        var forbidden = new List<int>();

        if (x >= 2 && tiles[x - 1, y] != null && tiles[x - 2, y] != null &&
            tiles[x - 1, y].tileType == tiles[x - 2, y].tileType)
            forbidden.Add(tiles[x - 1, y].tileType);

        if (y >= 2 && tiles[x, y - 1] != null && tiles[x, y - 2] != null &&
            tiles[x, y - 1].tileType == tiles[x, y - 2].tileType)
            forbidden.Add(tiles[x, y - 1].tileType);

        int t; int s = 0;
        do { t = Random.Range(0, tileSprites.Length); s++; }
        while (forbidden.Contains(t) && s < 100);

        return t;
    }

    
    void PlaceSpecialItems()
    {
        if (tiles == null) return;

        foreach (var goal in goals)
        {
            int placed = 0;
            int attempts = 0;

            while (placed < goal.targetAmount && attempts < 1000)
            {
                attempts++;
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);

                if (!tiles[x, y].isSpecialItem)
                {
                    tiles[x, y].SetType(goal.matchColorType);
                    tiles[x, y].isSpecialItem = true;

                    
                    if (goal.goalSprite != null && tiles[x, y].image != null)
                    {
                        tiles[x, y].image.sprite = goal.goalSprite;
                    }

                    placed++;
                }
            }
        }
    }

   
    void AddIceToBoard()
    {
        
        if (currentLevel < 4) return;

        if (tiles == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile t = tiles[x, y];
                if (t != null && t.isSpecialItem)
                {
                    
                    t.SetIce(2);
                    
                    t.isCollectible = false;
                }
            }
        }
    }

    void SetupTutorialBoard()
    {
        if (tiles == null) return;

        foreach (var t in tiles) if (t != null) t.SetHighlight(false);

        int c1 = 0;
        int cOther = 1;

        SafeSetType(1, 0, c1);
        SafeSetType(0, 0, cOther);
        SafeSetType(0, 1, c1);
        SafeSetType(0, 2, c1);

        int c2 = 2;
        SafeSetType(3, 2, c2);
        SafeSetType(3, 3, cOther);
        SafeSetType(4, 3, c2);
        SafeSetType(5, 3, c2);
    }

    void SafeSetType(int x, int y, int color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        Tile t = tiles[x, y];
        if (t != null) t.SetType(color);
    }

    

    public void SwapTiles(Tile a, Tile b)
    {
        if (inputLocked || isSwapping || movesLeft <= 0) return;

        if (tutorialManager != null && tutorialManager.isTutorialActive)
        {
            if (!tutorialManager.CheckMove(a, b)) return;
        }

        StartCoroutine(SwapRoutine(a, b));
    }

    IEnumerator SwapRoutine(Tile a, Tile b)
    {
        isSwapping = true;

        if (tutorialManager != null && tutorialManager.isTutorialActive)
        {
            if (!tutorialManager.CheckMove(a, b))
            {
                isSwapping = false;
                yield break;
            }
        }

        movesLeft--;
        UpdateMovesUI();

        SwapData(a, b);
        yield return new WaitForSeconds(0.2f);

        if (HasMatch())
        {
            yield return StartCoroutine(ProcessMatches());
        }
        else
        {
            yield return new WaitForSeconds(0.1f);
            SwapData(a, b);
        }

        CheckLoseCondition();
        isSwapping = false;
    }

    void SwapData(Tile a, Tile b)
    {
        tiles[a.x, a.y] = b;
        tiles[b.x, b.y] = a;
        int tx = a.x, ty = a.y;
        a.x = b.x; a.y = b.y;
        b.x = tx; b.y = ty;

        RectTransform ra = a.GetComponent<RectTransform>();
        RectTransform rb = b.GetComponent<RectTransform>();
        ra.anchoredPosition = new Vector2(a.x * tileSize, a.y * tileSize);
        rb.anchoredPosition = new Vector2(b.x * tileSize, b.y * tileSize);
    }

   

    public bool HasMatch()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (tiles[x, y] == null) continue;
                int t = tiles[x, y].tileType;

                if (x + 2 < width && tiles[x + 1, y] != null && tiles[x + 2, y] != null &&
                    tiles[x + 1, y].tileType == t && tiles[x + 2, y].tileType == t) return true;

                if (y + 2 < height && tiles[x, y + 1] != null && tiles[x, y + 2] != null &&
                    tiles[x, y + 1].tileType == t && tiles[x, y + 2].tileType == t) return true;
            }
        return false;
    }

    IEnumerator ProcessMatches()
    {
        while (HasMatch())
        {
            var lines = GetMatchLines();
            var toDestroy = new HashSet<Tile>();
            var toUpgrade = new List<(Tile tile, Tile.SpecialType type, int color)>();
            var toActivate = new List<Tile>();

            foreach (var line in lines)
            {
                List<Tile> lineTiles = new List<Tile>();
                for (int i = 0; i < line.length; i++)
                {
                    Tile t = line.horizontal ? tiles[line.sx + i, line.sy] : tiles[line.sx, line.sy + i];
                    if (t != null) lineTiles.Add(t);
                }

                foreach (var t in lineTiles)
                    if (t.specialType != Tile.SpecialType.None) toActivate.Add(t);

                if (line.length >= 5)
                {
                    Tile mid = lineTiles[line.length / 2];
                    toUpgrade.Add((mid, Tile.SpecialType.ColumnClear, mid.tileType));
                    foreach (var t in lineTiles) if (t != mid) toDestroy.Add(t);
                }
                else if (line.length == 4)
                {
                    Tile mid = lineTiles[1];
                    toUpgrade.Add((mid, Tile.SpecialType.RowClear, mid.tileType));
                    foreach (var t in lineTiles) if (t != mid) toDestroy.Add(t);
                }
                else
                {
                    foreach (var t in lineTiles) toDestroy.Add(t);
                }
            }

            foreach (var sp in toActivate)
            {
                if (sp.specialType == Tile.SpecialType.RowClear)
                    for (int x = 0; x < width; x++) toDestroy.Add(tiles[x, sp.y]);
                else if (sp.specialType == Tile.SpecialType.ColumnClear)
                    for (int y = 0; y < height; y++) toDestroy.Add(tiles[sp.x, y]);
            }

            foreach (var (ut, _, __) in toUpgrade) toDestroy.Remove(ut);

            
            var actualDestroy = new HashSet<Tile>();

            foreach (Tile t in toDestroy)
            {
                if (t == null) continue;

                if (t.isSpecialItem && t.hasIce)
                {
                    
                    t.iceHitPoints--;

                    if (t.iceHitPoints <= 0)
                    {
                        
                        t.ClearIce();
                        t.isCollectible = true;
                        
                        t.UpdateIceVisual();
                    }
                    else
                    {
                        
                        t.UpdateIceVisual();
                    }
                    
                    continue;
                }

                
                if (t.isSpecialItem && t.isCollectible)
                {
                    CollectGoal(t.tileType);
                }

                actualDestroy.Add(t);
            }

            int count = 0;
            foreach (Tile t in actualDestroy)
            {
                if (t == null) continue;
                count++;
                tiles[t.x, t.y] = null;
                StartCoroutine(DestroyTileAnimated(t));
            }

            foreach (var (ut, type, col) in toUpgrade) ut.SetSpecialType(type, col);

            if (count > 0) StartCoroutine(ShowMatchFeedback(count));

            yield return new WaitForSeconds(0.2f);
            ApplyGravity();
            yield return new WaitForSeconds(0.2f);
            SpawnNewTiles();
            yield return new WaitForSeconds(0.3f);
        }

        CheckLevelComplete();
    }

    IEnumerator DestroyTileAnimated(Tile t)
    {
        yield return StartCoroutine(t.PlayDestroyAnimation());
        Destroy(t.gameObject);
    }

    struct MatchLine { public int length; public bool horizontal; public int sx, sy; }

    List<MatchLine> GetMatchLines()
    {
        var result = new List<MatchLine>();

        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width - 2; x++)
            {
                int t = tiles[x, y].tileType;
                int len = 1;
                while (x + len < width && tiles[x + len, y] != null && tiles[x + len, y].tileType == t) len++;
                if (len >= 3) { result.Add(new MatchLine { length = len, horizontal = true, sx = x, sy = y }); x += len - 1; }
            }
        }

        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height - 2; y++)
            {
                int t = tiles[x, y].tileType;
                int len = 1;
                while (y + len < height && tiles[x, y + len] != null && tiles[x, y + len].tileType == t) len++;
                if (len >= 3) { result.Add(new MatchLine { length = len, horizontal = false, sx = x, sy = y }); y += len - 1; }
            }
        }

        return result;
    }

   

    void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (tiles[x, y] == null)
                    for (int ny = y + 1; ny < height; ny++)
                        if (tiles[x, ny] != null)
                        {
                            tiles[x, y] = tiles[x, ny];
                            tiles[x, ny] = null;
                            tiles[x, y].y = y;
                            tiles[x, y].GetComponent<RectTransform>().anchoredPosition = new Vector2(x * tileSize, y * tileSize);
                            break;
                        }
    }

    void SpawnNewTiles()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (tiles[x, y] == null) SpawnTileAt(x, y);
    }

    void CollectGoal(int tileType)
    {
        foreach (var goal in goals)
            if (goal.matchColorType == tileType && goal.collectedAmount < goal.targetAmount)
                goal.collectedAmount++;
    }

    [System.Obsolete]
    void CheckLevelComplete()
    {
        foreach (var goal in goals) if (goal.collectedAmount < goal.targetAmount) return;
        StartCoroutine(DelayedLevelComplete());
    }

    [System.Obsolete]
    IEnumerator DelayedLevelComplete()
    {
        inputLocked = true;
        yield return new WaitForSeconds(0.6f);
        FindObjectOfType<GameFlowManager>()?.OnLevelCompleted();
    }

    [System.Obsolete]
    void CheckLoseCondition()
    {
        if (movesLeft <= 0)
            FindObjectOfType<GameFlowManager>()?.OnOutOfMoves();
    }

    void UpdateMovesUI() { if (movesText != null) movesText.text = movesLeft.ToString(); }

    IEnumerator ShowMatchFeedback(int matchSize)
    {
        if (feedbackText == null) yield break;
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(AnimateFeedback(matchSize));
    }

    IEnumerator AnimateFeedback(int matchSize)
    {
        feedbackText.text = feedbackMessages[Random.Range(0, feedbackMessages.Length)];
        feedbackText.gameObject.SetActive(true);
        feedbackText.enableWordWrapping = false;
        feedbackText.overflowMode = TMPro.TextOverflowModes.Overflow;

        RectTransform rt = feedbackText.GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, 60f); 

        
        float baseSize = 80f;
        if (matchSize >= 5) baseSize = 120f;
        else if (matchSize == 4) baseSize = 100f;
        feedbackText.fontSize = baseSize;

        
        if (matchSize >= 5)
            feedbackText.color = new Color(1f, 0.85f, 0f, 1f);      
        else if (matchSize == 4)
            feedbackText.color = new Color(1f, 0.45f, 0.1f, 1f);     
        else
            feedbackText.color = new Color(1f, 1f, 1f, 1f);          

        
        float t = 0f;
        float popDur = 0.18f;
        while (t < popDur)
        {
            t += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, t / popDur) * 1.3f;
            feedbackText.transform.localScale = Vector3.one * s;
            yield return null;
        }
       
        t = 0f;
        float snapDur = 0.08f;
        while (t < snapDur)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.3f, 1.0f, t / snapDur);
            feedbackText.transform.localScale = Vector3.one * s;
            yield return null;
        }
        feedbackText.transform.localScale = Vector3.one;

        
        yield return new WaitForSeconds(0.4f);

        float fadeDur = 0.45f;
        t = 0f;
        Color startColor = feedbackText.color;
        while (t < fadeDur)
        {
            t += Time.deltaTime;
            float ratio = t / fadeDur;
            
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ratio);
            
            feedbackText.color = new Color(startColor.r, startColor.g, startColor.b,
                                           Mathf.Lerp(1f, 0f, ratio));
            yield return null;
        }

       
        feedbackText.gameObject.SetActive(false);
        feedbackText.transform.localScale = Vector3.one;
        feedbackText.color = Color.white;
        rt.anchoredPosition = startPos;
        feedbackCoroutine = null;
    }

    public void HideFeedback()
    {
        if (feedbackCoroutine != null) { StopCoroutine(feedbackCoroutine); feedbackCoroutine = null; }
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
            feedbackText.transform.localScale = Vector3.one;
            feedbackText.color = Color.white;
        }
    }
}
