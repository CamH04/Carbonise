using UnityEngine;

public class EnemyAI : MonoBehaviour {
    [Header("Stats")]
    public float maxHP = 100f;
    private float currentHP;
    private bool isDead = false;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float patrolRadius = 3f;
    public float stopDistance = 5f;

    [Header("Vision")]
    public float visionRange = 6f;
    public float visionAngle = 45f;
    public Transform player;

    [Header("Shooting")]
    public float shootRange = 8f;
    public float shootDamage = 20f;
    public float fireRate = 1f;
    public float projectileSpeed = 15f;
    public GameObject bulletPrefab; 
    public Transform firePoint;
    public LayerMask obstacleMask;
    public AudioClip shootSound;
    public ParticleSystem muzzleFlash;
    private float nextFireTime = 0f;
    private bool isShooting = false;

    [Header("Components")]
    private Rigidbody rb;
    private Animator animator;
    private AudioSource audioSource;
    private Vector3 patrolCenter;
    private float patrolAngle;
    private bool wasPlayerInVision = false;
    private float deathAnimLeng = 2f;

    

    void Start() {
        currentHP = maxHP;
        patrolCenter = transform.position;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (rb != null) {
            rb.freezeRotation = true;
        }

        if (animator == null) {
            Debug.LogWarning("No Animator component found on " + gameObject.name);
        }

        if (audioSource == null && shootSound != null) {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        if (firePoint == null) {
            GameObject firePointObj = new GameObject("FirePoint");
            firePointObj.transform.SetParent(transform);
            firePointObj.transform.localPosition = new Vector3(0, 1.5f, 1f);
            firePoint = firePointObj.transform;
        }
    }

    void Update() {
        if (isDead) return;

        bool playerCurrentlyInVision = PlayerInVision();
        float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        if (playerCurrentlyInVision && !wasPlayerInVision) {
            OnPlayerSpotted();
        }
        else if (!playerCurrentlyInVision && wasPlayerInVision) {
            OnPlayerLost();
        }

        if (playerCurrentlyInVision) {
            if (distanceToPlayer <= stopDistance && CanShootPlayer()) {
                StopAndShoot();
            }
            else {
                ChasePlayer();
            }
        }
        else {
            PatrolCircle();
        }

        wasPlayerInVision = playerCurrentlyInVision;
    }

    void OnPlayerSpotted() {
        Debug.Log("Player spotted!");

        if (animator != null) {
            animator.SetTrigger("PlayerSpotted");
            animator.SetBool("IsChasing", true);
            animator.SetBool("IsPatrolling", false);
        }
    }

    void OnPlayerLost() {
        Debug.Log("Player lost!");
        isShooting = false;

        if (animator != null) {
            animator.SetTrigger("PlayerLost");
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsPatrolling", true);
            animator.SetBool("IsShooting", false);
        }
    }

    void PatrolCircle() {
        isShooting = false;

        if (animator != null && !wasPlayerInVision) {
            animator.SetBool("IsPatrolling", true);
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsShooting", false);
        }

        patrolAngle += patrolSpeed * Time.deltaTime;
        float x = Mathf.Cos(patrolAngle) * patrolRadius;
        float z = Mathf.Sin(patrolAngle) * patrolRadius;
        Vector3 targetPos = new Vector3(patrolCenter.x + x, transform.position.y, patrolCenter.z + z);
        Vector3 direction = (targetPos - transform.position).normalized;

        if (rb != null) {
            rb.MovePosition(transform.position + direction * patrolSpeed * Time.deltaTime);
        }
        else {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, patrolSpeed * Time.deltaTime);
        }

        if (direction != Vector3.zero) {
            transform.forward = direction;
        }
    }

    void ChasePlayer() {
        isShooting = false;

        if (animator != null) {
            animator.SetBool("IsChasing", true);
            animator.SetBool("IsPatrolling", false);
            animator.SetBool("IsShooting", false);
        }

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;

        if (rb != null) {
            rb.MovePosition(transform.position + dir * chaseSpeed * Time.deltaTime);
        }
        else {
            transform.position += dir * chaseSpeed * Time.deltaTime;
        }

        if (dir != Vector3.zero) {
            transform.forward = dir;
        }
    }

    void StopAndShoot() {
        isShooting = true;

        if (animator != null) {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsPatrolling", false);
            animator.SetBool("IsShooting", true);
        }

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        if (dirToPlayer != Vector3.zero) {
            transform.forward = dirToPlayer;
        }

        if (Time.time >= nextFireTime) {
            ShootAtPlayer();
            nextFireTime = Time.time + (1f / fireRate);
        }
    }

    void ShootAtPlayer() {
        if (player == null || bulletPrefab == null || firePoint == null) return;

        if (animator != null) {
            animator.SetTrigger("Shoot");
        }

        if (muzzleFlash != null) {
            muzzleFlash.Play();
        }

        if (audioSource != null && shootSound != null) {
            audioSource.PlayOneShot(shootSound);
        }

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Vector3 targetPosition = player.position + Vector3.up * 1f;
        Vector3 shootDirection = (targetPosition - firePoint.position).normalized;

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null) {
            bulletRb.linearVelocity = shootDirection * projectileSpeed;
        }

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript == null) {
            bulletScript = bullet.AddComponent<Bullet>();
        }
        bulletScript.damage = shootDamage;
        bulletScript.shooter = this.gameObject;
    }

    bool CanShootPlayer() {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer > shootRange) return false;

        Vector3 startPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * 1f;
        Vector3 directionToPlayer = (targetPos - startPos).normalized;

        RaycastHit hit;
        float rayDistance = Vector3.Distance(startPos, targetPos);

        if (Physics.Raycast(startPos, directionToPlayer, out hit, rayDistance, obstacleMask)) {
            return false;
        }
        return true;
    }

    bool PlayerInVision() {
        if (player == null) return false;

        Vector3 dirToPlayer = player.position - transform.position;
        float distance = dirToPlayer.magnitude;

        if (distance > visionRange) return false;

        float angle = Vector3.Angle(transform.forward, dirToPlayer.normalized);
        return angle < visionAngle;
    }

    public void TakeDamage(float dmg) {
        if (isDead) return;

        currentHP -= dmg;

        if (animator != null) {
            animator.SetTrigger("TakeDamage");
        }

        if (currentHP <= 0) Die();
    }

    void Die() {
        isDead = true;

        if (animator != null) {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsPatrolling", false);
            animator.SetBool("IsShooting", false);
            animator.SetTrigger("Die");
        }

        if (rb != null) {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Destroy(gameObject, deathAnimLeng);
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * visionRange);

        Vector3 rightLimit = Quaternion.Euler(0, visionAngle, 0) * transform.forward;
        Vector3 leftLimit = Quaternion.Euler(0, -visionAngle, 0) * transform.forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + rightLimit * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + leftLimit * visionRange);

        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, shootRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}