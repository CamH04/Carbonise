using UnityEngine;

public class Bullet : MonoBehaviour {
    [Header("Bullet Settings")]
    public float damage = 20f;
    public float speed = 50f;                
    public float lifetime = 5f;
    public GameObject impactEffect;
    public LayerMask whatHitsMe = -1;

    [Header("Trail Settings")]
    public bool hasTrail = true;
    public float trailTime = 0.5f;
    public Color trailStartColor = Color.yellow;
    public Color trailEndColor = Color.red;
    public float trailStartWidth = 0.05f;
    public float trailEndWidth = 0.01f;

    [Header("Visual Effects")]
    public bool glowEffect = true;          
    public Color bulletColor = Color.yellow; 

    [HideInInspector]
    public GameObject shooter;

    private Rigidbody rb;
    private bool hasHit = false;
    private TrailRenderer trail;
    private Light bulletLight;              

    void Start() {
        rb = GetComponent<Rigidbody>();

        SetupTrail();
        SetupGlowEffect();
        ApplyBulletColor();
        Destroy(gameObject, lifetime);
    }

    void SetupTrail() {
        if (hasTrail && GetComponent<TrailRenderer>() == null) {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = trailStartWidth;
            trail.endWidth = trailEndWidth;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = trailStartColor;
            trail.endColor = trailEndColor;
        }
        else if (hasTrail) {
            trail = GetComponent<TrailRenderer>();
        }
    }

    void SetupGlowEffect() {
        if (glowEffect) {
            bulletLight = gameObject.AddComponent<Light>();
            bulletLight.type = LightType.Point;
            bulletLight.color = bulletColor;
            bulletLight.intensity = 2f;
            bulletLight.range = 3f;
            bulletLight.shadows = LightShadows.None;
        }
    }

    void ApplyBulletColor() {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) {
            Material mat = meshRenderer.material;
            if (mat.HasProperty("_Color")) {
                mat.color = bulletColor;
            }
            if (mat.HasProperty("_EmissionColor")) {
                mat.SetColor("_EmissionColor", bulletColor * 0.5f);
                mat.EnableKeyword("_EMISSION");
            }
        }
    }
    public void SetVelocity(Vector3 direction) {
        if (rb != null) {
            rb.linearVelocity = direction.normalized * speed;
        }
    }
    public void SetSpeed(float newSpeed) {
        speed = newSpeed;
        if (rb != null && rb.linearVelocity != Vector3.zero) {
            rb.linearVelocity = rb.linearVelocity.normalized * speed;
        }
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
        CreateImpactEffect();

        DestroyBullet();
    }

    void OnCollisionEnter(Collision collision) {
        OnTriggerEnter(collision.collider);
    }

    void CreateImpactEffect() {
        if (impactEffect != null) {
            Vector3 impactDirection = rb != null ? -rb.linearVelocity.normalized : -transform.forward;
            GameObject effect = Instantiate(impactEffect, transform.position, Quaternion.LookRotation(impactDirection));
            Destroy(effect, 2f);
        }
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
        if (bulletLight != null) {
            bulletLight.enabled = false;
        }
        float destroyDelay = hasTrail && trail != null ? trail.time : 0.1f;
        Destroy(gameObject, destroyDelay);
    }
}