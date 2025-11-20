using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Loadout")]
    public GunData starterGunData;   // assign your Starter Pistol in inspector

    [Header("Round Settings")]
    public int roundsToWin = 10;
    public float nextRoundDelay = 3f;

    public NetworkVariable<int> redScore = new NetworkVariable<int>();
    public NetworkVariable<int> blueScore = new NetworkVariable<int>();
    public NetworkVariable<int> roundNumber = new NetworkVariable<int>(1);
    public NetworkVariable<bool> roundInProgress = new NetworkVariable<bool>(true);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            redScore.Value = 0;
            blueScore.Value = 0;
            roundNumber.Value = 1;
            roundInProgress.Value = false;  // wait until lobby starts the match
        }
    }

    /// <summary>
    /// Called by the server when a player dies, with that player's teamId.
    /// </summary>
    public void OnPlayerDiedServer(int deadTeamId)
    {
        if (!IsServer) return;
        if (!roundInProgress.Value) return;

        // Check if any player on that team is still alive
        bool hasAliveOnTeam = false;

        foreach (var ph in FindObjectsOfType<PlayerHealth>())
        {
            if (!ph.IsSpawned) continue;
            if (ph.currentHealth.Value <= 0) continue;

            var team = ph.GetComponent<PlayerTeam>();
            if (team == null) continue;

            if (team.teamId.Value == deadTeamId)
            {
                hasAliveOnTeam = true;
                break;
            }
        }

        if (!hasAliveOnTeam)
        {
            int winningTeam = deadTeamId == 0 ? 1 : 0;
            EndRound(winningTeam);
        }
    }

    public void StartMatchFromLobby()
    {
        if (!IsServer) return;
        if (roundInProgress.Value) return; // already in a round

        // Reset scores & round number if you want a fresh match
        redScore.Value = 0;
        blueScore.Value = 0;
        roundNumber.Value = 1;

        StartCoroutine(StartFirstRoundCoroutine());
    }

    private IEnumerator StartFirstRoundCoroutine()
    {
        Debug.Log("[RoundManager] Starting first round from lobby...");

        // Move everyone to their team spawns with full health
        foreach (var ph in FindObjectsOfType<PlayerHealth>())
        {
            if (!ph.IsSpawned) continue;
            ph.ForceRespawnAtTeamSpawn();
        }

        // Reset weapons & ammo for everyone
        ResetAllPlayersForNewRound();
        ResetAllPlayersForNewRoundClientRpc();

        // Small delay if you want, or just yield null
        yield return null;

        roundInProgress.Value = true;
    }

    private void EndRound(int winningTeam)
    {
        if (!IsServer) return;

        roundInProgress.Value = false;

        if (winningTeam == 0)
            redScore.Value++;
        else
            blueScore.Value++;

        Debug.Log($"[RoundManager] Team {winningTeam} WON the round. Score: Red {redScore.Value} - Blue {blueScore.Value}");

        // Check match win
        if (redScore.Value >= roundsToWin || blueScore.Value >= roundsToWin)
        {
            Debug.Log($"[RoundManager] Team {winningTeam} WINS THE MATCH! 🎉");
            // Later: show end screen / stop game, etc.
        }

        StartCoroutine(NextRoundCoroutine());
    }

    private IEnumerator NextRoundCoroutine()
    {
        yield return new WaitForSeconds(nextRoundDelay);

        // Start next round if match not over
        if (redScore.Value < roundsToWin && blueScore.Value < roundsToWin)
        {
            roundNumber.Value++;
            Debug.Log($"[RoundManager] Starting round {roundNumber.Value}");

            // Reset all players back to their team spawns with full health
            foreach (var ph in FindObjectsOfType<PlayerHealth>())
            {
                if (!ph.IsSpawned) continue;
                ph.ForceRespawnAtTeamSpawn();
            }

            // 🔹 Reset weapons and ammo to starter pistol for everyone
            ResetAllPlayersForNewRound();
            ResetAllPlayersForNewRoundClientRpc();

            roundInProgress.Value = true;
        }
    }

    // ------------------------------------------------------
    // NEW: reset weapons for new round
    // ------------------------------------------------------
    private void ResetAllPlayersForNewRound()
    {
        if (starterGunData == null)
            return;

        var allHealth = FindObjectsOfType<PlayerHealth>();
        foreach (var ph in allHealth)
        {
            if (!ph.IsSpawned) continue;

            // Find the Gun on this player (root or children)
            var gun = ph.GetComponentInChildren<Gun>();
            if (gun == null) continue;

            gun.ApplyGunData(starterGunData);
            gun.GiveFullAmmo();
        }
    }

    [ClientRpc]
    private void ResetAllPlayersForNewRoundClientRpc()
    {
        // Run the same logic on each client so the local Gun
        // stats + ammo + HUD are correct everywhere.
        ResetAllPlayersForNewRound();
    }

    // --- Convenience properties for UI (read-only) ---
    public int RedScore => redScore.Value;
    public int BlueScore => blueScore.Value;
    public int RoundNum => roundNumber.Value;

}
