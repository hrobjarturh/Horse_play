using UnityEngine;

public class RagdollHitListener : MonoBehaviour
{
    private Cowboy cowboyScript;

    public void Init(Cowboy cowboy)
    {
        cowboyScript = cowboy;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (cowboyScript == null) // Good to have a null check
        {
            Debug.LogWarning("CowboyScript is null in RagdollHitListener on " + gameObject.name);
            return;
        }

        // Assuming your bullet has the tag "Bullet"
        if (collision.gameObject.CompareTag("Bullet"))
        {
            // Make sure the cowboy script itself isn't already ragdolling.
            // While the Cowboy script has its own internal check (isRagdolling),
            // calling OnHit() is the more standard way to initiate the hit process.
            // OnHit() will then internally call EnableRagdollAnatomy if not already ragdolling.
            cowboyScript.OnHit(); // This is the preferred way to signal a hit
                                  // It will internally call EnableRagdollAnatomy if appropriate.
        }
    }
}