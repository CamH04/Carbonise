using UnityEngine;
using UnityEngine.InputSystem;

public class HitscanWeapon : MonoBehaviour {
    [Header("Weapon Settings")]
    public float damage = 50f; //2 shots body to kill
    public float fireRate = 0.1f;
    public float range = 100f;
    public LayerMask hitLayers = -1;

    [Header("Visual Effects")]
    public LineRenderer muzzleFlash;
    public float muzzleFlashDuration = 0.05f;
    public ParticleSystem muzzleParticles;
    public GameObject hitEffect;

    [Header("Bullet Impact Visualization")]
    public GameObject bulletImpactMarker;
    public float markerLifetime = 3f;
    public bool showTrailLine = true;
    public LineRenderer trailLine;
    public float trailDuration = 0.2f;
    public bool showImpactSphere = true;
    public float impactSphereSize = 0.2f;
    public Material impactSphereMaterial;

    [Header("UI Impact Indicator")]
    public bool showUIIndicator = true;
    public GameObject impactUIIndicator;
    public Canvas worldCanvas;
    public float uiIndicatorLifetime = 1f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float shootVolume = 0.7f;
    [Range(0f, 1f)]
    public float hitVolume = 0.5f;

    [Header("Crosshair & UI")]
    public GameObject crosshair;
    public Color normalCrosshairColor = Color.white;
    public Color hitCrosshairColor = Color.red;
    public float crosshairHitDuration = 0.1f;

    [Header("Recoil")]
    public bool enableRecoil = true;
    public float recoilAmount = 2f;
    public float recoilRecovery = 5f;

    private Camera playerCamera;
    private AudioSource audioSource;
    private PlayerInput playerInput;
    private InputAction shootAction;
    private float lastShotTime;
    private bool isShooting;
    private UnityEngine.UI.Image crosshairImage;
    private float crosshairTimer;
    private Vector2 recoilOffset;

    void Awake() {
        playerCamera = GetComponentInParent<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null) {
            shootAction = playerInput.actions["Shoot"];
            if (shootAction != null) {
                shootAction.performed += OnShootPerformed;
                shootAction.canceled += OnShootCanceled;
            }
        }
        if (crosshair != null) {
            crosshairImage = crosshair.GetComponent<UnityEngine.UI.Image>();
            if (crosshairImage != null)
                crosshairImage.color = normalCrosshairColor;
        }
        if (muzzleFlash != null) {
            muzzleFlash.enabled = false;
            muzzleFlash.useWorldSpace = false;
        }
        if (trailLine != null) {
            trailLine.enabled = false;
            trailLine.useWorldSpace = true;
        }
        if (worldCanvas == null && showUIIndicator) {
            GameObject canvasObj = new GameObject("BulletImpactCanvas");
            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = playerCamera;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
    }

