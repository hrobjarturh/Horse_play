using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // Make sure this is here if you used Option 1 for input

[RequireComponent(typeof(Animator))]
public class CowboySequenceController : MonoBehaviour
{
    // --- Configuration ---
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private Transform targetSpot;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float distanceToOpenDoor = 5.0f;

    [Header("Animation Parameters (Triggers)")]
    [SerializeField] private string walkTriggerName = "Walk";
    [SerializeField] private string openDoorTriggerName = "OpenDoor";
    [SerializeField] private string stopWalkTriggerName = "StopWalk";

    private enum SequenceState
    {
        Idle,
        PerformingSequence,
    }
    private SequenceState currentState = SequenceState.Idle;

    private Animator animator;
    private Rigidbody rb;
    private Coroutine activeSequenceCoroutine = null;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (targetSpot == null)
        {
            Debug.LogError("TARGET SPOT NOT ASSIGNED!", this);
            enabled = false; return;
        }
        if (animator == null)
        {
             Debug.LogError("ANIMATOR NOT FOUND!", this);
             enabled = false; return;
        }
        currentState = SequenceState.Idle;
        Debug.Log("AWAKE: Cowboy starting in Idle state.");
    }

    public void StartEntrySequence()
    {
        // --- LOG 1 ---
        Debug.Log($"START_ENTRY_SEQUENCE: Called. Current State: {currentState}");

        if (currentState != SequenceState.Idle)
        {
            Debug.LogWarning($"START_ENTRY_SEQUENCE: Cannot start, current state is {currentState}. Bailing.");
            return;
        }
        if (activeSequenceCoroutine != null)
        {
             Debug.LogWarning("START_ENTRY_SEQUENCE: Previous coroutine was active. Stopping it.");
            StopCoroutine(activeSequenceCoroutine);
        }

        currentState = SequenceState.PerformingSequence;
        // --- LOG 2 ---
        Debug.Log("START_ENTRY_SEQUENCE: State changed to PerformingSequence. Starting coroutine.");
        activeSequenceCoroutine = StartCoroutine(PerformEntrySequenceCoroutine());
    }

    private IEnumerator PerformEntrySequenceCoroutine()
    {
        // --- LOG 3 ---
        Debug.Log("COROUTINE: Started. Setting '" + walkTriggerName + "' trigger.");
        animator.SetTrigger(walkTriggerName);
        yield return new WaitForSeconds(0.1f); // Small delay to allow transition

        // --- LOG 4 ---
        Debug.Log("COROUTINE: Initial Walk Phase starting.");
        while (Vector3.Distance(transform.position, targetSpot.position) > distanceToOpenDoor)
        {
            if (currentState != SequenceState.PerformingSequence)
            {
                // --- LOG 5 ---
                Debug.LogWarning("COROUTINE: State no longer PerformingSequence during initial walk. Exiting coroutine.");
                yield break;
            }
            MoveTowardsTarget();
            yield return null;
        }
        // --- LOG 6 ---
        Debug.Log("COROUTINE: Initial Walk Phase ended (or distanceToOpenDoor met).");


        if (Vector3.Distance(transform.position, targetSpot.position) <= stoppingDistance)
        {
             // --- LOG 7 ---
            Debug.Log("COROUTINE: Reached stopping distance early.");
        }
        else
        {
            // --- LOG 8 ---
            Debug.Log("COROUTINE: Door Open Phase. Setting '" + openDoorTriggerName + "' trigger.");
            animator.SetTrigger(openDoorTriggerName);

            // --- LOG 9 ---
            Debug.Log("COROUTINE: Final Walk Phase starting.");
             while (Vector3.Distance(transform.position, targetSpot.position) > stoppingDistance)
             {
                if (currentState != SequenceState.PerformingSequence)
                {
                    // --- LOG 10 ---
                    Debug.LogWarning("COROUTINE: State no longer PerformingSequence during final walk. Exiting coroutine.");
                    yield break;
                }
                MoveTowardsTarget();
                yield return null;
             }
            // --- LOG 11 ---
            Debug.Log("COROUTINE: Final Walk Phase ended (stoppingDistance met).");
        }

        // --- LOG 12 ---
        Debug.Log("COROUTINE: Stop Phase. Setting '" + stopWalkTriggerName + "' trigger.");
        animator.SetTrigger(stopWalkTriggerName);

        // --- LOG 13 ---
        // Wait for approximately the length of the current animator state (should be "Stopping")
        // This is a rough estimate and might need adjustment or a more robust check
        float currentClipLength = 0f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // It's possible the transition to "Stopping" hasn't fully happened yet when this line is hit.
        // A small fixed delay might be more reliable if the state name check is tricky.
        // For now, let's use a fixed delay after setting the stop trigger.
        // if(animator.HasState(0, Animator.StringToHash("Stopping"))) // More robust check
        // {
        //    currentClipLength = stateInfo.length;
        // }
        // else
        // {
        //    currentClipLength = 1.0f; // Default if "Stopping" state isn't immediately identified
        // }
        // Debug.Log($"COROUTINE: Waiting for stop animation to finish (approx {currentClipLength}s). Current state: {stateInfo.fullPathHash}");
        yield return new WaitForSeconds(1.5f); // Adjust this fixed delay based on your "Stopping" animation length


        // --- LOG 14 ---
        Debug.Log("COROUTINE: Sequence Complete. Setting state to Idle.");
        currentState = SequenceState.Idle;
        activeSequenceCoroutine = null;
    }

    void MoveTowardsTarget()
    {
        Vector3 direction = targetSpot.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 200 * Time.deltaTime);
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime, Space.Self);
    }

    void Update()
    {
        if (currentState == SequenceState.Idle)
        {
            // Using New Input System
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                // --- LOG 0 ---
                Debug.Log("UPDATE: 'G' Key Pressed (Input System). CurrentState: " + currentState);
                StartEntrySequence();
            }
        }
    }
}