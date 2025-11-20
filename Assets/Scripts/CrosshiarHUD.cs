using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CrosshairHUD : MonoBehaviour
{
    [Header("Crosshair Elements")]
    public RectTransform top;
    public RectTransform bottom;
    public RectTransform left;
    public RectTransform right;
    public RectTransform centerDot;   // optional

    [Header("Spread Settings")]
    public float baseSpread = 8f;          // idle distance from center
    public float moveSpread = 16f;         // extra when moving
    public float fireSpread = 10f;         // extra while shooting
    public float adsSpreadMultiplier = 0.4f; // shrink when ADS
    public float spreadLerpSpeed = 15f;    // smoothing

    [Header("Movement Sampling")]
    public float maxMoveSpeedForSpread = 8f; // roughly your sprint speed

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color adsColor = new Color(0.7f, 0.9f, 1f);

    private float _currentSpread;
    private FpsPlayerController _player;
    private CharacterController _controller;

    private void Start()
    {
        _currentSpread = baseSpread;
        FindLocalPlayer();
    }

    private void Update()
    {
        if (_player == null || !_player.IsOwner)
        {
            FindLocalPlayer();
            return;
        }

        float targetSpread = baseSpread;

        // --- movement-based spread ---
        float moveFactor = 0f;
        if (_controller != null)
        {
            Vector3 vel = _controller.velocity;
            vel.y = 0f;
            float speed = vel.magnitude;
            moveFactor = Mathf.Clamp01(speed / maxMoveSpeedForSpread);
        }

        targetSpread += moveSpread * moveFactor;

        // --- fire-based spread (approx) ---
        bool isFiring = Input.GetMouseButton(0);
        if (isFiring)
        {
            targetSpread += fireSpread;
        }

        // --- ADS shrink ---
        bool isAiming = Input.GetMouseButton(1);
        if (isAiming)
        {
            targetSpread *= adsSpreadMultiplier;
        }

        // smooth the spread
        _currentSpread = Mathf.Lerp(_currentSpread, targetSpread, Time.deltaTime * spreadLerpSpeed);

        // apply positions
        UpdateCrosshairPositions(_currentSpread);

        // change color slightly when ADS
        Color c = isAiming ? adsColor : normalColor;
        ApplyColor(c);
    }

    private void UpdateCrosshairPositions(float spread)
    {
        if (top != null)
            top.anchoredPosition = new Vector2(0f, spread);
        if (bottom != null)
            bottom.anchoredPosition = new Vector2(0f, -spread);
        if (left != null)
            left.anchoredPosition = new Vector2(-spread, 0f);
        if (right != null)
            right.anchoredPosition = new Vector2(spread, 0f);
    }

    private void ApplyColor(Color c)
    {
        if (top != null) SetImageColor(top, c);
        if (bottom != null) SetImageColor(bottom, c);
        if (left != null) SetImageColor(left, c);
        if (right != null) SetImageColor(right, c);
        if (centerDot != null) SetImageColor(centerDot, c);
    }

    private void SetImageColor(RectTransform rt, Color c)
    {
        var img = rt.GetComponent<Image>();
        if (img != null)
        {
            img.color = c;
        }
    }

    private void FindLocalPlayer()
    {
        _player = null;
        _controller = null;

        var players = FindObjectsOfType<FpsPlayerController>();
        foreach (var p in players)
        {
            if (p.IsOwner)
            {
                _player = p;
                _controller = p.GetComponent<CharacterController>();
                break;
            }
        }
    }
}
