using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BoardManager : MonoBehaviour
{
    // ??????????????????????????????????????????????????????????????
    //  INSPECTOR
    // ??????????????????????????????????????????????????????????????
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

    // ??????????????????????????????????????????????????????????????
    //  VERÝ
    // ??????????????????????????????????????????????????????????????
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

        // 3. Varsa buzlarý ekle
        AddIceToBoard();

        // 4. Eðer Level 1 ise Tutorial dizilimini yap (Bu metot özel itemlara dokunmaz)
        if (currentLevel == 1) SetupTutorialBoard();

        // 5. UI'yý Yenile: GameFlowManager'a hedefleri tekrar çizdir
        GameFlowManager gfm = FindObjectOfType<GameFlowManager>();
        if (gfm != null)
        {
            gfm.UpdateGoalUI(); // Goal Container'ý tekrar doldurur
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
            // Hedef sayýsý kadar rastgele yere bu taþtan koy
            while (placed < goal.targetAmount)
            {
                int x = Random.Range(0, width);
                int y = Random.Range(0, height);

                // Eðer orasý zaten özel bir item deðilse yerleþtir
                if (!tiles[x, y].isSpecialItem)
                {
                    tiles[x, y].SetType(goal.matchColorType);
                    tiles[x, y].isSpecialItem = true;
                    placed++;
                }
            }
        }
    }

    void AddIceToBoard() { }

    
    void SetupTutorialBoard()
    {
        if (tiles == null) return;

        // Her þeyi önce karartalým (TutorialManager bunu yönetecek ama baþlangýç için)
        foreach (var t in tiles) if (t != null) t.SetHighlight(false);

        
        // (1,0) saða (0,0)'a çekilecek. (0,0)-(0,1)-(0,2) ayný renk olacak.
        int c1 = 0; // Kýrmýzý diyelim
        int cOther = 1; // Mavi diyelim

        SafeSetType(1, 0, c1);     // Sürüklenecek taþ
        SafeSetType(0, 0, cOther); // Yerine geçecek olan
        SafeSetType(0, 1, c1);     // Hedefteki üstteki 1
        SafeSetType(0, 2, c1);     // Hedefteki üstteki 2

        // --- 2. HAMLE: SATIR MATCH (Yatay) ---
        // (3,2) yukarý (3,3)'e çekilecek. (3,3)-(4,3)-(5,3) ayný renk olacak.
        int c2 = 2; // Yeþil diyelim

        SafeSetType(3, 2, c2);     // Sürüklenecek taþ
        SafeSetType(3, 3, cOther); // Yerine geçecek olan
        SafeSetType(4, 3, c2);     // Hedefteki saðdaki 1
        SafeSetType(5, 3, c2);     // Hedefteki saðdaki 2
    }

    void SafeSetType(int x, int y, int color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        Tile t = tiles[x, y];
        if (t != null) t.SetType(color);
    }

    // ??????????????????????????????????????????????????????????????
    //  HAMLE MANTIÐI
    // ??????????????????????????????????????????????????????????????
    public void SwapTiles(Tile a, Tile b)
    {
        if (inputLocked || isSwapping || movesLeft <= 0) return;

        // Tutorial Kontrolü
        if (tutorialManager != null && tutorialManager.isTutorialActive)
        {
            if (!tutorialManager.CheckMove(a, b)) return;
        }

        StartCoroutine(SwapRoutine(a, b));
    }

    IEnumerator SwapRoutine(Tile a, Tile b)
    {
        isSwapping = true;

        // --- TUTORIAL KONTROLÜ ---
        if (tutorialManager != null && tutorialManager.isTutorialActive)
        {
            if (!tutorialManager.CheckMove(a, b))
            {
                // Tutorial'dayken yanlýþ hamle yapýlýrsa hiçbir þey yapma ve çýk
                isSwapping = false;
                yield break;
            }
        }

        // --- HAMLE DÜÞME MANTIÐI ---
        // Taþlar swap edildiði an (match olsun ya da olmasýn) hamle hakký düþer.
        movesLeft--;
        UpdateMovesUI();

        // Görsel olarak yer deðiþtir
        SwapData(a, b);
        yield return new WaitForSeconds(0.2f);

        if (HasMatch())
        {
            // Eþleþme varsa patlatma iþlemlerine geç
            yield return StartCoroutine(ProcessMatches());
        }
        else
        {
            // Eþleþme yoksa taþlarý geri al (ama hamle gitmiþ oldu)
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

    // ??????????????????????????????????????????????????????????????
    //  EÞLEÞTÝRME & ÖZEL TILELAR
    // ??????????????????????????????????????????????????????????????
    public bool HasMatch()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (tiles[x, y] == null) continue;
                int t = tiles[x, y].tileType;
                if (x + 2 < width && tiles[x + 1, y] != null && tiles[x + 2, y] != null && tiles[x + 1, y].tileType == t && tiles[x + 2, y].tileType == t) return true;
                if (y + 2 < height && tiles[x, y + 1] != null && tiles[x, y + 2] != null && tiles[x, y + 1].tileType == t && tiles[x, y + 2].tileType == t) return true;
            }
        return false;
    }

    IEnumerator ProcessMatches()
    {
        while (HasMatch())
        {
            var lines = GetMatchLines();
            var toDestroy = new HashSet<Tile>();
            var toUpgrade = new List<(Tile tile, Tile.SpecialType type)>();
            var toActivate = new List<Tile>();

            foreach (var line in lines)
            {
                List<Tile> lineTiles = new List<Tile>();
                for (int i = 0; i < line.length; i++)
                {
                    Tile t = line.horizontal ? tiles[line.sx + i, line.sy] : tiles[line.sx, line.sy + i];
                    if (t != null) lineTiles.Add(t);
                }

                // Özel Tile Aktivasyonu
                foreach (var t in lineTiles)
                    if (t.specialType != Tile.SpecialType.None) toActivate.Add(t);

                // 4'lü veya 5'li Match Kontrolü (Yeni Özel Tile Yaratma)
                if (line.length >= 5)
                {
                    Tile mid = lineTiles[line.length / 2];
                    toUpgrade.Add((mid, Tile.SpecialType.ColumnClear));
                    foreach (var t in lineTiles) if (t != mid) toDestroy.Add(t);
                }
                else if (line.length == 4)
                {
                    Tile mid = lineTiles[1];
                    toUpgrade.Add((mid, Tile.SpecialType.RowClear));
                    foreach (var t in lineTiles) if (t != mid) toDestroy.Add(t);
                }
                else
                {
                    foreach (var t in lineTiles) toDestroy.Add(t);
                }
            }

            // Özel Yetenekleri Çalýþtýr
            foreach (var sp in toActivate)
            {
                if (sp.specialType == Tile.SpecialType.RowClear)
                    for (int x = 0; x < width; x++) toDestroy.Add(tiles[x, sp.y]);
                else if (sp.specialType == Tile.SpecialType.ColumnClear)
                    for (int y = 0; y < height; y++) toDestroy.Add(tiles[sp.x, y]);
            }

            foreach (var (ut, _) in toUpgrade) toDestroy.Remove(ut);

            // Patlatma
            int count = 0;
            foreach (Tile t in toDestroy)
            {
                if (t == null) continue;
                CollectGoal(t.tileType);
                count++;
                tiles[t.x, t.y] = null;
                Destroy(t.gameObject);
            }

            // Upgrade
            foreach (var (ut, type) in toUpgrade) ut.SetSpecialType(type);

            if (count > 0) StartCoroutine(ShowMatchFeedback(count));

            yield return new WaitForSeconds(0.2f);
            ApplyGravity();
            yield return new WaitForSeconds(0.2f);
            SpawnNewTiles();
            yield return new WaitForSeconds(0.3f);
        }
        CheckLevelComplete();
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

    // ??????????????????????????????????????????????????????????????
    //  YERÇEKÝMÝ & FEEDBACK & KONTROLLER
    // ??????????????????????????????????????????????????????????????
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
        FindObjectOfType<GameFlowManager>()?.OnLevelCompleted();
    }

    [System.Obsolete]
    void CheckLoseCondition()
    {
        if (movesLeft <= 0) FindObjectOfType<GameFlowManager>()?.OnOutOfMoves();
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