    void Start() {
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDestroy() {
        if (shootAction != null) {
            shootAction.performed -= OnShootPerformed;
            shootAction.canceled -= OnShootCanceled;
        }
    }

    void Update() {
        HandleShooting();
        HandleCrosshair();
        HandleRecoil();
    }

    private void OnShootPerformed(InputAction.CallbackContext context) {
        isShooting = true;
    }

    private void OnShootCanceled(InputAction.CallbackContext context) {
        isShooting = false;
    }

    void HandleShooting() {
        if (isShooting && Time.time >= lastShotTime + fireRate) {
            Shoot();
            lastShotTime = Time.time;
        }
    }

    void Shoot() {
        if (shootSound != null && audioSource != null) {
            audioSource.PlayOneShot(shootSound, shootVolume);
        }
        if (enableRecoil) {
            ApplyRecoilImpulse();
        }
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit hit;

        Vector3 impactPoint;
        bool hitSomething = Physics.Raycast(ray, out hit, range, hitLayers);
        if (hitSomething) {
            impactPoint = hit.point;
            ShowBulletImpact(impactPoint, hit.normal, true);
            EnemyAI enemy = hit.collider.GetComponent<EnemyAI>();
            if (enemy != null) {
                enemy.TakeDamage(damage);
                ShowHitFeedback();
                if (hitSound != null && audioSource != null) {
                    audioSource.PlayOneShot(hitSound, hitVolume);
                }

                Debug.Log($"Hit enemy at: {impactPoint} for {damage} damage!");
            }
            else {
                Debug.Log($"Hit {hit.collider.name} at: {impactPoint}");
            }
            if (hitEffect != null) {
                GameObject effect = Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(effect, 2f);
            }
        }
        else {
            impactPoint = ray.origin + ray.direction * range;
            ShowBulletImpact(impactPoint, -ray.direction, false);
            Debug.Log($"Bullet missed, traveled to: {impactPoint}");
        }
        ShowMuzzleFlash(impactPoint);
        if (showTrailLine) {
            ShowBulletTrail(transform.position, impactPoint);
        }
        if (muzzleParticles != null) {
            muzzleParticles.Play();
        }
    }

    void ApplyRecoilImpulse() {
        float recoilX = Random.Range(-recoilAmount * 0.5f, recoilAmount * 0.5f);
        float recoilY = recoilAmount;

        recoilOffset.x += recoilX;
        recoilOffset.y += recoilY;

        recoilOffset.x = Mathf.Clamp(recoilOffset.x, -recoilAmount * 3f, recoilAmount * 3f);
        recoilOffset.y = Mathf.Clamp(recoilOffset.y, 0f, recoilAmount * 5f);
    }

    void ShowMuzzleFlash(Vector3 targetPoint) {
        if (muzzleFlash == null) return;

        muzzleFlash.enabled = true;
        muzzleFlash.positionCount = 2;
        muzzleFlash.SetPosition(0, transform.position);
        muzzleFlash.SetPosition(1, targetPoint);
        Invoke(nameof(HideMuzzleFlash), muzzleFlashDuration);
    }

    void HideMuzzleFlash() {
        if (muzzleFlash != null)
            muzzleFlash.enabled = false;
    }

    void ShowBulletImpact(Vector3 impactPoint, Vector3 surfaceNormal, bool hitSomething) {
        if (showImpactSphere) {
            GameObject impactSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impactSphere.name = hitSomething ? "Bullet Hit" : "Bullet Miss";
            impactSphere.transform.position = impactPoint;
            impactSphere.transform.localScale = Vector3.one * impactSphereSize;

            Collider col = impactSphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer renderer = impactSphere.GetComponent<Renderer>();
            if (renderer != null) {
                if (impactSphereMaterial != null) {
                    renderer.material = impactSphereMaterial;
                }

                Color impactColor = Color.blue;
                if (hitSomething) {
                    Ray checkRay = new Ray(playerCamera.transform.position, (impactPoint - playerCamera.transform.position).normalized);
                    RaycastHit checkHit;
                    if (Physics.Raycast(checkRay, out checkHit, range, hitLayers)) {
                        if (checkHit.collider.GetComponent<EnemyAI>() != null) {
                            impactColor = Color.green;
                        }
                        else {
                            impactColor = Color.red;
                        }
                    }
                }
                renderer.material.color = impactColor;
            }

            if (renderer != null && renderer.material.HasProperty("_EmissionColor")) {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", renderer.material.color * 0.5f);
            }

            Destroy(impactSphere, markerLifetime);
        }

        if (showUIIndicator && impactUIIndicator != null && worldCanvas != null) {
            GameObject uiIndicator = Instantiate(impactUIIndicator, worldCanvas.transform);
            uiIndicator.transform.position = impactPoint;
            uiIndicator.transform.LookAt(playerCamera.transform);
            uiIndicator.transform.Rotate(0, 180, 0);

            float distance = Vector3.Distance(playerCamera.transform.position, impactPoint);
            float scale = distance * 0.01f;
            uiIndicator.transform.localScale = Vector3.one * scale;

            Destroy(uiIndicator, uiIndicatorLifetime);
        }
        if (bulletImpactMarker != null) {
            GameObject marker = Instantiate(bulletImpactMarker, impactPoint, Quaternion.LookRotation(surfaceNormal));
            Destroy(marker, markerLifetime);
        }
        Debug.DrawRay(transform.position, (impactPoint - transform.position).normalized * Vector3.Distance(transform.position, impactPoint),
                     hitSomething ? Color.red : Color.yellow, 2f);
    }

    void ShowBulletTrail(Vector3 startPoint, Vector3 endPoint) {
        if (trailLine == null) return;

        trailLine.enabled = true;
        trailLine.positionCount = 2;
        trailLine.SetPosition(0, startPoint);
        trailLine.SetPosition(1, endPoint);

        Invoke(nameof(HideTrailLine), trailDuration);
    }

    void HideTrailLine() {
        if (trailLine != null)
            trailLine.enabled = false;
    }

    void ShowHitFeedback() {
        if (crosshairImage != null) {
            crosshairImage.color = hitCrosshairColor;
            crosshairTimer = crosshairHitDuration;
        }
    }

    void HandleCrosshair() {
        if (crosshairImage == null) return;

        if (crosshairTimer > 0f) {
            crosshairTimer -= Time.deltaTime;
            if (crosshairTimer <= 0f) {
                crosshairImage.color = normalCrosshairColor;
            }
        }
    }

    void HandleRecoil() {
        if (!enableRecoil || playerCamera == null) return;
        if (recoilOffset.magnitude > 0.01f) {
            recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * recoilRecovery);
            Transform cameraTransform = playerCamera.transform;
            Vector3 currentRotation = cameraTransform.localEulerAngles;

            float currentX = currentRotation.x > 180 ? currentRotation.x - 360 : currentRotation.x;
            float currentY = currentRotation.y > 180 ? currentRotation.y - 360 : currentRotation.y;

            cameraTransform.localEulerAngles = new Vector3(
                currentX - recoilOffset.y * Time.deltaTime * 60f,
                currentY + recoilOffset.x * Time.deltaTime * 30f, 
                currentRotation.z
            );
        }
    }

    public void SetPlayerCamera(Camera camera) {
        playerCamera = camera;
    }

    void OnDrawGizmosSelected() {
        if (playerCamera == null) return;

        Gizmos.color = Color.red;
        Vector3 forward = playerCamera.transform.forward;
        Gizmos.DrawRay(playerCamera.transform.position, forward * range);
    }
}