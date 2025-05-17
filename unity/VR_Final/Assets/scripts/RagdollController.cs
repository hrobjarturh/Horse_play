using UnityEngine;

public class RagdollController : MonoBehaviour
{
    public Animator animator;

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    void Start()
    {
        // Cache all child rigidbodies and colliders (exclude root object, which has none)
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = false;
        }

        // Disable ragdoll on start
        SetRagdoll(false);
    }

    public void SetRagdoll(bool enableRagdoll)
    {
        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        foreach (var col in ragdollColliders)
        {
            col.enabled = enableRagdoll;
        }

        // Disable animator when ragdoll is active
        if (animator != null)
            animator.enabled = !enableRagdoll;
    }
}
