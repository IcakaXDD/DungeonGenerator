using UnityEngine;

[CreateAssetMenu(fileName = "Wall", menuName = "CreateWallVarient/SpawnWallScriptableObject", order = 1)]
public class WallScriptableObject : ScriptableObject
{
    public GameObject gameObject;
    public int value;
}
