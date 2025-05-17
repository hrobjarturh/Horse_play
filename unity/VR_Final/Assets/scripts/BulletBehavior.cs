using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        //if collider has Pistol tag pass
        if (collision.collider.CompareTag("Pistol"))
        {
            return;
        }
        // Try to tell the hit object it was hit
        var hittable = collision.collider.GetComponentInParent<IHittable>();
        Debug.Log($"{gameObject.name} collided with {collision.gameObject.name}");
        if (hittable != null)
        {
            hittable.OnHit();  // let the object decide what to do
        }
        // turn gravity on 
        rb.useGravity = true; //turn gravity on
        



        //turn gravity on

    }

}
