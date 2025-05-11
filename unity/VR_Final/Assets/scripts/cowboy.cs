using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Cowboy : MonoBehaviour, IHittable
{
    private enum State
    {
        Idle, Walking, Running, Ragdoll, PreGame
    }

    private Rigidbody[] bodyPartRigidbodies;
    private Animator animator;
    private CharacterController controller;

    [Header("Hat Settings")]
    [SerializeField] private string hatGameObjectName = "CowboyHat";
    private Rigidbody instanceHatRigidbody;
    private Transform originalHatParent;
    private Vector3 originalHatLocalPosition;
    private Quaternion originalHatLocalRotation;
    private GameObject detachedHatInstance = null;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4f;

    [Header("Distance Thresholds")]
    [SerializeField] private float walkDistance = 0.50f;
    [SerializeField] private float runDistance = 2f;

    [SerializeField] private Camera gameCamera;
    private State currentState = State.PreGame;
    public CowboySpawner spawner;
    private bool isRagdolling = false;
    private bool localGameHasStarted = false;

    // REMOVED: AudioSource and AudioClip for deathSound are now handled by SoundManager

    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        // REMOVED: Local AudioSource setup block
        // --- NEW AUDIO ADDITION: Get or add AudioSource ---
        // ...
        // --- END NEW AUDIO ADDITION ---

        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            if (gameCamera == null) Debug.LogError("Main Camera not found for " + gameObject.name, this);
        }
        if (controller == null) Debug.LogError("CharacterController not found on " + gameObject.name, this);
        if (animator == null) Debug.LogError("Animator not found on " + gameObject.name, this);
        
        InitializeCowboyState();

        if (GameManager.Instance != null)
        {
            if (GameManager.HasInitialGunBeenPickedUp())
            {
                HandleGameActuallyStartedLogic();
            }
            else
            {
                GameManager.OnGunPickedUpToStart += HandleGameActuallyStartedLogic;
            }
        }
        else
        {
            Debug.LogError($"Cowboy {gameObject.name}: GameManager Instance not found.");
        }
    }


    void OnDestroy()
    {
        StopAllCoroutines();
        if (detachedHatInstance != null)
        {
            Destroy(detachedHatInstance);
            detachedHatInstance = null;
        }
        if (GameManager.Instance != null)
        {
            GameManager.OnGunPickedUpToStart -= HandleGameActuallyStartedLogic;
        }
    }

    private void HandleGameActuallyStartedLogic()
    {
        if (localGameHasStarted || isRagdolling) return;

        localGameHasStarted = true;
        currentState = State.Idle; 
        
        if (controller != null) controller.enabled = true;
        if (animator != null) animator.enabled = true; 

        if (GameManager.Instance != null)
        {
            GameManager.OnGunPickedUpToStart -= HandleGameActuallyStartedLogic;
        }
    }

    void InitializeCowboyState()
    {
        isRagdolling = false;
        detachedHatInstance = null;
        localGameHasStarted = false;
        currentState = State.PreGame;

        if (animator == null) animator = GetComponent<Animator>();
        if (controller == null) controller = GetComponent<CharacterController>();

        List<Rigidbody> rbs = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>(true));
        Transform hatTransform = FindDeepChild(this.transform, hatGameObjectName);

        if (hatTransform != null)
        {
            instanceHatRigidbody = hatTransform.GetComponent<Rigidbody>();
            if (instanceHatRigidbody != null)
            {
                if (originalHatParent == null) 
                {
                    originalHatParent = instanceHatRigidbody.transform.parent;
                    originalHatLocalPosition = instanceHatRigidbody.transform.localPosition;
                    originalHatLocalRotation = instanceHatRigidbody.transform.localRotation;
                }
                rbs.Remove(instanceHatRigidbody); 
                
                instanceHatRigidbody.gameObject.SetActive(true);
                instanceHatRigidbody.transform.SetParent(originalHatParent);
                instanceHatRigidbody.transform.localPosition = originalHatLocalPosition;
                instanceHatRigidbody.transform.localRotation = originalHatLocalRotation;
                instanceHatRigidbody.isKinematic = true;
                instanceHatRigidbody.useGravity = false;
                instanceHatRigidbody.linearVelocity = Vector3.zero;
                instanceHatRigidbody.angularVelocity = Vector3.zero;
            }
            else Debug.LogError($"Cowboy {gameObject.name}: Hat GameObject '{hatGameObjectName}' found, but no Rigidbody.", this);
        }
        else Debug.LogWarning($"Cowboy {gameObject.name}: Hat GameObject '{hatGameObjectName}' not found. No hat physics.", this);
        
        bodyPartRigidbodies = rbs.ToArray();

        foreach (var rb in bodyPartRigidbodies)
        {
            if (rb == null) continue;
            rb.gameObject.SetActive(true); 
            rb.isKinematic = true;
            rb.detectCollisions = true; 
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (controller != null)
        {
            controller.enabled = false; 
        }
        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind(); 
            animator.Update(0f); 
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }
    }

    Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }

    public void OnHit()
    {
        if (isRagdolling || !this.enabled) return;

        // Play death sound via SoundManager
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayCowboyDeathSound(transform.position);
        }
        else
        {
            Debug.LogWarning($"Cowboy {gameObject.name}: SoundManager.Instance not found. Cannot play death sound.");
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.IncrementCowboysShot();
        }
        else
        {
            Debug.LogWarning($"Cowboy {gameObject.name}: UIManager.Instance not found when trying to increment cowboys shot count.");
        }
        
        EnableRagdollAnatomy();
    }

    public void EnableRagdollAnatomy()
    {
        if (isRagdolling) return;

        isRagdolling = true;
        currentState = State.Ragdoll;

        if (animator != null) animator.enabled = false;
        if (controller != null) controller.enabled = false;

        foreach (var rb in bodyPartRigidbodies)
        {
            if (rb == null) continue;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate; 
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; 
            rb.WakeUp();
        }

        if (instanceHatRigidbody != null)
        {
            if (detachedHatInstance == null) 
            {
                 detachedHatInstance = instanceHatRigidbody.gameObject; 
            }
            instanceHatRigidbody.transform.SetParent(null); 
            instanceHatRigidbody.isKinematic = false;
            instanceHatRigidbody.useGravity = true;
            instanceHatRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            instanceHatRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Vector3 forceDirection = (Vector3.up * 1.5f) + (transform.forward * -1f * 2f) + (Random.insideUnitSphere * 0.5f);
            instanceHatRigidbody.AddForce(forceDirection, ForceMode.Impulse);
            instanceHatRigidbody.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
            instanceHatRigidbody.WakeUp();
        }

        StartCoroutine(NotifyDeathCoroutine());
    }

    private IEnumerator NotifyDeathCoroutine()
    {
        yield return new WaitForSeconds(3f); 
        if (spawner != null && this != null && gameObject != null && gameObject.activeInHierarchy) 
        {
            spawner.OnCowboyDied(gameObject); 
        }
        else if (this != null && gameObject != null && gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
        }
    }
    
    void PreGameBehavior()
    {
        if (animator != null && animator.enabled)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }
        if (controller != null) controller.enabled = false;
    }

    void IdleBehavior()
    {
        if (animator != null && animator.enabled)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }
    }

    void WalkingBehavior()
    {
        if (animator != null && animator.enabled)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", true);
        }
        if (controller != null && controller.enabled && animator != null && animator.enabled)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!animator.IsInTransition(0) && (stateInfo.IsName("Walking") || stateInfo.IsTag("Walk")) && stateInfo.normalizedTime >= 0.05f) 
            {
                MoveTowardsCamera(walkSpeed);
            }
        }
    }

    void RunningBehavior()
    {
        if (animator != null && animator.enabled)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", true);
        }
        if (controller != null && controller.enabled && animator != null && animator.enabled)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
             if (!animator.IsInTransition(0) && (stateInfo.IsName("Running") || stateInfo.IsTag("Run")) && stateInfo.normalizedTime >= 0.05f) 
            {
                MoveTowardsCamera(runSpeed);
            }
        }
    }

    private void MoveTowardsCamera(float speed)
    {
        if (gameCamera == null || controller == null || !controller.enabled) return;

        Vector3 dirToCamera = gameCamera.transform.position - transform.position;
        dirToCamera.y = 0;
        dirToCamera.Normalize();

        if (dirToCamera != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dirToCamera, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 200f * Time.deltaTime);
        }
        if (controller.enabled)
        {
            controller.Move(transform.forward * speed * Time.deltaTime);
            // NOTE: We will apply the Y-position fix in LateUpdate for better stability.
        }
    }

    void RagdollBehavior()
    {
        // Ragdoll is mostly physics-driven.
    }

    void Update()
    {
        if (isRagdolling) 
        {
            if (currentState != State.Ragdoll) currentState = State.Ragdoll;
            RagdollBehavior();
            return;
        }

        if (!localGameHasStarted) 
        {
            if (currentState != State.PreGame) currentState = State.PreGame; 
            PreGameBehavior();
            return; 
        }
        
        if (gameCamera == null) 
        {
            if (controller) controller.enabled = false;
            return;
        }
            
        float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                                      new Vector3(gameCamera.transform.position.x, 0, gameCamera.transform.position.z));


        if (dist <= walkDistance - 0.1f)
        {
            // Debug.Log($"{gameObject.name} is close enough to player! Distance: {dist}"); 
            if (UIManager.Instance != null && !UIManager.Instance.IsDeathScreenActive())
            {
                UIManager.Instance.NotifyPlayerReachedByCowboy();
            }
            if (controller) controller.enabled = false;
            // Keep animator enabled for idle pose, but don't play walk/run
            if (animator) {
                animator.SetBool("isWalking", false);
                animator.SetBool("isRunning", false);
            }
            currentState = State.Idle; // Ensure state is Idle
            // IdleBehavior(); // Call IdleBehavior to set animator bools correctly.
            return; // Stop further state changes and movement updates
        }


        if (dist > runDistance)
            currentState = State.Running;
        else if (dist > walkDistance)
            currentState = State.Walking;
        else 
            currentState = State.Idle;


        if (controller != null && controller.enabled && animator != null && animator.enabled)
        {
            switch (currentState)
            {
                case State.Idle:    IdleBehavior(); break;
                case State.Walking: WalkingBehavior(); break;
                case State.Running: RunningBehavior(); break;
            }
        }
        else if (controller != null && !controller.enabled && localGameHasStarted && !isRagdolling)
        {
            IdleBehavior();
        }
    }

    // +++ ADDED FIX BELOW +++
    void LateUpdate()
    {
        // This fix applies when the cowboy is active (not ragdolled, and not in the pre-game state).
        // It ensures the cowboy's transform.position.y is kept at 0.
        if (!isRagdolling && currentState != State.PreGame)
        {
            Vector3 currentPosition = transform.position;
            float targetYPosition = 0f; // Assuming ground plane is at Y=0

            // If the cowboy's Y position is not at the target, force it.
            // Using a small tolerance helps prevent unnecessary transform changes if it's already very close,
            // though for simplicity, an exact check and set is also fine.
            if (Mathf.Abs(currentPosition.y - targetYPosition) > 0.001f) // Check if not already at/very close to 0
            {
                transform.position = new Vector3(currentPosition.x, targetYPosition, currentPosition.z);
            }
        }
    }
    // +++ END OF ADDED FIX +++
}