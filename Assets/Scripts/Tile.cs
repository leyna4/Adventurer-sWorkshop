using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    public bool hasIce;
    public int iceHitPoints;
    public Image iceOverlay;

    public bool isSpecialItem = false;

    public Sprite[] tileSprites;
    public bool iceJustBroken = false;


    Vector2 dragStartPos;

    void Awake()
    {
        hasIce = false;
        iceJustBroken = false;

        if (iceOverlay != null)
            iceOverlay.gameObject.SetActive(false);
    }

    public void SetIce(int hitPoints)
    {
        hasIce = true;
        iceHitPoints = hitPoints;

        if (iceOverlay != null)
        {
            iceOverlay.transform.SetAsLastSibling();
            iceOverlay.color = Color.white;
            iceOverlay.gameObject.SetActive(true);
        }
    }




    public void SetType(int type)
    {
        tileType = type;
        image.sprite = tileSprites[type];
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("Pointer Down çalýþtý");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Drag baþladý");
        dragStartPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Drag bitti");

        Vector2 dragEndPos = eventData.position;
        Vector2 direction = dragEndPos - dragStartPos;

        if (direction.magnitude < 50f)
            return;

        direction.Normalize();

        int targetX = x;
        int targetY = y;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            targetX += direction.x > 0 ? 1 : -1;
        else
            targetY += direction.y > 0 ? 1 : -1;

        if (targetX >= 0 && targetX < board.width &&
            targetY >= 0 && targetY < board.height)
        {
            board.SwapTiles(this, board.tiles[targetX, targetY]);
        }
    }
}
