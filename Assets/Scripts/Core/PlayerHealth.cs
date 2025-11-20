using Unity.Netcode;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>();

    [Header("Death / Respawn Settings")]
    public float respawnDelay = 3f;

    private FpsPlayerController fpsController;
    private Gun gun;
    private CharacterController characterController;
    private Renderer[] renderers;

    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        fpsController = GetComponent<FpsPlayerController>();
        gun = GetComponent<Gun>();
        characterController = GetComponent<CharacterController>();
        renderers = GetComponentsInChildren<Renderer>();

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    void OnDestroy()
    {
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int previous, int current)
    {
        if (IsOwner)
        {
            Debug.Log($"[PlayerHealth] My health: {current}");
        }

        if (current <= 0 && !isDead)
        {
            HandleDeath();
        }
    }

    public void ApplyDamage(int damage)
    {
        if (!IsServer) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);

        if (currentHealth.Value == 0)
        {
            Debug.Log($"[PlayerHealth] {OwnerClientId} died");

            // Inform round manager (server-side)
            var team = GetComponent<PlayerTeam>();
            if (team != null && RoundManager.Instance != null)
            {
                RoundManager.Instance.OnPlayerDiedServer(team.teamId.Value);
            }
        }
    }

    private void HandleDeath()
    {
        isDead = true;

        if (fpsController != null)
            fpsController.enabled = false;

        if (gun != null)
            gun.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        if (renderers != null)
        {
            foreach (var r in renderers)
                r.enabled = false;
        }

        if (IsServer)
        {
            // Death-based respawn (outside of round system).
            StartCoroutine(ServerRespawnCoroutine());
        }
    }

    private System.Collections.IEnumerator ServerRespawnCoroutine()
    {
        // Wait some time "dead"
        yield return new WaitForSeconds(respawnDelay);

        currentHealth.Value = maxHealth;

        // Owner will move to team spawn in RespawnClientRpc
        RespawnClientRpc();
    }

    [ClientRpc]
    private void RespawnClientRpc()
    {
        isDead = false;

        // OWNER repositions at their team spawn
        if (IsOwner)
        {
            var team = GetComponent<PlayerTeam>();
            if (team != null)
            {
                Vector3 respawnPos = team.GetSpawnPosition();

                if (characterController != null)
                {
                    characterController.enabled = false;
                    transform.position = respawnPos;
                    characterController.enabled = true;
                }
                else
                {
                    transform.position = respawnPos;
                }
            }
        }

        if (fpsController != null)
            fpsController.enabled = true;

        if (gun != null)
            gun.enabled = true;

        if (characterController != null)
            characterController.enabled = true;

        if (renderers != null)
        {
            foreach (var r in renderers)
                r.enabled = true;
        }
    }

    public void ResetHealth()
    {
        if (!IsServer) return;
        currentHealth.Value = maxHealth;
    }

    /// <summary>
    /// Used by RoundManager to force a fresh respawn at team spawn at round start.
    /// </summary>
    public void ForceRespawnAtTeamSpawn()
    {
        if (!IsServer) return;

        isDead = false;
        currentHealth.Value = maxHealth;

        // Re-use the same logic as normal respawn
        RespawnClientRpc();
    }
}
