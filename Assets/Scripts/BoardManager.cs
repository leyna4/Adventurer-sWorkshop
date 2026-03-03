using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BoardManager : MonoBehaviour
{
    // ??????????????????????????????????????????????????????????????????
    // INSPECTOR
    // ??????????????????????????????????????????????????????????????????

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

    // ??????????????????????????????????????????????????????????????????
    // VERÝ
    // ??????????????????????????????????????????????????????????????????

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

        // Eski taþlarý temizle
        if (tiles != null)
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (tiles[x, y] != null) Destroy(tiles[x, y].gameObject);

        tiles = null;
        isSwapping = false;
        inputLocked = false;
        movesLeft = moveLimit;
        UpdateMovesUI();

        // 1. Önce normal board'u oluþtur
        GenerateBoard();

        // 2. ÖNEMLÝ: Hedef taþlarýný (special items) board üzerine yerleþtir
        PlaceSpecialItems();

        // 3. Level 4+ ise special tile'lara buz ekle (2 HP)
        AddIceToBoard();

        // 4. Eðer Level 1 ise Tutorial dizilimini yap
        if (currentLevel == 1) SetupTutorialBoard();

        // 5. UI'yý Yenile
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

    // ??????????????????????????????????????????????????????????????????
    // DÜZELTME 1: Goal tile'larýný board'a yerleþtir ve sprite'larýný göster
    // ??????????????????????????????????????????????????????????????????
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

                    // ? DÜZELTME: goalSprite varsa tile'ýn görseline uygula
                    if (goal.goalSprite != null && tiles[x, y].image != null)
                    {
                        tiles[x, y].image.sprite = goal.goalSprite;
                    }

                    placed++;
                }
            }
        }
    }

    // ??????????????????????????????????????????????????????????????????
    // DÜZELTME 2: Level 4+ special tile'lara 2HP buz ekle
    // ??????????????????????????????????????????????????????????????????
    void AddIceToBoard()
    {
        // Sadece Level 4 ve üzerinde buz mekanizmasý aktif
        if (currentLevel < 4) return;

        if (tiles == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile t = tiles[x, y];
                if (t != null && t.isSpecialItem)
                {
                    // 2 HP buz ekle (2 eþleþme sonrasý toplanabilir)
                    t.SetIce(2);
                    // Buz varken collect edilemez, önce kýrýlmasý lazým
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

    // ??????????????????????????????????????????????????????????????????
    // HAMLE MANTIÐI
    // ??????????????????????????????????????????????????????????????????

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

    // ??????????????????????????????????????????????????????????????????
    // EÞLEÞTÝRME & ÖZEL TILELAR
    // ??????????????????????????????????????????????????????????????????

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

            // ??????????????????????????????????????????????????????????
            // DÜZELTME 3: Buz kýrma ve collect mantýðý
            // ??????????????????????????????????????????????????????????
            var actualDestroy = new HashSet<Tile>();

            foreach (Tile t in toDestroy)
            {
                if (t == null) continue;

                if (t.isSpecialItem && t.hasIce)
                {
                    // Buzlu special tile: önce buz kýr
                    t.iceHitPoints--;

                    if (t.iceHitPoints <= 0)
                    {
                        // Buz tamamen kýrýldý ? artýk toplanabilir
                        t.ClearIce();
                        t.isCollectible = true;
                        // Bu eþleþmede hâlâ taþý patlatmýyoruz,
                        // sadece buzu kaldýrýyoruz. Bir sonraki eþleþmede toplanacak.
                        t.UpdateIceVisual();
                    }
                    else
                    {
                        // Hâlâ buz var, sadece görseli güncelle
                        t.UpdateIceVisual();
                    }
                    // Bu tur yok edilmeyecek
                    continue;
                }

                // Normal tile veya buzsuz special tile
                // Sadece isSpecialItem + isCollectible olanlar goal sayacýna girer
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

        // Yatay
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

        // Dikey
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

    // ??????????????????????????????????????????????????????????????????
    // YERÇEKÝMÝ & FEEDBACK & KONTROLLER
    // ??????????????????????????????????????????????????????????????????

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
        feedbackText.text = feedbackMessages[Random.Range(0, feedbackMessages.Length)];
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1f);
        feedbackText.gameObject.SetActive(false);
    }

    public void HideFeedback() { if (feedbackText != null) feedbackText.gameObject.SetActive(false); }
}