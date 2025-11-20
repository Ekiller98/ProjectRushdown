using Unity.Netcode;
using UnityEngine;

public class PlayerInteract : NetworkBehaviour
{
    [Header("Interaction")]
    public float interactRange = 2.5f;
    public float holdTimeToPickup = 0.5f;
    public LayerMask pickupLayer;   // set in Inspector to a "Pickup" layer
    public KeyCode interactKey = KeyCode.F;

    private float _holdTimer = 0f;
    private WeaponPickup _currentTarget;
    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        FindTargetPickup();

        if (_currentTarget != null)
        {
            // later: show "Hold F to pick up X" here

            if (Input.GetKey(interactKey))
            {
                _holdTimer += Time.deltaTime;

                if (_holdTimer >= holdTimeToPickup)
                {
                    var no = _currentTarget.GetComponent<NetworkObject>();
                    if (no != null)
                    {
                        // 🔹 NEW: apply gun data locally for the owner
                        var gun = GetComponent<Gun>();
                        if (gun != null && _currentTarget.weaponData != null)
                        {
                            gun.ApplyGunData(_currentTarget.weaponData);
                        }

                        // 🔹 Then tell the server to do the authoritative swap + despawn
                        RequestPickupServerRpc(no);
                    }

                    _holdTimer = 0f; // reset after pickup
                }
            }
            else
            {
                _holdTimer = 0f;
            }
        }
        else
        {
            _holdTimer = 0f;
        }
    }

    private void FindTargetPickup()
    {
        _currentTarget = null;

        // Find all pickups in a small sphere around the player
        var hits = Physics.OverlapSphere(transform.position, interactRange, pickupLayer);

        if (hits.Length == 0 || _cam == null) return;

        float bestDot = 0.5f; // only consider things roughly in front
        WeaponPickup best = null;

        foreach (var hit in hits)
        {
            var pickup = hit.GetComponentInParent<WeaponPickup>();
            if (pickup == null) continue;

            Vector3 dir = (pickup.transform.position - _cam.transform.position).normalized;
            float dot = Vector3.Dot(_cam.transform.forward, dir);

            if (dot > bestDot)
            {
                bestDot = dot;
                best = pickup;
            }
        }

        _currentTarget = best;
    }

    [ServerRpc]
    private void RequestPickupServerRpc(NetworkObjectReference pickupRef)
    {
        if (!pickupRef.TryGet(out NetworkObject pickupNO)) return;

        var pickup = pickupNO.GetComponent<WeaponPickup>();
        if (pickup == null || pickup.weaponData == null) return;

        // Validate range on server
        if (Vector3.Distance(transform.position, pickup.transform.position) > 3f)
            return;

        var gun = GetComponent<Gun>();
        if (gun == null) return;

        // Swap weapon on the server copy of this Gun
        gun.ApplyGunData(pickup.weaponData);

        // Despawn pickup for everyone
        pickupNO.Despawn();
    }
}
