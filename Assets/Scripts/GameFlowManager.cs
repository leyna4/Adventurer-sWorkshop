using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class GameFlowManager : MonoBehaviour
{
    [Header("Level Complete UI")]
    public GameObject levelCompletePanel;
    public Button retryButton;
    public Button nextLevelButton;

    [Header("Lose UI")]
    public GameObject losePanel;
    public Button loseRetryButton;

    
    public static int currentLevel = 1;

    bool levelFinished = false;

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

    private HashSet<int> rewardedLevels = new HashSet<int>();

    [Header("Coin UI")]
    public TextMeshProUGUI coinText;

    [System.Serializable]
    public class CustomerData
    {
        [TextArea] public string intro1;
        [TextArea] public string intro2;
        public Sprite customerSprite;
        public Sprite brokenSprite;
        public Sprite repairedSprite;
    }

    [Header("Customers")]
    public List<CustomerData> customers = new List<CustomerData>();
    public Image customerImage;

    [System.Serializable]
    public class LevelGoal
    {
        public Sprite goalSprite;
        public int targetAmount;
        public int matchColorID;
    }

    [System.Serializable]
    public class LevelData
    {
        public int moveLimit;
        public List<LevelGoal> goals;
    }

    [Header("Levels")]
    public List<LevelData> levels = new List<LevelData>();
    public int baseReward;
    public int bonusPerMove;


    void Start()
    {
        UpdateCoinUI();

        retryButton.onClick.AddListener(RestartLevel);
        nextLevelButton.onClick.AddListener(NextLevel);
        loseRetryButton.onClick.AddListener(RestartLevel);

        LoadLevel(currentLevel);
        LoadCustomer(currentLevel - 1);

        StartCoroutine(CustomerSequence());
    }

    void LoadLevel(int levelIndex)
    {
        if (board == null) return;
        if (levels.Count == 0) return;

        if (levelIndex - 1 >= levels.Count)
            levelIndex = 1;

        LevelData data = levels[levelIndex - 1];

        board.currentLevel = levelIndex;
        board.SetGoals(data.goals, data.moveLimit);
        board.ResetBoardForNextLevel();

    }

    void LoadCustomer(int index)
    {
        if (customers.Count == 0) return;

        if (index >= customers.Count)
            index = 0;

        CustomerData data = customers[index];

        introLine1 = data.intro1;
        introLine2 = data.intro2;

        brokenItemSprite = data.brokenSprite;
        repairedItemSprite = data.repairedSprite;

        if (customerImage != null)
            customerImage.sprite = data.customerSprite;
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
        dialogueText.text = (choice == 1) ? response1 : response2;
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
            goalUI.Refresh();
    }

    public void OnLevelCompleted()
    {
        if (levelFinished) return;
        levelFinished = true;
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

        yield return StartCoroutine(HideCustomerSequence());

        if (!rewardedLevels.Contains(currentLevel))
        {
            int bonus = board.movesLeft * 2;
            int totalReward = 20 + bonus;

            AddCoins(totalReward);
            rewardedLevels.Add(currentLevel);
        }


        yield return new WaitForSeconds(0.5f);

        levelCompletePanel.SetActive(true);
    }

    public void OnOutOfMoves()
    {
        if (levelFinished) return;

        levelFinished = true;
        StartCoroutine(LoseSequence());
    }

    IEnumerator LoseSequence()
    {
        match3Area.SetActive(false);
        specialPanel.SetActive(false);

        dialogueBox.SetActive(false);
        choicesContainer.SetActive(false);

        yield return StartCoroutine(HideCustomerSequence());

        losePanel.SetActive(true);
    }

    IEnumerator HideCustomerSequence()
    {
        yield return new WaitForSeconds(0.5f);

        if (customerVisual != null)
            customerVisual.SetActive(false);

        if (itemImage != null)
            itemImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);
    }

    void AddCoins(int amount)
    {
        playerCoins += amount;
        UpdateCoinUI();
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = playerCoins.ToString();
    }

    void RestartLevel()
    {
        levelFinished = false;

        levelCompletePanel.SetActive(false);
        losePanel.SetActive(false);

        
        LoadLevel(currentLevel);
        LoadCustomer(currentLevel - 1);

        StartCoroutine(CustomerSequence());
    }

    void NextLevel()
    {
        levelFinished = false;

        levelCompletePanel.SetActive(false);

        currentLevel++;

        if (currentLevel > levels.Count)
            currentLevel = 1;

        LoadLevel(currentLevel);
        LoadCustomer(currentLevel - 1);

        StartCoroutine(CustomerSequence());
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
}
