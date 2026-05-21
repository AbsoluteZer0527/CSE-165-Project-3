using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class AgentController : MonoBehaviour
{
    public NavMeshSurface surface; // drag your ground object here

    private NavMeshAgent agent;
    private Animator anim;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim  = GetComponent<Animator>();

        // Bake NavMesh at runtime (required since floor is placed at runtime on Quest)
        if (surface != null)
            surface.BuildNavMesh();
    }

    void Update()
    {
        // Step 6 from slides: drive Walking bool from actual velocity
        anim.SetBool("isWalking", agent.velocity.magnitude > 0.1f);
    }

    // Called by MouseClickDestination (editor) or HandGestureController (headset)
    public void SetDestination(Vector3 worldPos)
    {
        // Step 4 from slides: clamp to nearest valid NavMesh point
        if (NavMesh.SamplePosition(worldPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            agent.SetDestination(hit.position); // Step 5
    }

    public void Stop()
    {
        agent.ResetPath();
        anim.SetBool("isWalking", false);
    }
}