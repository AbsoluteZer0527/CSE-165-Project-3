using UnityEngine;

public class HandGestureController : MonoBehaviour
{
    [Header("Hand References")]
    public OVRHand hand;
    public OVRSkeleton skeleton;

    [Header("Agent")]
    public AgentController agentController;

    [Header("Raycast")]
    public LayerMask surfaceMask;

    private Transform indexTip;
    private Transform indexProximal;
    private Transform indexDistal;
    private Transform wrist;
    private bool bonesReady = false;

    private float gestureHeldTime = 0f;
    private const float DEBOUNCE_TIME = 0.2f;

    void Update()
    {
        // Debug 1 — is hand tracked at all?
        if (!hand.IsTracked)
        {
            Debug.Log("Hand NOT tracked");
            return;
        }

        Debug.Log($"Hand tracked. Confidence: {hand.HandConfidence}");

        // Debug 2 — are bones cached?
        if (!bonesReady)
        {
            Debug.Log("Bones not ready yet, trying to cache...");
            TryCacheBones();
            return;
        }

        Debug.Log("Bones ready ✅");

        // Debug 3 — confidence check
        if (hand.HandConfidence != OVRHand.TrackingConfidence.High)
        {
            Debug.Log($"Confidence too low: {hand.HandConfidence}");
            gestureHeldTime = 0f;
            return;
        }

        // Debug 4 — is index extended?
        bool extended = IsIndexExtended();
        Debug.Log($"Index extended: {extended}");

        if (extended)
        {
            gestureHeldTime += Time.deltaTime;
            Debug.Log($"Gesture held for: {gestureHeldTime:F2}s");

            if (gestureHeldTime >= DEBOUNCE_TIME)
            {
                Debug.Log("Debounce passed — casting ray!");
                CastRayToFloor();
            }
        }
        else
        {
            gestureHeldTime = 0f;
        }

        // Pinch to stop
        bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        Debug.Log($"Pinching: {pinching}");
        if (pinching)
            agentController.Stop();
    }

    void TryCacheBones()
    {
        if (!skeleton.IsInitialized || skeleton.Bones == null)
        {
            Debug.Log("Skeleton not initialized yet");
            return;
        }

        Debug.Log($"Skeleton bones count: {skeleton.Bones.Count}");

        foreach (var bone in skeleton.Bones)
        {
            switch (bone.Id)
            {
                case OVRSkeleton.BoneId.XRHand_IndexTip:
                    indexTip = bone.Transform; break;
                case OVRSkeleton.BoneId.XRHand_IndexProximal:
                    indexProximal = bone.Transform; break;
                case OVRSkeleton.BoneId.XRHand_IndexDistal:
                    indexDistal = bone.Transform; break;
                case OVRSkeleton.BoneId.XRHand_Wrist:
                    wrist = bone.Transform; break;
            }
        }

        bonesReady = indexTip != null && indexProximal != null
                  && indexDistal != null && wrist != null;

        Debug.Log($"Bones cached: indexTip={indexTip != null}, " +
                  $"proximal={indexProximal != null}, " +
                  $"distal={indexDistal != null}, " +
                  $"wrist={wrist != null}");

        if (bonesReady)
            Debug.Log("✅ All bones cached successfully!");
    }

    bool IsIndexExtended()
    {
        if (indexProximal == null || indexDistal == null || indexTip == null)
            return false;

        Vector3 a   = indexProximal.position;
        Vector3 b   = indexDistal.position;
        Vector3 tip = indexTip.position;

        float dot = Vector3.Dot(
            (b - a).normalized,
            (tip - b).normalized);

        Debug.Log($"Index dot product: {dot:F3} (need > 0.85)");

        return dot > 0.85f;
    }

    void CastRayToFloor()
    {
        if (indexTip == null || wrist == null) return;

        Vector3 dir = (indexTip.position - wrist.position).normalized;
        Ray ray = new Ray(indexTip.position, dir);

        Debug.Log($"Raycasting from {indexTip.position} dir {dir}");
        Debug.DrawRay(indexTip.position, dir * 10f, Color.red, 0.5f);

        if (Physics.Raycast(ray, out RaycastHit hit, 10f, surfaceMask))
        {
            Debug.Log($"✅ Ray hit: {hit.collider.name} at {hit.point}");
            agentController.SetDestination(hit.point);
        }
        else
        {
            Debug.LogWarning("❌ Ray missed — no Surface layer hit");
            
            // Debug: try without layer mask
            if (Physics.Raycast(ray, out RaycastHit anyHit, 10f))
                Debug.Log($"Hit without mask: {anyHit.collider.name} layer: {LayerMask.LayerToName(anyHit.collider.gameObject.layer)}");
            else
                Debug.LogWarning("Ray hit nothing at all");
        }
    }
}