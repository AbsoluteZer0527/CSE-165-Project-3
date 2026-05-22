using UnityEngine;
using Meta.XR.MRUtilityKit;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class RoomReader : MonoBehaviour
{
    // Drag a scene Plane (with NavMeshSurface component) here.
    // It will be repositioned and scaled to match the real floor at runtime.
    public NavMeshSurface navMeshFloor;

    void Start()
    {
        StartCoroutine(WaitForMRUK());
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
        if (room == null) return;

        Debug.Log("Room loaded! Walls: " + room.WallAnchors.Count);

        // Part 1: Define spatial anchors — set layer on all surfaces
        foreach (var wall in room.WallAnchors)
        {
            wall.gameObject.layer = LayerMask.NameToLayer("Surface");
            // Part 2: Place quad on each wall anchor
            PlaceQuad(wall, isWall: true);
        }

        foreach (var floor in room.FloorAnchors)
        {
            floor.gameObject.layer = LayerMask.NameToLayer("Surface");
            // Part 2: Place quad on floor anchor
            PlaceQuad(floor, isWall: false);
        }
    }

    void PlaceQuad(MRUKAnchor anchor, bool isWall)
    {
        // Create digital copy of physical surface
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = isWall ? "WallMesh" : "FloorMesh";

        // Remove default collider
        Destroy(quad.GetComponent<Collider>());

        // Lock to anchor transform
        quad.transform.SetParent(anchor.transform, false);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localRotation = Quaternion.identity;

        // Scale to real surface size
        if (anchor.PlaneRect.HasValue)
        {
            Vector2 size = anchor.PlaneRect.Value.size;
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
        else
        {
            quad.transform.localScale = Vector3.one * 2f;
        }

        // Create visible transparent material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_Cull", 0); // render both sides
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        mat.color = isWall
            ? new Color(0.2f, 0.6f, 1f, 0.4f)  // blue walls
            : new Color(0.2f, 1f, 0.4f, 0.3f);  // green floor

        quad.GetComponent<Renderer>().material = mat;

        // Add collider for raycasting (gestures)
        var meshCol = quad.AddComponent<MeshCollider>();
        meshCol.sharedMesh = quad.GetComponent<MeshFilter>().sharedMesh;

        if (isWall)
        {
            // Block NavMesh agent
            var obstacle = quad.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
        }
        else if (navMeshFloor != null)
        {
            // Snap the dragged-in Plane to the real floor anchor.
            // The MRUK floor anchor has local Z = world -Y, so local X and local Y are both
            // horizontal. Map anchor.localX → plane.localX and anchor.localY → plane.localZ
            // so the plane lies flat and its scale axes align with the room dimensions.
            navMeshFloor.transform.position = anchor.transform.position;
            navMeshFloor.transform.rotation = Quaternion.LookRotation(anchor.transform.up, Vector3.up);
            if (anchor.PlaneRect.HasValue)
            {
                // Unity Plane is 10x10 units, so divide size by 10 to match meters.
                // size.x → plane localX (anchor localX), size.y → plane localZ (anchor localY).
                Vector2 size = anchor.PlaneRect.Value.size;
                navMeshFloor.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);
            }
            navMeshFloor.BuildNavMesh();
            Debug.Log($"NavMesh baked at {navMeshFloor.transform.position}, scale {navMeshFloor.transform.localScale}");
        }
        else if (!isWall)
        {
            Debug.LogWarning("RoomReader: navMeshFloor is not assigned — NavMesh will not be built!");
        }
    }
}