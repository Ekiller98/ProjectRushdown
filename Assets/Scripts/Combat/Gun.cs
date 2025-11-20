using Unity.Netcode;
using UnityEngine;
using System;

public class Gun : NetworkBehaviour
{
    [Header("Gun Data")]
    public GunData gunData; // ScriptableObject with stats

    [Header("Gun Settings (runtime from GunData)")]
    public float fireRate = 10f;      // shots per second
    public float range = 100f;
    public int damage = 25;

    [Header("Shot Pattern")]
    [Tooltip("1 = normal hitscan, >1 = shotgun-style multi pellets")]
    public int pelletsPerShot = 1;

    [Header("Damage Multipliers")]
    [Tooltip("How much to multiply damage when hitting the head")]
    public float headshotMultiplier = 2f;

    [Header("ADS")]
    [Range(0f, 1f)]
    public float adsSpreadMultiplier = 0.4f;  // 0.4 = 60% tighter when aiming

    [Tooltip("Max spread angle in degrees for each pellet")]
    public float spreadAngle = 0f;

    [Header("Ammo")]
    public int magazineSize = 12;     // bullets per mag
    public int maxReserveAmmo = 120;  // how much extra ammo you can hold
    public float reloadTime = 1.2f;   // seconds

    public event Action OnAmmoPickup;

    [Header("References")]
    public Transform cameraTransform;    // player's camera
    public Transform muzzleTransform;    // muzzle position

    [Header("VFX")]
    public GameObject muzzleFlashPrefab;
    public GameObject hitImpactPrefab;
    public GameObject tracerPrefab;

    [Header("Audio")]
    public AudioSource audioSource;

    [Tooltip("Single gunshot sound (short clip)")]
    public AudioClip shotClip;
    [Range(0f, 1f)] public float shotVolume = 1f;

    [Tooltip("Optional: click when mag is empty")]
    public AudioClip dryFireClip;

    [Tooltip("Hitmarker ding for the shooter only")]
    public AudioClip hitmarkerClip;

    [Header("Reload Audio")]
    [Tooltip("Reload sound for this specific gun")]
    public AudioClip reloadClip;
    [Range(0f, 3f)]
    public float reloadVolume = 1.5f;

    // runtime ammo
    private int currentMagAmmo;
    private int currentReserveAmmo;
    private bool isReloading = false;

    private float nextFireTime = 0f;

    // small debounce so dry-fire spam doesn’t get crunchy
    private float lastDryFireTime = 0f;
    private const float dryFireCooldown = 0.08f;

    // ---------------------------------------------------
    // GUN DATA
    // ---------------------------------------------------

    public void ApplyGunData(GunData newData)
    {
        if (newData == null)
        {
            Debug.LogWarning("[Gun] Tried to apply null GunData.");
            return;
        }

        gunData = newData;

        // Core stats
        damage = gunData.damage;
        fireRate = gunData.fireRate;
        range = gunData.range;

        // ADS stats
        adsSpreadMultiplier = gunData.adsSpreadMultiplier;

        // Shot pattern
        pelletsPerShot = Mathf.Max(1, gunData.pelletsPerShot);
        spreadAngle = Mathf.Max(0f, gunData.spreadAngle);

        // Ammo stats
        magazineSize = Mathf.Max(1, gunData.magSize);
        maxReserveAmmo = Mathf.Max(0, gunData.maxReserveAmmo);
        reloadTime = Mathf.Max(0.01f, gunData.reloadTime);

        // 🔊 Audio from GunData
        shotClip = gunData.shotClip;
        shotVolume = gunData.shotVolume;
        dryFireClip = gunData.dryFireClip;
        reloadClip = gunData.reloadClip;
        reloadVolume = gunData.reloadVolume;
        hitmarkerClip = gunData.hitmarkerClip;

        // Give full ammo when applying a new gun
        currentMagAmmo = magazineSize;
        currentReserveAmmo = maxReserveAmmo;
    }


    // ---------------------------------------------------
    // LIFECYCLE
    // ---------------------------------------------------

    private void Awake()
    {
        // 1) Apply GunData if present (stats + ammo)
        if (gunData != null)
        {
            ApplyGunData(gunData);
        }
        else
        {
            // No GunData? Use inspector values
            currentMagAmmo = magazineSize;
            currentReserveAmmo = maxReserveAmmo;
            pelletsPerShot = Mathf.Max(1, pelletsPerShot);
        }

        // 2) Auto find camera if not set
        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }

