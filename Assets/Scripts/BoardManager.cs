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

    // ── İPUCU SİSTEMİ ────────────────────────────────────────────────
    float idleTimer = 0f;
    const float hintDelay = 3f;
    bool hintActive = false;
    List<Tile> hintTiles = new List<Tile>();

    static readonly string[] feedbackMessages = { "Amazing!", "Awesome!", "Super!", "Excellent!", "Fantastic!" };
    int feedbackIndex = 0;
    Coroutine feedbackCoroutine = null;

    [System.Obsolete]
    void Start() { orderUI = FindObjectOfType<OrderUI>(); }

    void Update()
    {
        if (inputLocked || isSwapping)
        {
            idleTimer = 0f;
            return;
        }
        idleTimer += Time.deltaTime;
        if (idleTimer >= hintDelay && !hintActive)
            ShowHint();
    }

    public void SetInputLocked(bool locked) { inputLocked = locked; }

    public Tile GetTile(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height) return tiles[x, y];
        return null;
    }

    // ── İPUCU METODLARI ──────────────────────────────────────────────
    void ResetIdleTimer()
    {
        idleTimer = 0f;
        HideHint();
    }

    void ShowHint()
    {
        hintActive = true;
        hintTiles.Clear();
        List<Tile> candidates = FindHintTiles();
        if (candidates == null || candidates.Count == 0) return;
        hintTiles = candidates;
        foreach (var t in hintTiles)
            if (t != null) t.StartHint();
    }

    void HideHint()
    {
        hintActive = false;
        foreach (var t in hintTiles)
            if (t != null) t.StopHint();
        hintTiles.Clear();
    }

    List<Tile> FindHintTiles()
    {
        int[] dx = { 1, 0, -1, 0 };
        int[] dy = { 0, 1, 0, -1 };

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Tile a = tiles[x, y];
                if (a == null) continue;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + dx[d], ny = y + dy[d];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                    Tile b = tiles[nx, ny];
                    if (b == null) continue;

                    // Geçici swap
                    tiles[x, y] = b; tiles[nx, ny] = a;
                    a.x = nx; a.y = ny; b.x = x; b.y = y;
                    bool match = HasMatch();
                    // Geri al
                    tiles[x, y] = a; tiles[nx, ny] = b;
                    a.x = x; a.y = y; b.x = nx; b.y = ny;

                    if (match) return new List<Tile> { a, b };
                }
            }
        return null;
    }

    // ── GOALS ────────────────────────────────────────────────────────
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
        HideHint();
        idleTimer = 0f;
        hintActive = false;

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
        if (gfm != null) gfm.UpdateGoalUI();
    }

    void GenerateBoard()
    {
        tiles = new Tile[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                SpawnTileAt(x, y);
    }

    void SpawnTileAt(int x, int y, bool animate = false)
    {
        GameObject obj = Instantiate(tilePrefab, transform);
        RectTransform rt = obj.GetComponent<RectTransform>();
        Vector2 finalPos = new Vector2(x * tileSize, y * tileSize);
        if (rt != null) rt.anchoredPosition = animate
            ? finalPos + new Vector2(0, tileSize * 2f)
            : finalPos;

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
            int placed = 0, attempts = 0;
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
                        tiles[x, y].image.sprite = goal.goalSprite;
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

    void SetupTutorialBoard()
    {
        if (tiles == null) return;
        foreach (var t in tiles) if (t != null) t.SetHighlight(false);
        int c1 = 0, cOther = 1;
        SafeSetType(1, 0, c1); SafeSetType(0, 0, cOther);
        SafeSetType(0, 1, c1); SafeSetType(0, 2, c1);
        int c2 = 2;
        SafeSetType(3, 2, c2); SafeSetType(3, 3, cOther);
        SafeSetType(4, 3, c2); SafeSetType(5, 3, c2);
    }

    void SafeSetType(int x, int y, int color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        Tile t = tiles[x, y];
        if (t != null) t.SetType(color);
    }

    // ── HAMLE MANTIĞI ────────────────────────────────────────────────
    public void SwapTiles(Tile a, Tile b)
    {
        if (inputLocked || isSwapping || movesLeft <= 0) return;
        if (tutorialManager != null && tutorialManager.isTutorialActive)
            if (!tutorialManager.CheckMove(a, b)) return;
        StartCoroutine(SwapRoutine(a, b));
    }

    IEnumerator SwapRoutine(Tile a, Tile b)
    {
        isSwapping = true;
        ResetIdleTimer();

        if (tutorialManager != null && tutorialManager.isTutorialActive)
        {
            if (!tutorialManager.CheckMove(a, b))
            {
                isSwapping = false;
                yield break;
            }
        }

        SwapLogicOnly(a, b);
        Vector2 posA = new Vector2(a.x * tileSize, a.y * tileSize);
        Vector2 posB = new Vector2(b.x * tileSize, b.y * tileSize);
        a.MoveTo(posA, 0.18f);
        b.MoveTo(posB, 0.18f);
        yield return new WaitForSeconds(0.2f);

        if (HasMatch())
        {
            movesLeft--;
            UpdateMovesUI();
            yield return StartCoroutine(ProcessMatches());
        }
        else
        {
            StartCoroutine(a.PlayShakeAnimation());
            StartCoroutine(b.PlayShakeAnimation());
            yield return new WaitForSeconds(0.2f);
            SwapLogicOnly(a, b);
            a.MoveTo(new Vector2(a.x * tileSize, a.y * tileSize), 0.15f);
            b.MoveTo(new Vector2(b.x * tileSize, b.y * tileSize), 0.15f);
            yield return new WaitForSeconds(0.18f);
        }

        CheckLoseCondition();
        isSwapping = false;
    }

    void SwapLogicOnly(Tile a, Tile b)
    {
        tiles[a.x, a.y] = b;
        tiles[b.x, b.y] = a;
        int tx = a.x, ty = a.y;
        a.x = b.x; a.y = b.y;
        b.x = tx; b.y = ty;
    }

    // ── EŞLEŞTİRME ───────────────────────────────────────────────────
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
            var toUpgrade = new List<(Tile tile, Tile.SpecialType type, int color)>();
            var toActivate = new List<Tile>();
            var lineGroups = new List<List<Tile>>();
            var allToDestroy = new HashSet<Tile>();

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
                    var group = new List<Tile>();
                    foreach (var t in lineTiles) if (t != mid) { group.Add(t); allToDestroy.Add(t); }
                    lineGroups.Add(group);
                }
                else if (line.length == 4)
                {
                    Tile mid = lineTiles[1];
                    toUpgrade.Add((mid, Tile.SpecialType.RowClear, mid.tileType));
                    var group = new List<Tile>();
                    foreach (var t in lineTiles) if (t != mid) { group.Add(t); allToDestroy.Add(t); }
                    lineGroups.Add(group);
                }
                else
                {
                    lineGroups.Add(new List<Tile>(lineTiles));
                    foreach (var t in lineTiles) allToDestroy.Add(t);
                }
            }

            foreach (var sp in toActivate)
            {
                if (sp.specialType == Tile.SpecialType.RowClear)
                    for (int x = 0; x < width; x++) if (tiles[x, sp.y] != null) allToDestroy.Add(tiles[x, sp.y]);
                        else if (sp.specialType == Tile.SpecialType.ColumnClear)
                            for (int y = 0; y < height; y++) if (tiles[sp.x, y] != null) allToDestroy.Add(tiles[sp.x, y]);
            }

            foreach (var (ut, _, __) in toUpgrade) allToDestroy.Remove(ut);

            var actualDestroy = new HashSet<Tile>();
            foreach (Tile t in allToDestroy)
            {
                if (t == null) continue;
                if (t.isSpecialItem && t.hasIce)
                {
                    t.iceHitPoints--;
                    if (t.iceHitPoints <= 0) { t.ClearIce(); t.isCollectible = true; }
                    else t.UpdateIceVisual();
                    continue;
                }
                if (t.isSpecialItem && t.isCollectible) CollectGoal(t.tileType);
                actualDestroy.Add(t);
            }

            float cascadeDelay = 0.13f;
            foreach (var group in lineGroups)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    Tile t = group[i];
                    if (t == null || !actualDestroy.Contains(t)) continue;
                    StartCoroutine(DestroyTileWithDelay(t, i * cascadeDelay));
                    tiles[t.x, t.y] = null;
                }
            }
            foreach (Tile t in actualDestroy)
            {
                if (t == null || tiles[t.x, t.y] == null) continue;
                StartCoroutine(DestroyTileWithDelay(t, 0f));
                tiles[t.x, t.y] = null;
            }

            foreach (var (ut, type, col) in toUpgrade) ut.SetSpecialType(type, col);

            if (actualDestroy.Count > 0) StartCoroutine(ShowMatchFeedback(actualDestroy.Count));

            float maxCascade = 0f;
            foreach (var g in lineGroups) maxCascade = Mathf.Max(maxCascade, g.Count * cascadeDelay);
            yield return new WaitForSeconds(maxCascade + 0.15f);

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

    IEnumerator DestroyTileWithDelay(Tile t, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (t == null || t.gameObject == null) yield break;
        yield return StartCoroutine(t.PlayDestroyAnimation());
        if (t != null && t.gameObject != null) Destroy(t.gameObject);
    }

    struct MatchLine { public int length; public bool horizontal; public int sx, sy; }

    List<MatchLine> GetMatchLines()
    {
        var result = new List<MatchLine>();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width - 2; x++)
            {
                if (tiles[x, y] == null) continue;
                int t = tiles[x, y].tileType;
                int len = 1;
                while (x + len < width && tiles[x + len, y] != null && tiles[x + len, y].tileType == t) len++;
                if (len >= 3) { result.Add(new MatchLine { length = len, horizontal = true, sx = x, sy = y }); x += len - 1; }
            }
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height - 2; y++)
            {
                if (tiles[x, y] == null) continue;
                int t = tiles[x, y].tileType;
                int len = 1;
                while (y + len < height && tiles[x, y + len] != null && tiles[x, y + len].tileType == t) len++;
                if (len >= 3) { result.Add(new MatchLine { length = len, horizontal = false, sx = x, sy = y }); y += len - 1; }
            }
        return result;
    }

    // ── YERÇEKİMİ & SPAWN ────────────────────────────────────────────
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
                            Vector2 target = new Vector2(x * tileSize, y * tileSize);
                            float dist = (ny - y) * tileSize;
                            float dur = Mathf.Clamp(dist / 600f, 0.1f, 0.3f);
                            tiles[x, y].MoveTo(target, dur);
                            break;
                        }
    }

    void SpawnNewTiles()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (tiles[x, y] == null)
                {
                    SpawnTileAt(x, y, animate: true);
                    Vector2 finalPos = new Vector2(x * tileSize, y * tileSize);
                    StartCoroutine(tiles[x, y].PlaySpawnAnimation(finalPos));
                }
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

    // ── FEEDBACK ─────────────────────────────────────────────────────
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

        if (matchSize >= 5) feedbackText.color = new Color(1f, 0.85f, 0f, 1f);
        else if (matchSize == 4) feedbackText.color = new Color(1f, 0.45f, 0.1f, 1f);
        else feedbackText.color = new Color(1f, 1f, 1f, 1f);

        float t = 0f;
        while (t < 0.18f) { t += Time.deltaTime; feedbackText.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t / 0.18f) * 1.3f; yield return null; }
        t = 0f;
        while (t < 0.08f) { t += Time.deltaTime; feedbackText.transform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, t / 0.08f); yield return null; }
        feedbackText.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(0.4f);

        t = 0f;
        Color startColor = feedbackText.color;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float ratio = t / 0.45f;
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, ratio);
            feedbackText.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(1f, 0f, ratio));
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