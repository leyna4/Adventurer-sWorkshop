using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class OrderUI : MonoBehaviour
{
    public BoardManager board;

    public TextMeshProUGUI movesText;

    public Transform goalContainer;
    public GameObject goalItemPrefab;

    List<GoalItemUI> goalUIs = new List<GoalItemUI>();

    void Start()
    {
        SetupGoals();
    }

    void Update()
    {
        UpdateMoves();
        UpdateGoals();
    }

    void SetupGoals()
    {
        foreach (Transform child in goalContainer)
            Destroy(child.gameObject);

        goalUIs.Clear();

        foreach (var goal in board.goals)
        {
            GameObject item = Instantiate(goalItemPrefab, goalContainer);
            GoalItemUI ui = item.GetComponent<GoalItemUI>();
            ui.Setup(goal);
            goalUIs.Add(ui);
        }
    }

    void UpdateMoves()
    {
        movesText.text = $"Moves: {board.movesLeft}";
    }

    void UpdateGoals()
    {
        foreach (var ui in goalUIs)
            ui.Refresh();
    }
}
