using Unity.Netcode;
using UnityEngine;

public class PlayerLobbyState : NetworkBehaviour
{
    // Ready state (server-authoritative)
    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private PlayerTeam _playerTeam;

    public bool IsReady => isReady.Value;

    private void Awake()
    {
        _playerTeam = GetComponent<PlayerTeam>();
    }

    public int GetTeamId()
    {
        return _playerTeam != null ? _playerTeam.teamId.Value : 0;
    }

    // --- CALLED BY LOCAL UI ---

    public void ToggleReadyFromLocal()
    {
        if (!IsOwner) return;
        SetReadyServerRpc(!isReady.Value);
    }

    public void ChangeTeamFromLocal(int newTeamId)
    {
        if (!IsOwner) return;
        ChangeTeamServerRpc(newTeamId);
    }

    // --- SERVER RPCs ---

    [ServerRpc]
    private void SetReadyServerRpc(bool ready)
    {
        isReady.Value = ready;
    }

    [ServerRpc]
    private void ChangeTeamServerRpc(int newTeamId)
    {
        if (_playerTeam == null) return;

        newTeamId = Mathf.Clamp(newTeamId, 0, 1); // 0 = Red, 1 = Blue
        _playerTeam.teamId.Value = newTeamId;
    }
}
