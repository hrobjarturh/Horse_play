using UnityEngine;
using UnityEngine.InputSystem;

public class cowboy : MonoBehaviour, IHittable
{
    private Rigidbody[] rigidbodies;
    private Animator animator;
    [SerializeField] private Transform hatTransform;
    [SerializeField] private Rigidbody hatRigidbody;

    void Awake()
    {
        Debug.Log("Cowboy spawned!");

        rigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();
        DisableRagdoll();
    }

    public void OnHit()
    {
        Debug.Log("Cowboy hit!");
        EnableRagdoll();
    }

    public void EnableRagdoll()
    {
        animator.enabled = false;

        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // Unparent the hat
        if (hatTransform != null)
        {
            hatTransform.SetParent(null);
            hatRigidbody.isKinematic = false;
            hatRigidbody.useGravity = true;
            hatRigidbody.AddForce(Vector3.up * 1.5f + Vector3.back * 2f, ForceMode.Impulse);
            // Add a torque to the hat
            hatRigidbody.AddTorque(Vector3.up * 10f, ForceMode.Impulse);
        }

        Debug.Log("Ragdoll + Hat launched!");
    }

        void DisableRagdoll()
    {
        foreach (var rb in rigidbodies)
        {
            //disable hitbox
            var col = rb.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
            rb.isKinematic = true;

            
            //gravity is on 
        }

        if (animator != null) animator.enabled = true;
    }
}
