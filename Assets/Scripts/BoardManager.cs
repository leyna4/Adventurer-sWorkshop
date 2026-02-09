using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public Tile[,] tiles;

    public int width = 7;
    public int height = 7;

    public GameObject tilePrefab;
    public float tileSize = 70f;

    void Start()
    {
        GenerateBoard();
    }

    void GenerateBoard()
    {
        float offsetX = (width - 1) * tileSize / 2f;
        float offsetY = (height - 1) * tileSize / 2f;

        tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject tile = Instantiate(tilePrefab, transform);
                Tile tileScript = tile.GetComponent<Tile>();

                int randomType = Random.Range(0, 5);
                tileScript.SetType(randomType);

                tile.transform.localPosition = new Vector3(
                    x * tileSize - offsetX,
                    y * tileSize - offsetY,
                    0
                );

                tiles[x, y] = tileScript;
            }
        }

        CheckMatches();
    }

    void CheckMatches()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile current = tiles[x, y];
                if (current == null) continue;

                
                if (x <= width - 3)
                {
                    if (tiles[x + 1, y].tileType == current.tileType &&
                        tiles[x + 2, y].tileType == current.tileType)
                    {
                        Debug.Log("Horizontal Match at: " + x + "," + y);
                    }
                }

                
                if (y <= height - 3)
                {
                    if (tiles[x, y + 1].tileType == current.tileType &&
                        tiles[x, y + 2].tileType == current.tileType)
                    {
                        Debug.Log("Vertical Match at: " + x + "," + y);
                    }
                }
            }
        }
    }
}
