using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GoalItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI amountText;

    BoardManager.GoalData goal;

    public void Setup(BoardManager.GoalData goalData)
    {
        goal = goalData;

        Color tileColor = GetColorForTileType(goal.tileType);
        icon.color = tileColor;

        Refresh();
    }

    public void Refresh()
    {
        amountText.text = $"{goal.collectedAmount} / {goal.targetAmount}";
    }

    Color GetColorForTileType(int tileType)
    {
        switch (tileType)
        {
            case 0: return Color.red;
            case 1: return Color.blue;
            case 2: return Color.green;
            case 3: return Color.yellow;
            case 4: return Color.magenta;
            default: return Color.white;
        }
    }
}
