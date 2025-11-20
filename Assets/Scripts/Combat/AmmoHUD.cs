using UnityEngine;
using Unity.Netcode;
using TMPro;

public class AmmoHUD : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text ammoText;        // drag AmmoText (TMP) here
    public TMP_Text gunNameText;     // drag GunNameText (TMP) here
    public RectTransform rootTransform;  // drag AmmoHUDPanel here (or leave null to auto-use self)

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color lowColor = new Color(1f, 0.8f, 0.3f);  // orange-ish
    public Color criticalColor = Color.red;
    [Range(0f, 1f)] public float lowThreshold = 0.3f;
    [Range(0f, 1f)] public float criticalThreshold = 0.1f;

    [Header("Pop Animation (on ammo pickup)")]
    public float popScale = 1.2f;
    public float popDuration = 0.2f;

    private Gun _myGun;
    private float _popTimer = 0f;
    private Vector3 _originalScale;

    private void Start()
    {
        if (rootTransform == null)
            rootTransform = GetComponent<RectTransform>();

        _originalScale = rootTransform != null ? rootTransform.localScale : Vector3.one;

        if (ammoText == null)
            Debug.LogWarning("[AmmoHUD] ammoText is NOT assigned in Inspector.");

        if (gunNameText == null)
            Debug.LogWarning("[AmmoHUD] gunNameText is NOT assigned in Inspector.");

        FindLocalGun();
    }

    private void OnEnable()
    {
        FindLocalGun();
    }

    private void OnDisable()
    {
        UnsubscribeFromGun();
    }

    private void Update()
    {
        if (_myGun == null || !_myGun.IsOwner)
        {
            // No valid local gun yet
            FindLocalGun();

            if (ammoText != null)
                ammoText.text = "-- / --";

            if (gunNameText != null)
                gunNameText.text = "";

            UpdatePopAnimation();
            return;
        }

        var (mag, reserve) = _myGun.GetAmmo();

        if (ammoText != null)
        {
            ammoText.text = $"{mag} / {reserve}";
            UpdateAmmoColor(mag, _myGun.magazineSize);
        }

        if (gunNameText != null)
        {
            string name = _myGun.gunData != null ? _myGun.gunData.displayName : "Weapon";
            gunNameText.text = name;
        }

        UpdatePopAnimation();
    }

    private void UpdateAmmoColor(int mag, int magSize)
    {
        if (ammoText == null || magSize <= 0)
            return;

        float ratio = (float)mag / magSize;
        Color c = normalColor;

        if (mag <= 0)
        {
            c = criticalColor;
        }
        else if (ratio <= criticalThreshold)
        {
            c = criticalColor;
        }
        else if (ratio <= lowThreshold)
        {
            c = lowColor;
        }

        ammoText.color = c;
    }

    private void UpdatePopAnimation()
    {
        if (rootTransform == null) return;

        if (_popTimer > 0f)
        {
            _popTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_popTimer / popDuration); // 0→1
            float scale = Mathf.Lerp(popScale, 1f, t);
            rootTransform.localScale = _originalScale * scale;

            if (_popTimer <= 0f)
            {
                rootTransform.localScale = _originalScale;
            }
        }
    }

    private void HandleAmmoPickup()
    {
        _popTimer = popDuration;
    }

    private void FindLocalGun()
    {
        UnsubscribeFromGun();

        var allGuns = FindObjectsOfType<Gun>();
        foreach (var gun in allGuns)
        {
            if (gun.IsOwner)
            {
                _myGun = gun;
                _myGun.OnAmmoPickup += HandleAmmoPickup;
                Debug.Log($"[AmmoHUD] Bound to gun on owner {gun.OwnerClientId}."); // DEBUG
                break;
            }
        }

        if (_myGun == null)
        {
            // This will spam if we log every frame; so only log rarely if needed.
            // Debug.Log("[AmmoHUD] No local Gun found yet.");
        }
    }

    private void UnsubscribeFromGun()
    {
        if (_myGun != null)
        {
            _myGun.OnAmmoPickup -= HandleAmmoPickup;
            _myGun = null;
        }
    }
}
