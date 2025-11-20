using Unity.Netcode;
using UnityEngine;

public class AmmoPickup : NetworkBehaviour
{
    [Header("Ammo Settings")]
    public int ammoAmount = 30;

    [Header("Float / Spin (optional)")]
    public float bobAmplitude = 0.2f;
    public float bobFrequency = 2f;
    public float rotateSpeed = 45f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        if (!IsSpawned) return;

        float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = _startPos + Vector3.up * bob;

        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Find the gun on whoever touched this
        var gun = other.GetComponentInParent<Gun>();
        if (gun == null) return;

        // 🔹 Local HUD update for non-host clients
        if (!IsServer && gun.IsOwner)
        {
            gun.AddReserveAmmo(ammoAmount);
        }

        // 🔹 Server: authoritative ammo + despawn
        if (IsServer)
        {
            gun.AddReserveAmmo(ammoAmount);

            var no = GetComponent<NetworkObject>();
            if (no != null && no.IsSpawned)
            {
                no.Despawn();
            }
        }
    }
}
