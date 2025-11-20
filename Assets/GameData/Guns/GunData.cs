using UnityEngine;

public enum ItemRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum FireMode
{
    SemiAuto,   // click = one shot (pistol, shotgun, etc.)
    FullAuto    // hold = continuous fire (rifle, SMG, etc.)
}

[CreateAssetMenu(menuName = "Game/Gun Data", fileName = "NewGunData")]
public class GunData : ScriptableObject
{
    [Header("Identity")]
    public string gunId = "pistol_01";
    public string displayName = "Starter Pistol";

    [Header("Stats")]
    public int damage = 5;
    public float fireRate = 10f;   // shots per second
    public float range = 100f;

    [Header("ADS Settings")]
    [Range(0f, 1f)]
    public float adsSpreadMultiplier = 0.4f;  // 0.4 = tighter spread while ADS


    [Header("Firing")]
    public FireMode fireMode = FireMode.SemiAuto;

    // For shotgun / multi-pellet weapons.
    // 1 = normal hitscan, >1 = shotgun style
    public int pelletsPerShot = 1;

    // Max spread angle in degrees for each pellet
    public float spreadAngle = 0f;

    [Header("Ammo")]
    public int magSize = 12;
    public int maxReserveAmmo = 60;
    public float reloadTime = 1.2f;

    [Header("Visuals (for later)")]
    public GameObject firstPersonModelPrefab;
    public GameObject worldModelPrefab;

    [Header("Audio")]
    public AudioClip shotClip;
    public float shotVolume = 1f;

    public AudioClip dryFireClip;
    public AudioClip reloadClip;
    public float reloadVolume = 1.5f;

    public AudioClip hitmarkerClip;

    [Header("Rarity")]
    public ItemRarity rarity = ItemRarity.Common;
    public Color rarityColor = Color.white;   // you can override per gun if you want
}
