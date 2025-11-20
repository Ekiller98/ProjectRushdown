using Unity.Netcode;
using UnityEngine;

public class WeaponPickup : NetworkBehaviour
{
    [Header("Weapon To Give")]
    public GunData weaponData;

    [Header("Float / Spin")]
    public float bobAmplitude = 0.25f;
    public float bobFrequency = 2f;
    public float rotateSpeed = 45f;

    [Header("Rarity Visuals")]
    [Tooltip("Renderers whose material color/emission will be tinted.")]
    public MeshRenderer[] coloredRenderers;
    public Light glowLight;
    public float emissionIntensity = 3f;   // how bright the glow is

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
        ApplyRarityVisuals();
    }

    private void Update()
    {
        if (!IsSpawned) return;

        // Float
        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = _startPos + Vector3.up * bob;

        // Spin
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void ApplyRarityVisuals()
    {
        if (weaponData == null) return;

        // choose color: prefer per-gun override, otherwise based on rarity
        Color baseColor = weaponData.rarityColor != default
            ? weaponData.rarityColor
            : GetDefaultRarityColor(weaponData.rarity);

        // Tint mesh(es)
        if (coloredRenderers != null)
        {
            foreach (var mr in coloredRenderers)
            {
                if (mr == null) continue;

                // clone material so we don't edit the shared one
                var mat = mr.material;

                // Base color tint (optional)
                mat.color = baseColor;

                // Emission (make sure your shader supports it)
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", baseColor * emissionIntensity);
                }
            }
        }

        // Tint glow light
        if (glowLight != null)
        {
            glowLight.color = baseColor;
        }
    }

    private Color GetDefaultRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Color.white;
            case ItemRarity.Rare: return new Color(0.2f, 0.4f, 1f);     // blue
            case ItemRarity.Epic: return new Color(0.7f, 0.2f, 0.9f);   // purple
            case ItemRarity.Legendary: return new Color(1f, 0.6f, 0.1f);     // gold
            default: return Color.white;
        }
    }
}
