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
        
        if (board != null && board.goals != null && board.goals.Count > 0)
            SetupGoals();
    }

    void Update()
    {
        UpdateMoves();
        UpdateGoals();
    }

    
    public void RefreshGoals()
    {
        SetupGoals();
    }

    void SetupGoals()
    {
        foreach (Transform child in goalContainer)
            Destroy(child.gameObject);
        goalUIs.Clear();

        if (board == null || board.goals == null) return;

        foreach (var goal in board.goals)
        {
            GameObject item = Instantiate(goalItemPrefab, goalContainer);
            GoalItemUI ui = item.GetComponent<GoalItemUI>();
            if (ui == null) continue;
            ui.board = board;
            ui.Setup(goal);
            goalUIs.Add(ui);
        }
    }

    void UpdateMoves()
    {
        if (movesText != null && board != null)
            movesText.text = $"Moves: {board.movesLeft}";
    }

    void UpdateGoals()
    {
        foreach (var ui in goalUIs)
            if (ui != null) ui.Refresh();
    }
}
