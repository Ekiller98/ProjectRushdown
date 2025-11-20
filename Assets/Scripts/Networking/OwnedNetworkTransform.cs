using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// Simple wrapper so the *owner* of the object can move it,
// not just the server/host.
public class OwnedNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // owner authority instead of server authority
    }
}
