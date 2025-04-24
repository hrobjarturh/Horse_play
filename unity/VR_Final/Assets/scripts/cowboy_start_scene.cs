using UnityEngine;
using System.Collections;

// Ensures the GameObject has an Animator component
[RequireComponent(typeof(Animator))]
public class CowboySequenceController : MonoBehaviour // Changed class name for clarity
{
    // --- Configuration ---
    [Header("Movement")]
    [Tooltip("How fast the character moves forward (units per second).")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Tooltip("Assign an empty GameObject in the scene marking the final destination.")]
    [SerializeField] private Transform targetSpot;

    [Tooltip("How close the character needs to be to the targetSpot to stop.")]
    [SerializeField] private float stoppingDistance = 0.5f;

    [Tooltip("How far from the targetSpot the 'OpenDoor' animation should trigger.")]
    [SerializeField] private float distanceToOpenDoor = 5.0f;

    [Header("Animation Parameters (Triggers)")]
    [Tooltip("Name of the Trigger parameter in the Animator to start walking.")]
    [SerializeField] private string walkTriggerName = "Walk";

    [Tooltip("Name of the Trigger parameter for the door opening animation.")]
    [SerializeField] private string openDoorTriggerName = "OpenDoor";

    [Tooltip("Name of the Trigger parameter to transition from walking to idle.")]
    [SerializeField] private string stopWalkTriggerName = "StopWalk";
    // Idle state is assumed to be reached automatically after the stop animation

    // --- State ---
    private enum SequenceState
    {
        NotStarted,
        PerformingSequence,
        Idle // Sequence completed
    }
    private SequenceState currentState = SequenceState.NotStarted;

    // --- Components ---
    private Animator animator;

    // --- Coroutine Reference ---
    private Coroutine activeSequenceCoroutine = null;

    void Awake()
    {
        animator = GetComponent<Animator>();

        // Validate essential references
        if (targetSpot == null)
        {
            Debug.LogError("Target Spot is not assigned in the Inspector for the Cowboy!", this);
            enabled = false; // Disable this script if target is missing
            return;
        }
        if (animator == null)
        {
             Debug.LogError("Animator component not found on the Cowboy!", this);
             enabled = false; // Disable this script if Animator is missing
             return;
        }
    }

    void Start()
    {
        // Automatically start the sequence when the game begins
        StartEntrySequence();
    }

    // Public method to potentially restart the sequence if needed later
    public void StartEntrySequence()
    {
        // Prevent starting if already running
        if (currentState == SequenceState.PerformingSequence && activeSequenceCoroutine != null)
        {
            Debug.LogWarning("Sequence already running.");
            return;
        }

        // Stop any previous coroutine just in case
        if(activeSequenceCoroutine != null)
        {
            StopCoroutine(activeSequenceCoroutine);
        }

        // Begin the sequence
        currentState = SequenceState.PerformingSequence;
        activeSequenceCoroutine = StartCoroutine(PerformEntrySequenceCoroutine());
    }


    // --- Main Sequence Coroutine ---
    private IEnumerator PerformEntrySequenceCoroutine()
    {
        Debug.Log("Starting Entry Sequence...");

        // --- Phase 1: Initial Walk ---
        Debug.Log("Phase 1: Walking towards door area");
        animator.SetTrigger(walkTriggerName);

        // Walk until close enough to the target to trigger the door open part
        while (Vector3.Distance(transform.position, targetSpot.position) > distanceToOpenDoor)
        {
            MoveTowardsTarget();
            yield return null; // Wait for the next frame
        }

        // Optional check: If we started too close or overshot, skip door open and go straight to stopping
        if (Vector3.Distance(transform.position, targetSpot.position) <= stoppingDistance)
        {
            Debug.Log("Reached stopping distance early, skipping door open and triggering StopWalk.");
        }
        else
        {
            // --- Phase 2: Walking Door Open ---
            Debug.Log("Phase 2: Triggering Open Door animation");
            animator.SetTrigger(openDoorTriggerName);
            // We assume the Animator transitions back to Walking automatically after this state finishes.
            // Continue moving during this phase.
            Debug.Log("Continuing movement during/after door open animation");

            // --- Phase 3: Walk Again (After Door, if needed) ---
            // Continue walking until the final stopping distance is reached
             Debug.Log("Phase 3: Walking final distance");
             while (Vector3.Distance(transform.position, targetSpot.position) > stoppingDistance)
             {
                MoveTowardsTarget();
                yield return null; // Wait for the next frame
             }
        }

        // --- Phase 4: Stop Walking ---
        Debug.Log("Phase 4: Reached target, triggering Stop Walk animation");
        animator.SetTrigger(stopWalkTriggerName);
        // Stop physical movement here. The Animator plays the stop animation.

        // --- Phase 5: Idle ---
        // Wait a brief moment for the Animator to likely settle into the Idle state.
        // A more robust method checks animator.GetCurrentAnimatorStateInfo, but this is simpler.
        yield return new WaitForSeconds(0.2f);

        Debug.Log("Sequence Complete. Switching to Idle state.");
        currentState = SequenceState.Idle;
        activeSequenceCoroutine = null; // Coroutine finished
    }

    // --- Helper Movement Function ---
    void MoveTowardsTarget()
    {
        // Calculate direction towards target, ignoring vertical difference
        Vector3 direction = targetSpot.position - transform.position;
        direction.y = 0;

        // Check if we are already very close to avoid unnecessary calculations/rotation
        if (direction.sqrMagnitude < 0.01f) // Use squared magnitude for efficiency
        {
            return;
        }

        // Rotate towards the target direction
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        // Slerp or RotateTowards can be used. RotateTowards is often more predictable for constant speed.
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 200 * Time.deltaTime); // Adjust rotation speed as needed

        // Move the character forward in its local Z direction
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    // --- Update Loop ---
    // The Update loop is now very simple. It doesn't need to do anything once the
    // sequence starts, as the coroutine handles everything until completion.
    // You could add logic here for what happens *after* the sequence (in the Idle state).
    void Update()
    {
        if (currentState == SequenceState.Idle)
        {
            // Character is now idle. Add any idle behavior here if needed.
            // For example, slightly rotate to look around, etc.
            // If no further action is needed, this can remain empty.
        }
        // No action needed during PerformingSequence, the coroutine handles it.
    }
}