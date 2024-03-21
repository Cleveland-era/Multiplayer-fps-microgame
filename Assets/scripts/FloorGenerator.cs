using UnityEngine;
using Unity.Netcode;

// ÊµÀý»¯µØ°å
public class FloorGenerator : MonoBehaviour
{
    public GameObject floorTilePrefab;
    public int gridSize = 10;

    void Start()
    {
        GenerateFloor();
    }

    void GenerateFloor()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            float startOffset = (gridSize - 1) * 10 * 0.5f;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector3 position = new Vector3(x * 10 - startOffset, 0, z * 10 - startOffset);
                    var floorTileObject = Instantiate(floorTilePrefab, position, Quaternion.identity);
                    var networkObject = floorTileObject.GetComponent<NetworkObject>();
                    if (networkObject != null)
                    {
                        networkObject.Spawn(); 
                    }
                }
            }
        }
    }
}


