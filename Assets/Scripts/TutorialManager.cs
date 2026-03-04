using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public BoardManager boardManager;
    public CanvasGroup dimOverlay;
    public bool isTutorialActive = false;
    private int step = 0;

    
    private Vector2Int[,] steps = {
        { new Vector2Int(1,0), new Vector2Int(0,0) }, 
        { new Vector2Int(3,2), new Vector2Int(3,3) }  
    };

    public void StartTutorial()
    {
        isTutorialActive = true;
        step = 0;
        ApplyStepHighlight();
    }

    void ApplyStepHighlight()
    {
        if (step >= 2) { EndTutorial(); return; }

       
        foreach (var t in boardManager.tiles) if (t != null) t.SetHighlight(false);

        
        Vector2Int posA = steps[step, 0];
        Vector2Int posB = steps[step, 1];

        boardManager.GetTile(posA.x, posA.y)?.SetHighlight(true);
        boardManager.GetTile(posB.x, posB.y)?.SetHighlight(true);

       
        if (step == 0)
        {
            boardManager.GetTile(0, 1)?.SetHighlight(true);
            boardManager.GetTile(0, 2)?.SetHighlight(true);
        }
        else
        {
            boardManager.GetTile(4, 3)?.SetHighlight(true);
            boardManager.GetTile(5, 3)?.SetHighlight(true);
        }
    }

    public bool CheckMove(Tile a, Tile b)
    {
        if (!isTutorialActive) return true;
        if (step >= 2) return true;

        Vector2Int targetA = steps[step, 0];
        Vector2Int targetB = steps[step, 1];

        
        bool match = (new Vector2Int(a.x, a.y) == targetA && new Vector2Int(b.x, b.y) == targetB) ||
                     (new Vector2Int(a.x, a.y) == targetB && new Vector2Int(b.x, b.y) == targetA);

        if (match)
        {
            step++;
           
            Invoke("ApplyStepHighlight", 0.6f);
            return true;
        }
        return false; 
    }

    public void ForceReset() { step = 0; isTutorialActive = false; }

    void EndTutorial()
    {
        isTutorialActive = false;

       
        if (boardManager != null && boardManager.tiles != null)
        {
            for (int x = 0; x < boardManager.width; x++)
            {
                for (int y = 0; y < boardManager.height; y++)
                {
                    Tile t = boardManager.tiles[x, y];
                    if (t != null)
                    {
                        t.StopPulse();      
                        t.SetHighlight(true); 
                    }
                }
            }
        }

        if (dimOverlay != null) dimOverlay.gameObject.SetActive(false);
    }
}