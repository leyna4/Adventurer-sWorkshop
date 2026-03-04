using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

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

    [Header("Dialogue Buttons")]
    public GameObject choiceButtons;
    public Button choiceButton1;
    public Button choiceButton2;
    private bool playerAnswered = false;

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

    [Header("Pause System")]
    public GameObject pausePanel;
    private bool isPaused = false;

    [Header("Coin UI")]
    public int playerCoins = 0;
    public TextMeshProUGUI coinText;
    private HashSet<int> rewardedLevels = new HashSet<int>();

    
    [Header("Tutorial")]
    public TutorialManager tutorialManager;

    void Start()
    {
        UpdateCoinUI();
        retryButton.onClick.AddListener(RestartLevel);
        nextLevelButton.onClick.AddListener(NextLevel);
        loseRetryButton.onClick.AddListener(RestartLevel);
        choiceButton1.onClick.AddListener(OnPlayerChoice);
        choiceButton2.onClick.AddListener(OnPlayerChoice);
        StartCoroutine(InitialSetup());
    }

    IEnumerator InitialSetup()
    {
        LoadLevel(currentLevel);
        LoadCustomer(currentLevel - 1);
        yield return new WaitForEndOfFrame();

      
        UpdateGoalUI();

        yield return StartCoroutine(CustomerSequence());

        
        if (currentLevel == 1 && tutorialManager != null)
            tutorialManager.StartTutorial();
    }

    void LoadLevel(int levelIndex)
    {
        if (board == null || levels.Count == 0) return;

        
        if (levelIndex - 1 >= levels.Count) levelIndex = 1;

        LevelData data = levels[levelIndex - 1];
        board.currentLevel = levelIndex;

        
        board.SetGoals(data.goals, data.moveLimit);

       
        board.ResetBoardForNextLevel();

        
        SetupGoalUI();
    }

    public void RestartLevel()
    {
        StopAllCoroutines();
        levelFinished = false;
        Time.timeScale = 1f;
        isPaused = false;
        levelCompletePanel.SetActive(false);
        losePanel.SetActive(false);
        pausePanel.SetActive(false);

        if (tutorialManager != null)
            tutorialManager.ForceReset();

        LoadLevel(currentLevel);
        LoadCustomer(currentLevel - 1);
        StartCoroutine(RestartSequence());
    }

    IEnumerator RestartSequence()
    {
        yield return StartCoroutine(CustomerSequence());

        if (currentLevel == 1 && tutorialManager != null)
            tutorialManager.StartTutorial();
    }

    void NextLevel()
    {
        StopAllCoroutines();
        levelFinished = false;
        levelCompletePanel.SetActive(false);
        currentLevel++;
        if (currentLevel > levels.Count) currentLevel = 1;
        LoadLevel(currentLevel);
        LoadCustomer(currentLevel - 1);
        StartCoroutine(CustomerSequence());
    }

    IEnumerator CustomerSequence()
    {
        playerAnswered = false;

        
        match3Area.SetActive(false);
        specialPanel.SetActive(false);
        customerVisual.SetActive(true);

        if (itemImage != null)
        {
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = brokenItemSprite;
        }

        dialogueBox.SetActive(true);
        choiceButtons.SetActive(false);
        dialogueText.text = customers[currentLevel - 1].intro1;
        yield return new WaitForSecondsRealtime(2f);
        dialogueText.text = customers[currentLevel - 1].intro2;
        yield return new WaitForSecondsRealtime(2f);
        choiceButtons.SetActive(true);

        yield return new WaitUntil(() => playerAnswered);

        
        dialogueBox.SetActive(false);
        choiceButtons.SetActive(false);

        match3Area.SetActive(true);
        specialPanel.SetActive(true); 

        UpdateGoalUI(); 
    }

    public void OnPlayerChoice()
    {
        playerAnswered = true;
    }

    public void OnLevelCompleted()
    {
        if (levelFinished) return;
        levelFinished = true;
        if (board != null) board.HideFeedback();
        StartCoroutine(FinishSequence());
    }

    IEnumerator FinishSequence()
    {
        match3Area.SetActive(false);
        specialPanel.SetActive(false);
        if (itemImage != null) itemImage.sprite = repairedItemSprite;
        yield return new WaitForSeconds(2f);

        if (!rewardedLevels.Contains(currentLevel))
        {
            int bonus = board.movesLeft * 2;
            playerCoins += (20 + bonus);
            rewardedLevels.Add(currentLevel);
            UpdateCoinUI();
        }

        levelCompletePanel.SetActive(true);
    }

    public void OnOutOfMoves()
    {
        if (levelFinished) return;
        levelFinished = true;
        if (board != null) board.HideFeedback();
        losePanel.SetActive(true);
    }

    void SetupGoalUI()
    {
        
        if (goalContainer != null)
        {
            foreach (Transform child in goalContainer) Destroy(child.gameObject);
        }

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
        
        if (activeGoals.Count > 0)
            foreach (var goalUI in activeGoals)
                if (goalUI != null) goalUI.Refresh();
    }

    public void UpdateGoalUI()
    {
        if (specialPanel != null) specialPanel.SetActive(true);
        if (goalContainer != null) goalContainer.gameObject.SetActive(true);

        foreach (var goalUI in activeGoals)
            if (goalUI != null) goalUI.Refresh();
    }

    void LoadCustomer(int index)
    {
        if (customers.Count == 0) return;
        if (index >= customers.Count) index = 0;
        CustomerData data = customers[index];
        brokenItemSprite = data.brokenSprite;
        repairedItemSprite = data.repairedSprite;
        if (customerImage != null) customerImage.sprite = data.customerSprite;
        if (itemImage != null) itemImage.sprite = data.brokenSprite;
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = playerCoins.ToString();
    }

    public void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    void PauseGame()
    {
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }
}