        // 3) Auto fallback for muzzle if not set
        if (muzzleTransform == null)
            muzzleTransform = cameraTransform;

        // 4) AudioSource auto-find
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;      // we use PlayOneShot per bullet
            audioSource.spatialBlend = 0f;        // 2D for now
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (cameraTransform == null) return;

        if (RoundManager.Instance != null && !RoundManager.Instance.roundInProgress.Value)
            return;

        if (isReloading)
        {
            return;
        }

        // Right-click = aiming down sights
        bool isAiming = Input.GetMouseButton(1);

        // Choose input behavior based on fire mode
        bool wantToFire = false;

        if (gunData != null && gunData.fireMode == FireMode.SemiAuto)
        {
            // semi: click once per shot
            wantToFire = Input.GetMouseButtonDown(0);
        }
        else
        {
            // full auto (or no GunData): hold to fire
            wantToFire = Input.GetMouseButton(0);
        }

        if (wantToFire && Time.time >= nextFireTime)
        {
            TryShoot(isAiming);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }
    }

    // ---------------------------------------------------
    // SHOOT / RELOAD (LOCAL)
    // ---------------------------------------------------

    private void TryShoot(bool isAiming)
    {
        // Always advance the cooldown (even on dry fire)
        float shotsPerSecond = Mathf.Max(0.01f, fireRate);
        nextFireTime = Time.time + (1f / shotsPerSecond);

        // No ammo in mag → just play dry fire and bail
        if (currentMagAmmo <= 0)
        {
            PlayDryFire();
            return;
        }

        currentMagAmmo--;

        Vector3 cameraOrigin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;

        Vector3 muzzleOrigin = muzzleTransform.position;

        PlayShotSoundLocal();
        ShootServerRpc(cameraTransform.position, cameraTransform.forward, isAiming);

    }

    private void TryReload()
    {
        if (isReloading) return;
        if (currentMagAmmo >= magazineSize) return;
        if (currentReserveAmmo <= 0) return;

        StartCoroutine(ReloadRoutine());
    }

    private System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;

        // 🔊 play local reload sound
        PlayReloadSoundLocal();

        yield return new WaitForSeconds(reloadTime);

        int needed = magazineSize - currentMagAmmo;
        int toLoad = Mathf.Min(needed, currentReserveAmmo);

        currentMagAmmo += toLoad;
        currentReserveAmmo -= toLoad;

        isReloading = false;
    }

    private void PlayShotSoundLocal()
    {
        if (audioSource == null || shotClip == null) return;

        audioSource.PlayOneShot(shotClip, shotVolume);
    }

    private void PlayDryFire()
    {
        if (audioSource == null || dryFireClip == null) return;

        if (Time.time - lastDryFireTime < dryFireCooldown)
            return;

        lastDryFireTime = Time.time;
        audioSource.PlayOneShot(dryFireClip, 0.8f);
    }
    private void PlayReloadSoundLocal()
    {
        if (audioSource == null || reloadClip == null)
            return;

        audioSource.PlayOneShot(reloadClip, reloadVolume);
    }


    // ---------------------------------------------------
    // SERVER: HIT DETECTION (WITH PELLETS / SPREAD)
    // ---------------------------------------------------

    [ServerRpc]
    private void ShootServerRpc(Vector3 clientCamPos, Vector3 clientForward, bool isAiming)
    {
        int pellets = Mathf.Max(1, pelletsPerShot);

        float baseSpread = Mathf.Max(0f, spreadAngle);
        float spread = isAiming ? baseSpread * adsSpreadMultiplier : baseSpread;

        bool anyHit = false;
        Vector3 firstHitPoint = clientCamPos + clientForward * range;

        for (int i = 0; i < pellets; i++)
        {
            // Spread applied relative to camera forward
            Vector3 shotDir = GetSpreadDirection(clientForward, spread);

            // Raycast from CAMERA (not muzzle)
            bool hitSomething = Physics.Raycast(
                clientCamPos,
                shotDir,
                out RaycastHit hitInfo,
                range,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide
            );

            if (i == 0)
            {
                firstHitPoint = hitSomething
                    ? hitInfo.point
                    : clientCamPos + shotDir * range;
                anyHit = hitSomething;
            }

            if (!hitSomething)
                continue;

            var targetNO = hitInfo.collider.GetComponentInParent<NetworkObject>();
            if (targetNO == null)
                continue;

            var targetTeam = targetNO.GetComponent<PlayerTeam>();
            var myTeam = GetComponent<PlayerTeam>();

            if (myTeam != null && targetTeam != null &&
                myTeam.teamId.Value == targetTeam.teamId.Value)
            {
                continue;
            }

            var targetHealth = targetNO.GetComponent<PlayerHealth>();
            if (targetHealth != null)
            {
                bool isHeadshot = hitInfo.collider.CompareTag("Head");

                int finalDamage = isHeadshot
                    ? Mathf.RoundToInt(damage * headshotMultiplier)
                    : damage;

                targetHealth.ApplyDamage(finalDamage);

                if (i == 0)
                    PlayHitmarkerClientRpc(OwnerClientId);
            }
        }

        // Spawn tracer + muzzle flash using muzzle position
        Vector3 muzzleOrigin = muzzleTransform.position;
        SpawnShootVFXClientRpc(muzzleOrigin, firstHitPoint, anyHit);
    }

    // Helper: make a random direction within a cone of 'angleDeg' around 'forward'
    private Vector3 GetSpreadDirection(Vector3 forward, float angleDeg)
    {
        if (angleDeg <= 0f)
            return forward.normalized;

        forward = forward.normalized;

        // build orthonormal basis from forward
        Vector3 right = Vector3.Cross(forward, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(forward, Vector3.forward);
        right.Normalize();

        Vector3 up = Vector3.Cross(right, forward).normalized;

        float spreadRadius = Mathf.Tan(angleDeg * Mathf.Deg2Rad);
        Vector2 rand = UnityEngine.Random.insideUnitCircle * spreadRadius;

        Vector3 offset = right * rand.x + up * rand.y;
        return (forward + offset).normalized;
    }

    // ---------------------------------------------------
    // CLIENT RPCs
    // ---------------------------------------------------

    [ClientRpc]
    private void PlayHitmarkerClientRpc(ulong targetClientId)
    {
        if (OwnerClientId != targetClientId) return; // only shooter hears it

        if (audioSource != null && hitmarkerClip != null)
        {
            audioSource.PlayOneShot(hitmarkerClip, 1f);
        }
    }

    [ClientRpc]
    private void SpawnShootVFXClientRpc(Vector3 origin, Vector3 hitPoint, bool hitSomething)
    {
        // muzzle flash
        if (muzzleFlashPrefab != null && muzzleTransform != null)
        {
            var fx = Instantiate(muzzleFlashPrefab, muzzleTransform.position, muzzleTransform.rotation);
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                Destroy(fx, main.duration + main.startLifetime.constantMax);
            }
            else Destroy(fx, 0.5f);
        }

        // tracer
        if (tracerPrefab != null)
        {
            var tracer = Instantiate(tracerPrefab);
            var line = tracer.GetComponent<LineRenderer>();

            if (line != null)
            {
                line.positionCount = 2;
                line.SetPosition(0, origin);
                line.SetPosition(1, hitPoint);
            }

            Destroy(tracer, 0.05f);
        }

        // hit impact
        if (hitSomething && hitImpactPrefab != null)
        {
            var fx = Instantiate(hitImpactPrefab, hitPoint, Quaternion.identity);
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                Destroy(fx, main.duration + main.startLifetime.constantMax);
            }
            else Destroy(fx, 0.5f);
        }
    }

    [ClientRpc]
    private void SpawnImpactClientRpc(Vector3 hitPoint)
    {
        if (hitImpactPrefab == null)
            return;

        var fx = Instantiate(hitImpactPrefab, hitPoint, Quaternion.identity);
        var ps = fx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            Destroy(fx, main.duration + main.startLifetime.constantMax);
        }
        else
        {
            Destroy(fx, 0.5f);
        }
    }


    // ---------------------------------------------------
    // PUBLIC HELPERS (for pickups / HUD)
    // ---------------------------------------------------

    public void GiveFullAmmo()
    {
        currentMagAmmo = magazineSize;
        currentReserveAmmo = maxReserveAmmo;
        OnAmmoPickup?.Invoke();
    }

    public void AddReserveAmmo(int amount)
    {
        currentReserveAmmo = Mathf.Clamp(currentReserveAmmo + amount, 0, maxReserveAmmo);
        OnAmmoPickup?.Invoke();
    }

    public (int mag, int reserve) GetAmmo()
    {
        return (currentMagAmmo, currentReserveAmmo);
    }
}
