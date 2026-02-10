using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class Tile : MonoBehaviour, IPointerClickHandler
{
    public int tileType;
    public Image image;

    
    public int x;
    public int y;
    public BoardManager board;
    public bool isObstacle = false;
    public int hitPoints = 0;
    public bool isCollectible = true;


    public void SetObstacle(int hits)
    {
        isObstacle = true;
        hitPoints = hits;
        image.color = Color.gray;
    }

    public void SetType(int type)
    {
        tileType = type;
        image.color = GetColorByType(type);
    }

    Color GetColorByType(int type)
    {
        switch (type)
        {
            case 0: return Color.red;
            case 1: return Color.blue;
            case 2: return Color.green;
            case 3: return Color.yellow;
            case 4: return new Color(0.7f, 0.3f, 0.9f); 
            default: return Color.white;
        }
    }

    
    public void OnPointerClick(PointerEventData eventData)
    {
        board.SelectTile(this);
    }

    public Color GetTileColor()
    {
        return image.color;
    }

}
