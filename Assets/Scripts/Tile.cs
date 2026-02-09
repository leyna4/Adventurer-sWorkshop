using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public int tileType;
    public Image image;

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
            case 4: return new Color(0.7f, 0.3f, 0.9f); // mor
            default: return Color.white;
        }
    }
}
