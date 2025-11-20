using Unity.Netcode;
using UnityEngine;

public class PlayerTeam : NetworkBehaviour
{
    // 0 = Red, 1 = Blue
    public NetworkVariable<int> teamId = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Team Colors")]
    public Color redColor = Color.red;
    public Color blueColor = Color.blue;

    private Renderer[] renderers;

    public override void OnNetworkSpawn()
    {
        // Cache renderers (model, gun, etc.)
        renderers = GetComponentsInChildren<Renderer>();

        // Server decides the team
        if (IsServer)
        {
            int assignedTeam = (int)(OwnerClientId % 2); // 0 or 1
            teamId.Value = assignedTeam;
        }

        // Apply initial color
        ApplyTeamColor(teamId.Value);

        // Listen for team changes
        teamId.OnValueChanged += OnTeamChanged;

        // IMPORTANT: owner moves themselves to team spawn
        // IMPORTANT: owner moves themselves to lobby or team spawn
        if (IsOwner)
        {
            bool inMatch = RoundManager.Instance != null && RoundManager.Instance.roundInProgress.Value;
            if (inMatch)
            {
                MoveToTeamSpawn();
            }
            else
            {
                MoveToLobbySpawn();
            }
        }

    }

    private void OnDestroy()
    {
        teamId.OnValueChanged -= OnTeamChanged;
    }

    private void OnTeamChanged(int previous, int current)
    {
        ApplyTeamColor(current);

        // If my team changed (or got set) and I own this player, move to spawn
        if (IsOwner)
        {
            MoveToTeamSpawn();
        }
    }

    private void ApplyTeamColor(int team)
    {
        if (renderers == null) return;

        Color teamColor = team == 0 ? redColor : blueColor;

        foreach (var r in renderers)
        {
            if (r != null && r.material != null && r.material.HasProperty("_Color"))
            {
                r.material.color = teamColor;
            }
        }
    }

    public Vector3 GetSpawnPosition()
    {
        string spawnName = teamId.Value == 0 ? "RedSpawn" : "BlueSpawn";
        GameObject spawnObj = GameObject.Find(spawnName);

        if (spawnObj != null)
        {
            return spawnObj.transform.position;
        }

        Debug.LogWarning($"[PlayerTeam] Could not find spawn object '{spawnName}'. Using (0,0,0).");
        return Vector3.zero;
    }
    public Vector3 GetLobbySpawnPosition()
    {
        GameObject lobbySpawn = GameObject.Find("LobbySpawn");
        if (lobbySpawn != null)
        {
            return lobbySpawn.transform.position;
        }

        Debug.LogWarning("[PlayerTeam] Could not find 'LobbySpawn'. Using (0,0,0).");
        return Vector3.zero;
    }
    public void MoveToLobbySpawn()
    {
        Vector3 spawnPos = GetLobbySpawnPosition();

        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = spawnPos;
            cc.enabled = true;
        }
        else
        {
            transform.position = spawnPos;
        }
    }

    public void MoveToTeamSpawn()
    {
        Vector3 spawnPos = GetSpawnPosition();

        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = spawnPos;
            cc.enabled = true;
        }
        else
        {
            transform.position = spawnPos;
        }
    }

    public bool IsRedTeam() => teamId.Value == 0;
    public bool IsBlueTeam() => teamId.Value == 1;
}
