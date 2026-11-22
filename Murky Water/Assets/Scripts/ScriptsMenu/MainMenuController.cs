using UnityEngine;
using UnityEngine.SceneManagement;

/// Main menu buttons: Play loads the gameplay scene, Quit closes the game.
/// Hook the public methods to the Buttons' OnClick events.
/// Settings and credits panels stay in SettingsMenuController.
/// Both scenes must be in File > Build Profiles > Scenes In Build.
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Exact name of the gameplay scene.")]
    [SerializeField] private string gameSceneName = "Gameplay";

    private bool loading;

    public void Play()
    {
        if (loading) return;

        if (string.IsNullOrWhiteSpace(gameSceneName) || !Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"MainMenuController: scene '{gameSceneName}' was not found. Check the name and add it to Scenes In Build.", this);
            return;
        }

        loading = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Application.Quit does nothing in the editor
#else
        Application.Quit();
#endif
    }
}
