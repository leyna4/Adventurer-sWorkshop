using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour,
    IPointerDownHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public int tileType;
    public Image image;
    public int x;
    public int y;
    public BoardManager board;
    public bool isObstacle = false;
    public int hitPoints = 0;
    public bool isCollectible = true;
    public bool hasIce = false;
    public int iceHitPoints = 0;
    public Image iceOverlay;
    public bool iceJustBroken = false;
    public bool isSpecialItem = false;

    public enum SpecialType { None, RowClear, ColumnClear }
    public SpecialType specialType = SpecialType.None;

    public Sprite[] specialSprites;
    public Sprite[] tileSprites;
    public Image specialOverlay;
    public Image selectionBorder;  // Inspector'da "SelectionBorder" child Image

    private Coroutine pulseCoroutine = null;
    private Coroutine selectionCoroutine = null;
    private static Tile currentSelected = null; // board genelinde tek seçim
    private Coroutine moveCoroutine = null;
    private Coroutine hintCoroutine = null;

    // Patlama parçacıkları için renk paleti (tile rengine göre seçilir)
    static readonly Color[] particleColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),
        new Color(1f, 0.8f, 0.2f),
        new Color(0.3f, 0.8f, 1f),
        new Color(0.5f, 1f, 0.4f),
        new Color(1f, 0.4f, 0.9f),
    };

    void Awake()
    {
        if (image == null) image = GetComponent<Image>();
        if (iceOverlay == null)
            iceOverlay = transform.Find("IceOverlay")?.GetComponent<Image>();
        if (iceOverlay != null) iceOverlay.raycastTarget = false;
        if (specialOverlay == null)
            specialOverlay = transform.Find("SpecialOverlay")?.GetComponent<Image>();
        if (specialOverlay != null)
        {
            specialOverlay.raycastTarget = false;
            specialOverlay.gameObject.SetActive(false);
        }

        if (selectionBorder == null)
            selectionBorder = transform.Find("SelectionBorder")?.GetComponent<Image>();
        if (selectionBorder != null)
        {
            selectionBorder.raycastTarget = false;
            selectionBorder.gameObject.SetActive(false);
        }
    }

    // ── TYPE & SPECIAL ───────────────────────────────────────────────
    public void SetType(int type)
    {
        tileType = type;
        specialType = SpecialType.None;
        if (tileSprites != null && type < tileSprites.Length)
            image.sprite = tileSprites[type];
        if (specialOverlay != null)
            specialOverlay.gameObject.SetActive(false);
    }

    public void SetSpecialType(SpecialType st, int colorType = -1)
    {
        specialType = st;
        if (colorType >= 0) tileType = colorType;
        if (tileSprites != null && tileType < tileSprites.Length)
            image.sprite = tileSprites[tileType];

        if (st == SpecialType.None)
        {
            if (specialOverlay != null) specialOverlay.gameObject.SetActive(false);
            return;
        }

        if (specialOverlay == null) specialOverlay = CreateSpecialOverlay();
        if (specialOverlay == null) return;

        specialOverlay.gameObject.SetActive(true);
        specialOverlay.transform.SetAsLastSibling();

        if (st == SpecialType.RowClear && specialSprites != null &&
            specialSprites.Length > 0 && specialSprites[0] != null)
        {
            specialOverlay.sprite = specialSprites[0];
            specialOverlay.transform.localRotation = Quaternion.identity;
        }
        else if (st == SpecialType.ColumnClear && specialSprites != null &&
                 specialSprites.Length > 1 && specialSprites[1] != null)
        {
            specialOverlay.sprite = specialSprites[1];
            specialOverlay.transform.localRotation = Quaternion.identity;
        }
        else if (specialSprites != null && specialSprites.Length > 0 && specialSprites[0] != null)
        {
            specialOverlay.sprite = specialSprites[0];
            specialOverlay.transform.localRotation = (st == SpecialType.ColumnClear)
                ? Quaternion.Euler(0, 0, 90f) : Quaternion.identity;
        }

        StartCoroutine(SpecialPopAnim());
    }

    Image CreateSpecialOverlay()
    {
        GameObject go = new GameObject("SpecialOverlay");
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = Color.white;
        go.transform.SetAsLastSibling();
        return img;
    }

    // ── SMOOTH HAREKET ───────────────────────────────────────────────
    public void MoveTo(Vector2 targetPos, float duration)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveRoutine(targetPos, duration));
    }

    IEnumerator MoveRoutine(Vector2 target, float duration)
    {
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 start = rt.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rt.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }
        rt.anchoredPosition = target;
        moveCoroutine = null;
    }

    // ── SPAWN ANİMASYONU ─────────────────────────────────────────────
    public IEnumerator PlaySpawnAnimation(Vector2 finalPos, float dropHeight = 80f)
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchoredPosition = finalPos + new Vector2(0, dropHeight);
        transform.localScale = Vector3.one * 0.5f;

        float dur = 0.25f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / dur));
            rt.anchoredPosition = Vector2.Lerp(finalPos + new Vector2(0, dropHeight), finalPos, t);
            transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one * 1.05f, t);
            yield return null;
        }
        elapsed = 0f;
        float snapDur = 0.07f;
        while (elapsed < snapDur)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * 1.05f, Vector3.one, elapsed / snapDur);
            yield return null;
        }
        rt.anchoredPosition = finalPos;
        transform.localScale = Vector3.one;
    }

    // ── SHAKE (geçersiz hamle) ───────────────────────────────────────
    public IEnumerator PlayShakeAnimation()
    {
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 origin = rt.anchoredPosition;
        float dur = 0.35f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / dur);
            rt.anchoredPosition = origin + new Vector2(Mathf.Sin(elapsed * 28f) * 7f * decay, 0);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    // ── İPUCU ANİMASYONU ─────────────────────────────────────────────
    public void StartHint()
    {
        StopHint();
        hintCoroutine = StartCoroutine(HintLoop());
    }

    public void StopHint()
    {
        if (hintCoroutine != null) { StopCoroutine(hintCoroutine); hintCoroutine = null; }
        transform.localScale = Vector3.one;
    }

    IEnumerator HintLoop()
    {
        // Tile hafifçe öne gelsin (sibling order)
        transform.SetAsLastSibling();
        float speed = 1.4f;
        Vector3 s0 = Vector3.one;
        Vector3 s1 = Vector3.one * 1.18f;
        while (true)
        {
            // Büyü
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                transform.localScale = Vector3.Lerp(s0, s1, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            // Küçül
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * speed;
                transform.localScale = Vector3.Lerp(s1, s0, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            // Kısa bekleme — nefes efekti
            yield return new WaitForSeconds(0.15f);
        }
    }

    // ── DESTROY ANİMASYONU + PATLAMA EFEKTLERİ ──────────────────────
    public IEnumerator PlayDestroyAnimation()
    {
        StopPulse();
        StopHint();
        Deselect();

        // Patlama efektlerini HEMEN başlat (animasyonla eş zamanlı)
        SpawnParticles();
        StartCoroutine(SpawnShockwave());

        // 1. Flash: beyazlaşır + büyür
        float flashDur = 0.12f, elapsed = 0f;
        Color baseColor = image.color;
        while (elapsed < flashDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDur);
            image.color = Color.Lerp(baseColor, Color.white, t);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.25f, t);
            yield return null;
        }

        // 2. Burst: küçülür ve solar
        elapsed = 0f;
        float popDur = 0.22f;
        while (elapsed < popDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDur);
            transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 0f, Mathf.SmoothStep(0, 1, t));
            image.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        transform.localScale = Vector3.zero;
    }

    // ── PATLAMA PARTİKÜLLERİ ────────────────────────────────────────
    void SpawnParticles()
    {
        // Tile rengine göre renk seç
        Color pColor = particleColors[tileType % particleColors.Length];

        int count = 6;
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + Random.Range(-15f, 15f);
            float speed = Random.Range(90f, 160f);
            float size = Random.Range(8f, 16f);
            StartCoroutine(ParticleRoutine(angle, speed, size, pColor));
        }
    }

    IEnumerator ParticleRoutine(float angleDeg, float speed, float size, Color color)
    {
        // Parent canvas'ı bul (particles board'un altına değil canvas'a eklensin)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        GameObject go = new GameObject("Particle");
        go.transform.SetParent(canvas.transform, false);

        RectTransform prt = go.AddComponent<RectTransform>();
        prt.sizeDelta = new Vector2(size, size);

        // Başlangıç pozisyonu: tile'ın dünya → canvas lokal pozisyonu
        RectTransform selfRt = GetComponent<RectTransform>();
        Vector3 worldPos = selfRt.position;
        prt.position = worldPos;

        Image pImg = go.AddComponent<Image>();
        pImg.color = color;
        pImg.raycastTarget = false;

        // Yuvarlak yapmak için: eğer specialSprites[0] varsa kullan, yoksa kare
        if (specialSprites != null && specialSprites.Length > 0 && specialSprites[0] != null)
            pImg.sprite = specialSprites[0];

        // Hareket yönü
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        float dur = Random.Range(0.35f, 0.55f);
        float elapsed = 0f;
        Vector3 startPos = prt.position;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            // İleri gider + yerçekimi
            prt.position = startPos + new Vector3(
                dir.x * speed * elapsed,
                dir.y * speed * elapsed - 120f * elapsed * elapsed,
                0
            );
            // Küçülür ve solar
            float scale = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0.3f, 1f, t));
            prt.localScale = Vector3.one * scale;
            pImg.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        Destroy(go);
    }

    // ── SHOCKWAVE (dışa doğru büyüyen halka) ────────────────────────
    IEnumerator SpawnShockwave()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        GameObject go = new GameObject("Shockwave");
        go.transform.SetParent(canvas.transform, false);

        RectTransform srt = go.AddComponent<RectTransform>();
        srt.sizeDelta = Vector2.one * 10f;
        srt.position = GetComponent<RectTransform>().position;

        Image sImg = go.AddComponent<Image>();
        Color ringColor = particleColors[tileType % particleColors.Length];
        sImg.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0.7f);
        sImg.raycastTarget = false;

        float dur = 0.3f, elapsed = 0f;
        float tileW = GetComponent<RectTransform>().rect.width;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float s = Mathf.Lerp(0.3f, tileW * 1.8f / 10f, Mathf.SmoothStep(0, 1, t));
            srt.localScale = Vector3.one * s;
            sImg.color = new Color(ringColor.r, ringColor.g, ringColor.b, Mathf.Lerp(0.7f, 0f, t));
            yield return null;
        }

        Destroy(go);
    }

    // ── SPECIAL POP ──────────────────────────────────────────────────
    IEnumerator SpecialPopAnim()
    {
        float dur = 0.2f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dur;
            transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.25f);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    // ── ICE ──────────────────────────────────────────────────────────
    public void SetIce(int hp)
    {
        hasIce = true;
        iceHitPoints = hp;
        if (iceOverlay == null)
            iceOverlay = transform.Find("IceOverlay")?.GetComponent<Image>();
        if (iceOverlay != null)
        {
            iceOverlay.gameObject.SetActive(true);
            iceOverlay.color = new Color(1f, 1f, 1f, 0.6f);
            iceOverlay.transform.SetAsLastSibling();
        }
    }

    public void UpdateIceVisual()
    {
        if (iceOverlay != null)
            iceOverlay.color = new Color(1f, 1f, 1f, iceHitPoints == 1 ? 0.3f : 0.6f);
    }

    public void ClearIce()
    {
        hasIce = false;
        iceHitPoints = 0;
        if (iceOverlay != null) iceOverlay.gameObject.SetActive(false);
    }

    // ── HIGHLIGHT ────────────────────────────────────────────────────
    public void SetHighlight(bool highlight)
    {
        if (image == null) image = GetComponent<Image>();
        image.color = highlight ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);
        if (iceOverlay != null)
            iceOverlay.color = highlight ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);
        if (highlight) { transform.SetAsLastSibling(); StartPulse(); }
        else StopPulse();
    }

    public void StartPulse()
    {
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseLoop());
    }

    public void StopPulse()
    {
        if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
        transform.localScale = Vector3.one;
    }

    IEnumerator PulseLoop()
    {
        Vector3 s0 = Vector3.one, s1 = Vector3.one * 1.12f;
        float speed = 1.8f;
        while (true)
        {
            float t = 0;
            while (t < 1f) { t += Time.deltaTime * speed; transform.localScale = Vector3.Lerp(s0, s1, Mathf.SmoothStep(0, 1, t)); yield return null; }
            t = 0;
            while (t < 1f) { t += Time.deltaTime * speed; transform.localScale = Vector3.Lerp(s1, s0, Mathf.SmoothStep(0, 1, t)); yield return null; }
        }
    }

    // ── DRAG ─────────────────────────────────────────────────────────
    Vector2 dragStartPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (board != null && board.inputLocked) return;
        // Önceki seçimi kapat
        if (currentSelected != null && currentSelected != this)
            currentSelected.Deselect();
        // Kendini seç
        Select();
    }

    public void Select()
    {
        currentSelected = this;
        if (selectionBorder == null) selectionBorder = CreateSelectionBorder();
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(true);
            selectionBorder.transform.SetAsLastSibling();
            selectionBorder.transform.localRotation = Quaternion.identity;
            selectionBorder.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    public void Deselect()
    {
        if (currentSelected == this) currentSelected = null;
        if (selectionCoroutine != null) { StopCoroutine(selectionCoroutine); selectionCoroutine = null; }
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(false);
            selectionBorder.transform.localScale = Vector3.one;
            selectionBorder.color = Color.white;
        }
    }

    IEnumerator SelectionAnimation() { yield break; }

    Image CreateSelectionBorder()
    {
        GameObject go = new GameObject("SelectionBorder");
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        // Tile'dan biraz büyük
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-4f, -4f);
        rt.offsetMax = new Vector2(4f, 4f);
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(1f, 1f, 1f, 0.9f);
        go.transform.SetAsLastSibling();
        return img;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (board != null && board.inputLocked) return;
        dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (board != null && board.inputLocked) return;
        Deselect();
        if (currentSelected != null) { currentSelected.Deselect(); }
        Vector2 dir = eventData.position - dragStartPos;
        if (dir.magnitude < 50f) return;
        dir.Normalize();
        int tx = x, ty = y;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) tx += dir.x > 0 ? 1 : -1;
        else ty += dir.y > 0 ? 1 : -1;
        if (board != null && tx >= 0 && tx < board.width && ty >= 0 && ty < board.height)
            board.SwapTiles(this, board.tiles[tx, ty]);
    }
}