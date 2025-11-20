using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    [Header("Menu References")]
    public GameObject homeScreenPanel;   // Panel with background + buttons
    public Camera homeScreenCamera;      // Optional: camera for menu view
    public AudioSource homeScreenMusic;      // Music that plays on menu
    public GameObject hudCanvas;
    public GameObject mainMenuCanvasRoot; // assign MainMenuCanvas in Inspector
    public GameObject lobbyCanvasRoot; // assign LobbyCanvas in Inspector

    public void StartHost()
    {
        Debug.Log("StartHost button pressed");

        // Turn off menu visuals & audio

        if (homeScreenCamera != null)
            homeScreenCamera.gameObject.SetActive(false);

        if (homeScreenMusic != null)
            homeScreenMusic.Stop();

        if (hudCanvas != null)
            hudCanvas.SetActive(false);      // not in-game yet

        if (mainMenuCanvasRoot != null)
            mainMenuCanvasRoot.SetActive(false);

        if (lobbyCanvasRoot != null)
            lobbyCanvasRoot.SetActive(true); // show lobby UI

        NetworkManager.Singleton.StartHost();
        // We keep this Canvas alive for future HUD; if you want to hide everything:
        // gameObject.SetActive(false);
    }

    public void StartClient()
    {
        Debug.Log("StartClient button pressed");

        if (homeScreenCamera != null)
            homeScreenCamera.gameObject.SetActive(false);

        if (homeScreenMusic != null)
            homeScreenMusic.Stop();

        if (hudCanvas != null)
            hudCanvas.SetActive(false);      // not in-game yet

        if (mainMenuCanvasRoot != null)
            mainMenuCanvasRoot.SetActive(false);

        if (lobbyCanvasRoot != null)
            lobbyCanvasRoot.SetActive(true); // show lobby UI

        NetworkManager.Singleton.StartClient();
        // gameObject.SetActive(false);
    }
}
