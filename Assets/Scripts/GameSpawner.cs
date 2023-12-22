using UnityEngine;
using Sirenix.OdinInspector;

public class GameSpawner : SerializedMonoBehaviour
{
    [SerializeField]
    private HexSpawner hexSpawner;
    [SerializeField]
    private EdgeSpawner edgeSpawner;
    [SerializeField]
    private CornerSpawner cornerSpawner;

    [Button("Spawn Game Elements")]
    public void SpawnGameElements()
    {
        ClearGameElements();

        hexSpawner.Spawn();
        edgeSpawner.Spawn();
        cornerSpawner.Spawn();

        AssociateElements();
        AdjustCameraPosition();
    }

    [Button("Clear Game Elements")]
    public void ClearGameElements()
    {

    }

    private void AssociateElements()
    {
        // Implement logic to associate Hexes with their respective Edges and Corners
    }

    private void AdjustCameraPosition()
    {
        // Logic to adjust the camera to fit the game board
    }

    // Additional methods for game setup and management...
}
