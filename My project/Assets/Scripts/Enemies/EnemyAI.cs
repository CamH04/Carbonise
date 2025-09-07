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
    public float optimalRange = 6f;          
    public float minRange = 4f;              
    public float maxRange = 8f;              
    public float repositionSpeed = 2f;       

    [Header("Vision")]
    public float visionRange = 10f;          
    public float visionAngle = 45f;
    public Transform player;

    [Header("Shooting")]
    public float shootRange = 8f;
    public float shootDamage = 20f;
    public float fireRate = 1f;
    public float projectileSpeed = 100f;     
    public GameObject bulletPrefab;
    public Transform firePoint;
    public LayerMask obstacleMask;
    public AudioClip shootSound;
    public ParticleSystem muzzleFlash;

    [Header("Prediction Settings")]           
    public bool predictPlayerMovement = true;
    public float predictionTime = 0.3f;

    private float nextFireTime = 0f;
    private bool isShooting = false;
    private Vector3 playerLastPosition;        
    private Vector3 playerVelocity;            
    private float playerSpeedCheckInterval = 0.1f;
    private float nextSpeedCheck = 0f;

    [Header("Components")]
    private Rigidbody rb;
    private Animator animator;
    private AudioSource audioSource;
    private Vector3 patrolCenter;
    private float patrolAngle;
    private bool wasPlayerInVision = false;
    private float deathAnimLeng = 2f;

    private enum CombatState {
        Patrol,
        Chase,
        Combat,
        Reposition
    }
    private CombatState currentState = CombatState.Patrol;

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

        if (player != null) {
            playerLastPosition = player.position;
        }
    }

    void Update() {
        if (isDead) return;

        UpdatePlayerVelocity();

        bool playerCurrentlyInVision = PlayerInVision();
        float distanceToPlayer = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        if (playerCurrentlyInVision && !wasPlayerInVision) {
            OnPlayerSpotted();
        }
        else if (!playerCurrentlyInVision && wasPlayerInVision) {
            OnPlayerLost();
        }

        if (playerCurrentlyInVision && distanceToPlayer <= maxRange) {
            HandleCombatBehavior(distanceToPlayer);
        }
        else if (playerCurrentlyInVision) {
            ChasePlayer();
            currentState = CombatState.Chase;
        }
        else {
            PatrolCircle();
            currentState = CombatState.Patrol;
        }

        wasPlayerInVision = playerCurrentlyInVision;
    }

    void UpdatePlayerVelocity() {
        if (player == null || Time.time < nextSpeedCheck) return;

        Vector3 currentPlayerPos = player.position;
        playerVelocity = (currentPlayerPos - playerLastPosition) / playerSpeedCheckInterval;
        playerLastPosition = currentPlayerPos;
        nextSpeedCheck = Time.time + playerSpeedCheckInterval;
    }

    void HandleCombatBehavior(float distanceToPlayer) {
        if (distanceToPlayer < minRange) {
            RepositionAway();
            TryShoot();
            currentState = CombatState.Reposition;
        }
        else if (distanceToPlayer > maxRange) {
            ChasePlayer();
            currentState = CombatState.Chase;
        }
        else if (distanceToPlayer >= minRange && distanceToPlayer <= optimalRange + 1f) {
            StopAndShoot();
            currentState = CombatState.Combat;
        }
        else {
            if (distanceToPlayer > optimalRange) {
                MoveToOptimalRange();
            }
            TryShoot();
            currentState = CombatState.Combat;
        }
    }

    void RepositionAway() {
        if (player == null) return;

        isShooting = false;

        Vector3 awayDirection = (transform.position - player.position).normalized;
        awayDirection.y = 0;

        if (rb != null) {
            rb.MovePosition(transform.position + awayDirection * repositionSpeed * Time.deltaTime);
        }
        else {
            transform.position += awayDirection * repositionSpeed * Time.deltaTime;
        }

        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero) {
            transform.forward = lookDirection;
        }

        UpdateAnimator(false, false, false);
    }

    void MoveToOptimalRange() {
        if (player == null) return;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;

        float moveSpeed = chaseSpeed * 0.5f;

        if (rb != null) {
            rb.MovePosition(transform.position + dirToPlayer * moveSpeed * Time.deltaTime);
        }
        else {
            transform.position += dirToPlayer * moveSpeed * Time.deltaTime;
        }

        if (dirToPlayer != Vector3.zero) {
            transform.forward = dirToPlayer;
        }
    }

    void TryShoot() {
        if (!CanShootPlayer()) return;

        isShooting = true;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        if (dirToPlayer != Vector3.zero) {
            transform.forward = dirToPlayer;
        }

        if (Time.time >= nextFireTime) {
            ShootAtPlayer();
            nextFireTime = Time.time + (1f / fireRate);
        }

        if (animator != null) {
            animator.SetBool("IsShooting", true);
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsPatrolling", false);
        }
    }

    void OnPlayerSpotted() {
        Debug.Log("Player spotted!");
        UpdateAnimator(false, true, false);
    }

    void OnPlayerLost() {
        Debug.Log("Player lost!");
        isShooting = false;
        currentState = CombatState.Patrol;
        UpdateAnimator(true, false, false);
    }

    void PatrolCircle() {
        isShooting = false;
        UpdateAnimator(true, false, false);

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
        UpdateAnimator(false, true, false);

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
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        if (dirToPlayer != Vector3.zero) {
            transform.forward = dirToPlayer;
        }

        TryShoot();
    }

    void UpdateAnimator(bool patrolling, bool chasing, bool shooting) {
        if (animator == null) return;

        animator.SetBool("IsPatrolling", patrolling);
        animator.SetBool("IsChasing", chasing);
        animator.SetBool("IsShooting", shooting);
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

        Vector3 targetPosition;
        if (predictPlayerMovement && playerVelocity.magnitude > 0.1f) {
            targetPosition = player.position + playerVelocity * predictionTime;
        }
        else {
            targetPosition = player.position;
        }

        targetPosition += Vector3.up * 1f; 
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
        Gizmos.DrawWireSphere(transform.position, optimalRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }
}