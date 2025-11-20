using Unity.Netcode;
using UnityEngine;

public class PlayerVault : NetworkBehaviour
{
    [Header("Vault Settings")]
    public float checkDistance = 1.5f;      // how far ahead to detect obstacle
    public float minVaultHeight = 0.4f;     // lowest ledge height
    public float maxVaultHeight = 1.5f;     // highest ledge height
    public float vaultDuration = 0.2f;      // how long the lerp takes
    public LayerMask vaultLayers;           // what is vaultable (set in inspector)

    private CharacterController _controller;
    private bool _isVaulting = false;
    private Vector3 _vaultStart;
    private Vector3 _vaultEnd;
    private float _vaultTimer = 0f;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (_isVaulting)
        {
            UpdateVault();
            return;
        }

        // Try to vault when you press jump (Space) – you can change this
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryVault();
        }
    }

    private void TryVault()
    {
        // Use the camera direction for detecting vaults
        var cam = GetComponentInChildren<Camera>();
        if (cam == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        // 1) Check for a wall/obstacle in front
        if (!Physics.Raycast(origin, dir, out RaycastHit wallHit, checkDistance, vaultLayers, QueryTriggerInteraction.Ignore))
            return;

        // 2) From above the hit point, raycast down to find the top
        Vector3 topCheckStart = wallHit.point + Vector3.up * maxVaultHeight;

        if (!Physics.Raycast(topCheckStart, Vector3.down, out RaycastHit topHit, maxVaultHeight + 0.5f, vaultLayers))
            return;

        float heightDelta = topHit.point.y - transform.position.y;

        // Only vault if within reasonable height
        if (heightDelta < minVaultHeight || heightDelta > maxVaultHeight)
            return;

        // 3) Choose a target position on top, slightly forward
        Vector3 forward = new Vector3(dir.x, 0f, dir.z).normalized;
        Vector3 endPos = topHit.point + forward * 0.4f;
        endPos.y += 0.05f;  // small offset so you don't clip

        _vaultStart = transform.position;
        _vaultEnd = endPos;
        _vaultTimer = 0f;
        _isVaulting = true;
    }

    private void UpdateVault()
    {
        _vaultTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_vaultTimer / vaultDuration);

        Vector3 targetPos = Vector3.Lerp(_vaultStart, _vaultEnd, t);
        Vector3 delta = targetPos - transform.position;

        // Move via CharacterController so collisions still apply
        _controller.Move(delta);

        if (t >= 1f)
        {
            _isVaulting = false;
        }
    }
}
