using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    
    public static TutorialManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    
    [HideInInspector] public bool isTutorialActive = false;

    
    [Header("UI — Overlay")]
    public CanvasGroup dimOverlay;

    [Header("UI — Dialog")]
    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText;
    public Button nextButton;

    [Header("UI — El & Ok")]
    public RectTransform handIcon;
    public RectTransform arrowIcon;

    [Header("UI — Highlight")]
    public GameObject highlightPrefab;

    [Header("References")]
    public BoardManager boardManager;
    public RectTransform boardRect;

    
    private class TutorialStep
    {
        public string text;
        public bool needsSwap;
        public Vector2Int src;
        public Vector2Int dst;
    }

    private List<TutorialStep> steps = new List<TutorialStep>();
    private bool waitingForInput = false;
    private List<GameObject> highlights = new List<GameObject>();
    private Tile lockedSrc = null;
    private Tile lockedDst = null;
    private Coroutine handAnimCoroutine = null;

   
    public void StartTutorial()
    {
        if (PlayerPrefs.GetInt("TutorialDone_v1", 0) == 1) return;
        isTutorialActive = true;
        BuildSteps();
        StartCoroutine(RunTutorial());
    }

    void BuildSteps()
    {
        steps.Clear();

        steps.Add(new TutorialStep
        {
            text = "Hos geldin! Bu atolyede maceracilarin\nekipmanlarini tamir ediyoruz!",
            needsSwap = false
        });

        steps.Add(new TutorialStep
        {
            text = "Tahtada renkli taslar var.\nAyni renkten 3'unu yan yana getirince\npatliyorlar!",
            needsSwap = false
        });

        
        steps.Add(new TutorialStep
        {
            text = "Dene! Mavi tasi saga surukle\nve 3'lu eslestir!",
            needsSwap = true,
            src = new Vector2Int(2, 2),
            dst = new Vector2Int(3, 2)
        });

        steps.Add(new TutorialStep
        {
            text = "Harika! Taslar patladi!\nMusterinin hedefi dolmaya basladi.",
            needsSwap = false
        });

        steps.Add(new TutorialStep
        {
            text = "Dikkat: sinirli hamle hakkýn var.\nHer hamleni dikkatlice kullan!",
            needsSwap = false
        });

        steps.Add(new TutorialStep
        {
            text = "Artik hazirsin!\nMusteriyi mutlu et, ekipman tamir edildi!",
            needsSwap = false
        });
    }

    IEnumerator RunTutorial()
    {
        yield return new WaitForSeconds(0.5f);
        if (boardManager != null) boardManager.SetInputLocked(true);
        ShowDim(true);

        foreach (var step in steps)
            yield return StartCoroutine(ExecuteStep(step));

        EndTutorial();
    }

    IEnumerator ExecuteStep(TutorialStep step)
    {
        ClearHighlights();
        HideHandArrow();
        ShowDialog(step.text);
        waitingForInput = true;

        if (step.needsSwap)
        {
            HighlightTile(step.src);
            HighlightTile(step.dst);

            Vector2 srcPos = TileAnchoredPos(step.src);
            Vector2 dstPos = TileAnchoredPos(step.dst);
            ShowHand(srcPos, dstPos);
            ShowArrow(srcPos, dstPos);

            if (nextButton != null) nextButton.interactable = false;

            lockedSrc = boardManager != null ? boardManager.GetTile(step.src.x, step.src.y) : null;
            lockedDst = boardManager != null ? boardManager.GetTile(step.dst.x, step.dst.y) : null;

            if (boardManager != null) boardManager.SetInputLocked(false);

            while (waitingForInput) yield return null;

            if (boardManager != null) boardManager.SetInputLocked(true);
            HideHandArrow();
            ClearHighlights();
            if (nextButton != null) nextButton.interactable = true;
            yield return new WaitForSeconds(1.4f);
            HideDialog();
        }
        else
        {
            if (nextButton != null) nextButton.interactable = true;
            while (waitingForInput) yield return null;
            HideDialog();
        }

        yield return new WaitForSeconds(0.25f);
    }

    
    public bool ValidateSwap(Tile a, Tile b)
    {
        if (!isTutorialActive) return true;
        if (lockedSrc == null) return true;

        bool correct = (a == lockedSrc && b == lockedDst) ||
                       (a == lockedDst && b == lockedSrc);

        if (correct)
        {
            waitingForInput = false;
            lockedSrc = null;
            lockedDst = null;
        }
        else
        {
            StartCoroutine(ShakeTile(a));
        }

        return correct;
    }

    
    public void OnGoalCollected() { }

    void EndTutorial()
    {
        isTutorialActive = false;
        ClearHighlights();
        HideHandArrow();
        HideDialog();
        ShowDim(false);
        if (boardManager != null) boardManager.SetInputLocked(false);
        PlayerPrefs.SetInt("TutorialDone_v1", 1);
        PlayerPrefs.Save();
    }

    
    void ShowDialog(string text)
    {
        if (dialogPanel != null) dialogPanel.SetActive(true);
        if (dialogText != null) dialogText.text = text;
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    void HideDialog()
    {
        if (dialogPanel != null) dialogPanel.SetActive(false);
    }

    public void OnNextClicked()
    {
        waitingForInput = false;
    }

    
    void ShowDim(bool on)
    {
        if (dimOverlay == null) return;
        dimOverlay.gameObject.SetActive(on);
        dimOverlay.alpha = on ? 0.55f : 0f;
    }

    
    Vector2 TileAnchoredPos(Vector2Int grid)
    {
        if (boardManager == null) return Vector2.zero;
        Tile tile = boardManager.GetTile(grid.x, grid.y);
        if (tile == null) return Vector2.zero;
        RectTransform trt = tile.GetComponent<RectTransform>();
        if (trt == null) return Vector2.zero;
        Vector2 boardOffset = (boardRect != null) ? boardRect.anchoredPosition : Vector2.zero;
        return trt.anchoredPosition + boardOffset;
    }

    
    void HighlightTile(Vector2Int grid)
    {
        if (highlightPrefab == null || boardManager == null) return;
        Tile tile = boardManager.GetTile(grid.x, grid.y);
        if (tile == null) return;

        GameObject hl = Instantiate(highlightPrefab, tile.transform.parent);
        RectTransform hrt = hl.GetComponent<RectTransform>();
        if (hrt != null)
        {
            hrt.anchoredPosition = tile.GetComponent<RectTransform>().anchoredPosition;
            hrt.SetAsLastSibling();
        }
        highlights.Add(hl);
        StartCoroutine(PulseScale(hl.transform));
    }

    void ClearHighlights()
    {
        foreach (var h in highlights)
            if (h != null) Destroy(h);
        highlights.Clear();
    }

    
    void ShowHand(Vector2 from, Vector2 to)
    {
        if (handIcon == null) return;
        handIcon.gameObject.SetActive(true);
        handIcon.anchoredPosition = from;
        if (handAnimCoroutine != null) StopCoroutine(handAnimCoroutine);
        handAnimCoroutine = StartCoroutine(AnimateHand(from, to));
    }

    void ShowArrow(Vector2 from, Vector2 to)
    {
        if (arrowIcon == null) return;
        arrowIcon.gameObject.SetActive(true);
        arrowIcon.anchoredPosition = (from + to) / 2f;
        Vector2 dir = (to - from).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowIcon.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void HideHandArrow()
    {
        if (handAnimCoroutine != null) { StopCoroutine(handAnimCoroutine); handAnimCoroutine = null; }
        if (handIcon != null) handIcon.gameObject.SetActive(false);
        if (arrowIcon != null) arrowIcon.gameObject.SetActive(false);
    }

    
    IEnumerator AnimateHand(Vector2 from, Vector2 to)
    {
        while (handIcon != null && handIcon.gameObject.activeSelf)
        {
            float t = 0f;
            while (t < 1f)
            {
                if (handIcon == null || !handIcon.gameObject.activeSelf) yield break;
                t += Time.deltaTime * 1.8f;
                handIcon.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }
            yield return new WaitForSeconds(0.15f);
            if (handIcon != null) handIcon.anchoredPosition = from;
            yield return new WaitForSeconds(0.35f);
        }
    }

    IEnumerator PulseScale(Transform t)
    {
        Vector3 baseScale = t != null ? t.localScale : Vector3.one;
        while (t != null && t.gameObject != null)
        {
            float s = 0.88f + 0.22f * Mathf.PingPong(Time.time * 2.2f, 1f);
            t.localScale = baseScale * s;
            yield return null;
        }
    }

    IEnumerator ShakeTile(Tile tile)
    {
        if (tile == null) yield break;
        RectTransform rt = tile.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector2 orig = rt.anchoredPosition;
        float dur = 0.32f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed * 55f) * 9f * (1f - elapsed / dur);
            rt.anchoredPosition = orig + new Vector2(x, 0f);
            yield return null;
        }
        rt.anchoredPosition = orig;
    }
}
