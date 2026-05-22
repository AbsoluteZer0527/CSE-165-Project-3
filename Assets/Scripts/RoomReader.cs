using UnityEngine;
using Meta.XR.MRUtilityKit;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class RoomReader : MonoBehaviour
{
    // Assign pre-made transparent materials in Inspector so variants are guaranteed in build.
    // If left empty, falls back to Unlit/Color (always included).
    [SerializeField] Material wallMat;
    [SerializeField] Material floorMat;

    void OnEnable()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoaded);
            if (MRUK.Instance.GetCurrentRoom() != null)
                OnSceneLoaded();
        }
        else
        {
            StartCoroutine(WaitForMRUK());
        }
    }

    void OnDisable()
    {
        if (MRUK.Instance != null)
            MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneLoaded);
    }

    System.Collections.IEnumerator WaitForMRUK()
    {
        while (MRUK.Instance == null)
            yield return null;

        MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneLoaded);

        if (MRUK.Instance.GetCurrentRoom() != null)
            OnSceneLoaded();
    }

    void OnSceneLoaded()
    {
        var room = MRUK.Instance.GetCurrentRoom();
        if (room == null) { Debug.LogError("RoomReader: room is null"); return; }

        Debug.Log($"RoomReader: {room.WallAnchors.Count} walls, {room.FloorAnchors.Count} floors");

        foreach (var wall in room.WallAnchors)
        {
            wall.gameObject.layer = LayerMask.NameToLayer("Surface");
            PlaceQuadOnAnchor(wall, isWall: true);
        }

        foreach (var floor in room.FloorAnchors)
        {
            floor.gameObject.layer = LayerMask.NameToLayer("Surface");
            PlaceQuadOnAnchor(floor, isWall: false);
        }
    }

    void PlaceQuadOnAnchor(MRUKAnchor anchor, bool isWall)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = isWall ? "WallMesh" : "FloorMesh";
        Destroy(quad.GetComponent<Collider>());

        quad.transform.SetParent(anchor.transform, false);
        quad.transform.localPosition = Vector3.zero;

        // Quad normal is +Z; MRUK floor anchor's +Y points out of the floor,
        // so rotate -90 around X to make the quad face upward.
        quad.transform.localRotation = isWall ? Quaternion.identity : Quaternion.Euler(-90f, 0f, 0f);

        if (anchor.PlaneRect.HasValue)
        {
            Vector2 size = anchor.PlaneRect.Value.size;
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
        else
        {
            quad.transform.localScale = Vector3.one * 2f;
            Debug.LogWarning($"RoomReader: PlaneRect null for {anchor.name}, using 2x2 default");
        }

        var renderer = quad.GetComponent<Renderer>();
        Material mat = isWall ? wallMat : floorMat;
        renderer.material = mat != null ? mat : MakeFallbackMat(isWall);

        var meshCollider = quad.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = quad.GetComponent<MeshFilter>().sharedMesh;

        if (isWall)
        {
            var obstacle = quad.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
        }
        else
        {
            var surface = quad.AddComponent<NavMeshSurface>();
            surface.BuildNavMesh();
        }
    }

    // Unlit/Color is always included in Android builds — safe last resort.
    Material MakeFallbackMat(bool isWall)
    {
        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = isWall ? new Color(0f, 0.5f, 1f, 0.5f) : new Color(0f, 1f, 0.3f, 0.4f);
        return mat;
    }
}
