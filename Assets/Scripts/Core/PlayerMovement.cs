using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Only let the local owner control this player
        if (!IsOwner) return;

        float x = Input.GetAxisRaw("Horizontal");  // A/D or Left/Right
        float z = Input.GetAxisRaw("Vertical");    // W/S or Up/Down

        moveInput = new Vector3(x, 0f, z).normalized;
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}
