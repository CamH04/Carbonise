using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour {
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI References")]
    public Slider healthSlider;
    public Image healthFillImage;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;

    [Header("Damage Effects")]
    public float damageCooldown = 0f; 
    public AudioClip hurtSound;
    public GameObject damageEffect;

    private float lastDamageTime;
    private AudioSource audioSource;
    private bool isDead = false;

    void Start() {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null && hurtSound != null) {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        UpdateHealthUI();
    }

    public void TakeDamage(float damage) {
        if (isDead) return;
        if (Time.time < lastDamageTime + damageCooldown) {
            return;
        }

        lastDamageTime = Time.time;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
        if (audioSource != null && hurtSound != null) {
            audioSource.PlayOneShot(hurtSound);
        }
        if (damageEffect != null) {
            GameObject effect = Instantiate(damageEffect, transform.position + Vector3.up, Quaternion.identity);
            Destroy(effect, 2f);
        }
        UpdateHealthUI();
        if (currentHealth <= 0 && !isDead) {
            Die();
        }
    }

    public void Heal(float amount) {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthUI();
    }

    void Die() {
        isDead = true;
        Debug.Log("Player died!");

        //TODO add death logic eg show died screen so on lol
    }

    void UpdateHealthUI() {
        if (healthSlider != null) {
            healthSlider.value = currentHealth / maxHealth;
        }

        if (healthFillImage != null) {
            float healthPercentage = currentHealth / maxHealth;
            healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
        }
    }
    public bool IsDead() {
        return isDead;
    }
    public float GetHealthPercentage() {
        return currentHealth / maxHealth;
    }
}