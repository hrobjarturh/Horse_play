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
        if (collision.gameObject.CompareTag("Bullet")) // assumes your bullet has the tag
        {
            //make cowboy larger
            cowboyScript.EnableRagdoll();
        }
    }
}
