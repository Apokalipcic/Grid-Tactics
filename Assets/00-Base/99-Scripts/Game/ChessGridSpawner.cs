using UnityEngine;
using System.Collections;

public class ChessGridSpawner : MonoBehaviour
{
    public int gridSizeX = 8;
    public int gridSizeY = 8;
    public float cellOffset = 1f;
    public GameObject prefabWhite;
    public GameObject prefabBlack;
    public float spawnDuration = 2f;

    private GameObject[,] grid;

    void Start()
    {
        grid = new GameObject[gridSizeX, gridSizeY];
        StartCoroutine(SpawnGrid());
    }

    IEnumerator SpawnGrid()
    {
        int totalCells = gridSizeX * gridSizeY;
        float delayBetweenSpawns = spawnDuration / totalCells;

        for (int y = 0; y < gridSizeY; y++)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                SpawnCell(x, y);
                yield return new WaitForSeconds(delayBetweenSpawns);

                // Spawn next row's cell if it's not the last row
                if (y < gridSizeY - 1)
                {
                    SpawnCell(x, y + 1);
                    yield return new WaitForSeconds(delayBetweenSpawns);
                }
            }
        }

        CenterCamera();
    }

    void SpawnCell(int x, int y)
    {
        Vector3 position = new Vector3(x * cellOffset, 0, y * cellOffset);
        GameObject prefabToSpawn = ((x + y) % 2 == 0) ? prefabWhite : prefabBlack;
        grid[x, y] = Instantiate(prefabToSpawn, position, Quaternion.identity);
        grid[x, y].transform.SetParent(transform);
    }

    void CenterCamera()
    {
        Vector3 centerPosition = new Vector3((gridSizeX - 1) * cellOffset / 2f, 0, (gridSizeY - 1) * cellOffset / 2f);
        Camera.main.transform.position = centerPosition + new Vector3(0, 10, -10); // Adjust these values as needed
        Camera.main.transform.LookAt(centerPosition);
    }
}