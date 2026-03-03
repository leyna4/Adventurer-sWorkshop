using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

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

    public Sprite[] specialSprites;   // [0]=RowClear overlay, [1]=ColumnClear overlay
    public Sprite[] tileSprites;

    private Coroutine pulseCoroutine = null;

    void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();

        if (iceOverlay == null)
            iceOverlay = transform.Find("IceOverlay")?.GetComponent<Image>();

        if (iceOverlay != null)
            iceOverlay.raycastTarget = false;
    }

    public void SetType(int type)
    {
        tileType = type;
        specialType = SpecialType.None;

        if (tileSprites != null && type < tileSprites.Length)
            image.sprite = tileSprites[type];

        // Special overlay varsa kapat (normal tile'da gözükmesin)
        SetSpecialOverlayActive(false);
    }

    // ?????????????????????????????????????????????????????????????????
    // DÜZELTME: colorType parametresi ile rengi koruyarak special göster
    // ?????????????????????????????????????????????????????????????????
    public void SetSpecialType(SpecialType st, int colorType = -1)
    {
        specialType = st;

        // Renk bilgisi verilmiþse güncelle
        if (colorType >= 0)
            tileType = colorType;

        if (st == SpecialType.None)
        {
            // Normal tile görselini geri yükle
            if (tileSprites != null && tileType < tileSprites.Length)
                image.sprite = tileSprites[tileType];

            SetSpecialOverlayActive(false);
            return;
        }

        // 1. Base: Tile'ýn kendi renk sprite'ý
        if (tileSprites != null && tileType < tileSprites.Length)
            image.sprite = tileSprites[tileType];

        // 2. Üst katman: RowClear / ColumnClear sembolü
        UpdateSpecialOverlay(st);
    }

    // Special sembolünü ayrý SpecialOverlay child Image üzerinden göster
    // Bu sayede tile rengi korunur, sembol sadece üste biner
    void UpdateSpecialOverlay(SpecialType st)
    {
        Transform overlayT = transform.Find("SpecialOverlay");

        // SpecialOverlay yoksa fallback: direkt image.sprite'ý deðiþtir (eski davranýþ)
        if (overlayT == null)
        {
            if (specialSprites == null) return;

            if (st == SpecialType.RowClear && specialSprites.Length > 0 && specialSprites[0] != null)
                image.sprite = specialSprites[0];
            else if (st == SpecialType.ColumnClear && specialSprites.Length > 1 && specialSprites[1] != null)
                image.sprite = specialSprites[1];
            return;
        }

        Image overlay = overlayT.GetComponent<Image>();
        if (overlay == null) return;

        overlay.gameObject.SetActive(true);
        overlay.raycastTarget = false;
        overlay.transform.SetAsLastSibling();

        if (st == SpecialType.RowClear && specialSprites != null && specialSprites.Length > 0 && specialSprites[0] != null)
            overlay.sprite = specialSprites[0];
        else if (st == SpecialType.ColumnClear && specialSprites != null && specialSprites.Length > 1 && specialSprites[1] != null)
            overlay.sprite = specialSprites[1];
    }

    void SetSpecialOverlayActive(bool active)
    {
        Transform overlayT = transform.Find("SpecialOverlay");
        if (overlayT != null)
            overlayT.gameObject.SetActive(active);
    }

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
        {
            float alpha = (iceHitPoints == 1) ? 0.3f : 0.6f;
            iceOverlay.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    public void ClearIce()
    {
        hasIce = false;
        iceHitPoints = 0;

        if (iceOverlay != null)
            iceOverlay.gameObject.SetActive(false);
    }

    public void SetHighlight(bool highlight)
    {
        if (image == null) image = GetComponent<Image>();

        image.color = highlight ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);

        if (iceOverlay != null)
            iceOverlay.color = highlight ? Color.white : new Color(0.25f, 0.25f, 0.25f, 1f);

        if (highlight)
        {
            transform.SetAsLastSibling();
            StartPulse();
        }
        else
        {
            StopPulse();
        }
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
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * 1.12f;
        float speed = 1.8f;

        while (true)
        {
            float t = 0;
            while (t < 1.0f)
            {
                t += Time.deltaTime * speed;
                transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            t = 0;
            while (t < 1.0f)
            {
                t += Time.deltaTime * speed;
                transform.localScale = Vector3.Lerp(endScale, startScale, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }
    }

    Vector2 dragStartPos;

    public void OnPointerDown(PointerEventData eventData) { }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (board != null && board.inputLocked) return;
        dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (board != null && board.inputLocked) return;

        Vector2 dir = eventData.position - dragStartPos;
        if (dir.magnitude < 50f) return;

        dir.Normalize();
        int tx = x, ty = y;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) tx += dir.x > 0 ? 1 : -1;
        else ty += dir.y > 0 ? 1 : -1;

        if (board != null && tx >= 0 && tx < board.width && ty >= 0 && ty < board.height)
            board.SwapTiles(this, board.tiles[tx, ty]);
    }

    public IEnumerator PlayDestroyAnimation()
    {
        StopPulse();
        float dur = 0.15f, elapsed = 0f;
        Vector3 start = transform.localScale;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, elapsed / dur);
            yield return null;
        }
    }
}