using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GoalItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI amountText;

    BoardManager.GoalData goal;
    public BoardManager board; 

    public void Setup(BoardManager.GoalData goalData)
    {
        goal = goalData;

        if (goal.goalSprite != null)
        {
            icon.sprite = goal.goalSprite;
        }

        Refresh();
    }

    public void Refresh()
    {
        if (goal != null)
            amountText.text = $"{goal.collectedAmount} / {goal.targetAmount}";
    }
}
