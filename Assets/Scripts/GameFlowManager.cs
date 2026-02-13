using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class GameFlowManager : MonoBehaviour
{
    [Header("References")]
    public BoardManager board;

    public GameObject match3Area;
    public GameObject specialPanel;

    [Header("Goal UI")]
    public Transform goalContainer;
    public GoalItemUI goalItemPrefab;

    List<GoalItemUI> activeGoals = new List<GoalItemUI>();

    [Header("Customer Area")]
    public GameObject customerVisual;
    public Image itemImage;
    public TextMeshProUGUI dialogueText;

    [Header("Customer Data")]
    public Sprite brokenItemSprite;
    public Sprite repairedItemSprite;

    public GameObject dialogueBox;

    [Header("Dialogue Choices UI")]
    public GameObject choicesContainer;
    public Button choiceButton1;
    public Button choiceButton2;

    [Header("Dialogue Texts")]
    [TextArea] public string introLine1;
    [TextArea] public string introLine2;

    [TextArea] public string choice1Text;
    [TextArea] public string choice2Text;

    [TextArea] public string response1;
    [TextArea] public string response2;

    [Header("Coin System")]
    public int playerCoins = 0;
    public int levelReward = 50;

    [Header("Coin UI")]
    public TextMeshProUGUI coinText;   // Üstteki coin yazýsý

    void Start()
    {
        UpdateCoinUI(); // Baþlangýçta coin göster
        StartCoroutine(CustomerSequence());
    }

    IEnumerator CustomerSequence()
    {
        match3Area.SetActive(false);
        specialPanel.SetActive(false);

        customerVisual.SetActive(true);

        dialogueBox.SetActive(true);
        choicesContainer.SetActive(false);

        dialogueText.text = introLine1;
        yield return new WaitForSeconds(2f);

        dialogueText.text = introLine2;
        yield return new WaitForSeconds(2f);

        ShowChoices();
    }

    void ShowChoices()
    {
        choicesContainer.SetActive(true);

        choiceButton1.GetComponentInChildren<TextMeshProUGUI>().text = choice1Text;
        choiceButton2.GetComponentInChildren<TextMeshProUGUI>().text = choice2Text;

        choiceButton1.onClick.RemoveAllListeners();
        choiceButton2.onClick.RemoveAllListeners();

        choiceButton1.onClick.AddListener(() => OnChoiceSelected(1));
        choiceButton2.onClick.AddListener(() => OnChoiceSelected(2));
    }

    void OnChoiceSelected(int choice)
    {
        choicesContainer.SetActive(false);

        if (choice == 1)
            dialogueText.text = response1;
        else
            dialogueText.text = response2;

        StartCoroutine(StartGameplayAfterDialogue());
    }

    IEnumerator StartGameplayAfterDialogue()
    {
        yield return new WaitForSeconds(2f);

        itemImage.sprite = brokenItemSprite;
        itemImage.gameObject.SetActive(true);

        HideDialogue();

        match3Area.SetActive(true);
        specialPanel.SetActive(true);

        SetupGoalUI();
    }

    void SetupGoalUI()
    {
        foreach (Transform child in goalContainer)
            Destroy(child.gameObject);

        activeGoals.Clear();

        foreach (var goal in board.goals)
        {
            GoalItemUI ui = Instantiate(goalItemPrefab, goalContainer);
            ui.Setup(goal);
            activeGoals.Add(ui);
        }
    }

    void Update()
    {
        foreach (var goalUI in activeGoals)
        {
            goalUI.Refresh();
        }
    }

    public void OnLevelCompleted()
    {
        StartCoroutine(FinishSequence());
    }

    IEnumerator FinishSequence()
    {
        match3Area.SetActive(false);
        specialPanel.SetActive(false);

        itemImage.sprite = repairedItemSprite;

        ShowDialogue("Harika olmuþ! Çok teþekkür ederim!");
        yield return new WaitForSeconds(2f);

        HideDialogue();

        // Coin ekle
        AddCoins(levelReward);

        yield return new WaitForSeconds(1f);

        customerVisual.SetActive(false);
        itemImage.gameObject.SetActive(false);
    }

    void AddCoins(int amount)
    {
        playerCoins += amount;
        UpdateCoinUI();
        Debug.Log("Toplam Coin: " + playerCoins);
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = playerCoins.ToString();
    }

    void ShowDialogue(string message)
    {
        dialogueBox.SetActive(true);
        dialogueText.text = message;
    }

    void HideDialogue()
    {
        dialogueBox.SetActive(false);
    }

    public void OnOutOfMoves()
    {
        match3Area.SetActive(false);
        ShowDialogue("Hamlelerin bitti!");
    }
}
