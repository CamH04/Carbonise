using UnityEngine;

public class EnemyAI : MonoBehaviour {
    [Header("Stats")]
    public float maxHP = 100f;
    private float currentHP;

    [Header("Movement")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float patrolRadius = 3f;

    [Header("Vision")]
    public float visionRange = 6f;
    public float visionAngle = 45f;
    public Transform player;

    private Vector3 patrolCenter;
    private float patrolAngle;

    void Start() {
        currentHP = maxHP;
        patrolCenter = transform.position;
    }

    void Update() {
        if (PlayerInVision()) {
            ChasePlayer();
        }
        else {
            PatrolCircle();
        }
    }

    void PatrolCircle() {
        patrolAngle += patrolSpeed * Time.deltaTime;
        float x = Mathf.Cos(patrolAngle) * patrolRadius;
        float z = Mathf.Sin(patrolAngle) * patrolRadius;
        Vector3 newPos = new Vector3(patrolCenter.x + x, transform.position.y, patrolCenter.z + z);
        transform.position = Vector3.MoveTowards(transform.position, newPos, patrolSpeed * Time.deltaTime);
        Vector3 dir = (newPos - transform.position).normalized;
        if (dir != Vector3.zero) transform.forward = dir;
    }

    void ChasePlayer() {
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * chaseSpeed * Time.deltaTime;
        transform.forward = dir;
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
        currentHP -= dmg;
        if (currentHP <= 0) Die();
    }

    void Die() {
        Destroy(gameObject);
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
