using UnityEngine;

[CreateAssetMenu(fileName = "MTQuadTreeHeader", menuName = "MTTerrain/CreateMTQuadTreeHeader")]
public class MTQuadTreeHeader : ScriptableObject
{
    public int QuadTreeDepth = 0;
    public Vector3 BoundMin = Vector3.zero;
    public Vector3 BoundMax = Vector3.zero;
    public int LOD = 1;
    public string DataName;
    [SerializeField]
    public MTMeshHeader[] Meshes;

    public int MeshCount;

    public int[] MeshIDArray;

    public void RefreshMeshData()
    {
        MeshCount = Meshes.Length;
        MeshIDArray = new int[MeshCount];
        for (int i = 0; i < MeshCount; i++)
        {
            MeshIDArray[i] = Meshes[i].MeshID;
        }
    }
}