using UnityEngine;

public class Bullet : MonoBehaviour {
    [Header("Bullet Settings")]
    public float damage = 20f;
    public float lifetime = 5f;
    public GameObject impactEffect;
    public LayerMask whatHitsMe = -1;

    [Header("Trail Settings")]
    public bool hasTrail = true;
    public float trailTime = 0.5f;

    [HideInInspector]
    public GameObject shooter;

    private Rigidbody rb;
    private bool hasHit = false;
    private TrailRenderer trail;

    void Start() {
        rb = GetComponent<Rigidbody>();
        if (hasTrail && GetComponent<TrailRenderer>() == null) {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = 0.05f;
            trail.endWidth = 0.01f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = Color.yellow;
            trail.endColor = Color.yellow;
        }
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other) {
        if (hasHit) return;
        if (other.gameObject == shooter) return;
        if (((1 << other.gameObject.layer) & whatHitsMe) == 0) return;

        hasHit = true;
        Debug.Log($"Bullet hit: {other.name} with tag: {other.tag}");

        if (other.CompareTag("Player")) {
            Debug.Log($"Player hit for {damage} damage!");
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null) {
                playerHealth.TakeDamage(damage);
            }
        }
        else if (other.GetComponent<EnemyAI>() != null) {
            Debug.Log($"Enemy hit for {damage} damage!");
            other.GetComponent<EnemyAI>().TakeDamage(damage);
        }
        else {
            Debug.Log("Bullet hit environment: " + other.name);
        }
        if (impactEffect != null) {
            Vector3 impactDirection = rb != null ? -rb.linearVelocity.normalized : -transform.forward;
            GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.LookRotation(impactDirection));
            Destroy(effect, 2f);
        }

        DestroyBullet();
    }

    void OnCollisionEnter(Collision collision) {
        OnTriggerEnter(collision.collider);
    }

    void DestroyBullet() {
        if (rb != null) {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) {
            meshRenderer.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) {
            col.enabled = false;
        }
        float destroyDelay = hasTrail && trail != null ? trail.time : 0.1f;
        Destroy(gameObject, destroyDelay);
    }
}