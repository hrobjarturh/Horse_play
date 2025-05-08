using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class Cowboy : MonoBehaviour, IHittable
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
    [SerializeField] private float walkSpeed = 0f;
    private CharacterController controller;
    [SerializeField] private Camera camera;
    private State currentState = State.Walking;
    public CowboySpawner spawner;

    void Awake()
    {
        Debug.Log("Cowboy spawned!");
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
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

        if (hatTransform != null)
        {
            hatTransform.SetParent(null);
            hatRigidbody.isKinematic = false;
            hatRigidbody.useGravity = true;
            hatRigidbody.AddForce(Vector3.up * 1.5f + Vector3.back * 2f, ForceMode.Impulse);
            hatRigidbody.AddTorque(Vector3.up * 10f, ForceMode.Impulse);
        }

        StartCoroutine(NotifyDeathCoroutine());
    }

    private IEnumerator NotifyDeathCoroutine()
    {
        yield return new WaitForSeconds(3f);
        spawner?.OnCowboyDied(gameObject);
    }

    void DisableRagdoll()
    {
        foreach (var rb in rigidbodies)
        {
            var col = rb.GetComponent<Collider>();
            if (col != null) col.enabled = true;
            rb.isKinematic = true;
        }

        if (animator != null) animator.enabled = true;
    }

    void WalkingBehavior()
    {
        animator.SetBool("isWalking", true);
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        // then your usual check...
        if (!animator.IsInTransition(0)
            && state.IsName("Walking")
            && state.normalizedTime >= 0.1f)
        {

            //log the state
            Vector3 direction = camera.transform.position - transform.position;
            direction.y = 0;
            direction.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 20 * Time.deltaTime);

            Vector3 moveVector = transform.forward * walkSpeed;
            controller.Move(moveVector * Time.deltaTime);
            
        }
    }
    

    void IdleBehavior()
    {
        animator.SetBool("isWalking", false);
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
        if (currentState != State.Ragdoll)
        {
            float distanceToCamera = Vector3.Distance(transform.position, camera.transform.position);
            currentState = distanceToCamera > 2.5f ? State.Walking : State.Idle;
        }

        switch (currentState)
        {
            case State.Idle:
                IdleBehavior();
                break;
            case State.Walking:
                WalkingBehavior();
                break;
            case State.Ragdoll:
                RagdollBehavior();
                break;
        }
    }
}

