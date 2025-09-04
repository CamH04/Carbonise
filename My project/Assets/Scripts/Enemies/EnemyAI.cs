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

    [Header("Vision")]
    public float visionRange = 6f;
    public float visionAngle = 45f;
    public Transform player;

    [Header("Components")]
    private Rigidbody rb;
    private Animator animator;
    private Vector3 patrolCenter;
    private float patrolAngle;
    private bool wasPlayerInVision = false;
    private float deathAnimLeng = 2f;

    void Start() {
        currentHP = maxHP;
        patrolCenter = transform.position;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (rb != null) {
            rb.freezeRotation = true;
        }

        if (animator == null) {
            Debug.LogWarning("No Animator component found on " + gameObject.name);
        }
    }

    void Update() {
        if (isDead) return;

        bool playerCurrentlyInVision = PlayerInVision();

        if (playerCurrentlyInVision && !wasPlayerInVision) {
            OnPlayerSpotted();
        }
        else if (!playerCurrentlyInVision && wasPlayerInVision) {
            OnPlayerLost();
        }

        if (playerCurrentlyInVision) {
            ChasePlayer();
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

        if (animator != null) {
            animator.SetTrigger("PlayerLost");
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsPatrolling", true);
        }
    }

    void PatrolCircle() {
        if (animator != null && !wasPlayerInVision) {
            animator.SetBool("IsPatrolling", true);
            animator.SetBool("IsChasing", false);
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
        if (animator != null) {
            animator.SetBool("IsChasing", true);
            animator.SetBool("IsPatrolling", false);
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
    }
}