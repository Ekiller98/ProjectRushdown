using TMPro;
using UnityEngine;

public class RoundHud : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text scoreText;
    public TMP_Text roundText;

    void Update()
    {
        // Make sure RoundManager exists
        if (RoundManager.Instance == null) return;

        var rm = RoundManager.Instance;

        scoreText.text = $"Red {rm.RedScore} - {rm.BlueScore} Blue";
        roundText.text = $"Round {rm.RoundNum}";
    }
}
