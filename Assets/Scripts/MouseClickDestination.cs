using UnityEngine;
using UnityEngine.InputSystem;  // ← new input system

public class MouseClickDestination : MonoBehaviour
{
    public AgentController agent;

    void Update()
    {
        // Left click → walk there
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                agent.SetDestination(hit.point);
                Debug.Log($"Destination set to: {hit.point}");
            }
        }

        // Right click → stop
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            agent.Stop();
            Debug.Log("Agent stopped");
        }
    }
}