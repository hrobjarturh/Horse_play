using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class cowboy : MonoBehaviour, IHittable
{
    private enum State
    {
        Idle,
        Walking,
        Running,
        Ragdoll
    }
    private Rigidbody[] rigidbodies;
    private Animator animator;
    [SerializeField] private Transform hatTransform;
    [SerializeField] private Rigidbody hatRigidbody;
    [SerializeField] private Camera camera;
    private State currentState = State.Walking;

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

    void WalkingBehavior()
    {
        Vector3 direction = camera.transform.position - transform.position;
        direction.y = 0;
        direction.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 20 * Time.deltaTime);
        
    }

    void RunningBehavior()
    {
        if (currentState != State.Running)
        {
            currentState = State.Running;
            animator.SetBool("isRunning", true);
        }
    }

    void IdleBehavior()
    {
        if (currentState != State.Idle)
        {
            currentState = State.Idle;
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }
    }

    void RagdollBehavior()
    {
        if (currentState != State.Ragdoll)
        {
            currentState = State.Ragdoll;
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                IdleBehavior();
                break;
            case State.Walking:
                WalkingBehavior();
                break;
            case State.Running:
                RunningBehavior();
                break;
            case State.Ragdoll:
                RagdollBehavior();
                break;
        }
    }
}
