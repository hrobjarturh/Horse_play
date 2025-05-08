using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 2f;

    [Header("Distance Thresholds")]
    [Tooltip("Distance from camera at which to start walking")]
    [SerializeField] private float walkDistance = .50f;
    [Tooltip("Distance from camera at which to start running")]
    [SerializeField] private float runDistance = 2f;

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

    void IdleBehavior()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
    }

    void WalkingBehavior()
    {
        animator.SetBool("isRunning", false);
        animator.SetBool("isWalking", true);

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!animator.IsInTransition(0)
            && state.IsName("Walking")
            && state.normalizedTime >= 0.1f)
        {
            MoveTowardsCamera(walkSpeed);
        }
    }

    void RunningBehavior()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", true);

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (!animator.IsInTransition(0)
            && state.IsName("Running")
            && state.normalizedTime >= 0.1f)
        {
            MoveTowardsCamera(runSpeed);
        }
    }

    private void MoveTowardsCamera(float speed)
    {
        Vector3 dir = camera.transform.position - transform.position;
        dir.y = 0;
        dir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            20f * Time.deltaTime
        );

        controller.Move(transform.forward * speed * Time.deltaTime);
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
            float dist = Vector3.Distance(transform.position, camera.transform.position);


            if (dist <= 1.1f)
            {
                // Player caught!
                UIManager.Instance.ShowDeathScreen();
                // Optionally, disable further movement:
                enabled = false;
                return;
            }

            if (dist > runDistance)
                currentState = State.Running;
            else if (dist > walkDistance)
                currentState = State.Walking;
            else
                currentState = State.Idle;
        }

        switch (currentState)
        {
            case State.Idle: IdleBehavior(); break;
            case State.Walking: WalkingBehavior(); break;
            case State.Running: RunningBehavior(); break;
            case State.Ragdoll: RagdollBehavior(); break;
        }
    }
}
