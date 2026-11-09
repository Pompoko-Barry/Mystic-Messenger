using UnityEngine;
using UnityEngine.InputSystem;
using SimpleTwineDialogue;

/// <summary>
/// A testing panel for jumping straight to any game state or passage,
/// and for speeding up the dialogue. Press F1 (or the key you choose) to show or hide it.
///
/// Only runs in the Unity editor and in Development Builds, so players never see it.
/// Put this on the same GameObject as GameStateManager.
/// </summary>
public class GameStateDebugger : MonoBehaviour
{
    [Header("Scripts")]
    public GameStateManager stateManager;
    public ChatView chatView;

    [Header("Where each chat starts when you jump to it")]
    [Tooltip("The first chat passage after the name screen.")]
    public string introChatPassage = "Logging in...";

    [Tooltip("Leave empty to use Second Chat First Passage from GameStateManager.")]
    public string secondChatPassage = "";

    [Header("Panel")]
    [Tooltip("Name used when you skip past the name screen.")]
    public string debugPlayerName = "Tester";

    public Key toggleKey = Key.F1;
    public bool showOnStart = true;

    [Tooltip("Makes the panel bigger or smaller on screen.")]
    public float panelScale = 1.5f;

    bool visible;
    string passageToPlay = "";

    void Awake()
    {
        // Debug.isDebugBuild is true in the editor and in Development Builds only.
        if (!Debug.isDebugBuild)
        {
            enabled = false;
            return;
        }
        visible = showOnStart;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            visible = !visible;
    }

    void OnDisable()
    {
        Time.timeScale = 1f; // never leave the game sped up
    }

    // ---------- Jumps ----------

    /// <summary>Skips the name screen and gives the player a name, so chats display correctly.</summary>
    void SkipNameScreen()
    {
        if (chatView.nameScreen != null)
            chatView.nameScreen.SetActive(false);

        if (chatView.playerName == "You" || string.IsNullOrWhiteSpace(chatView.playerName))
            chatView.SetPlayerName(debugPlayerName);
    }

    void JumpTo(GameState state)
    {
        SkipNameScreen();

        switch (state)
        {
            case GameState.IntroChat:
                stateManager.SetState(GameState.IntroChat);
                chatView.PlayFrom(introChatPassage);
                break;

            case GameState.SecondChat:
                stateManager.SetState(GameState.SecondChat);
                chatView.PlayFrom(string.IsNullOrWhiteSpace(secondChatPassage)
                    ? stateManager.secondChatFirstPassage
                    : secondChatPassage);
                break;

            case GameState.HomeAfterIntro:
            case GameState.HomeAfterSecondChat:
                // Stop the chat first, so a passage still playing can't change the state afterwards.
                chatView.StopAllCoroutines();
                stateManager.SetState(state);
                break;
        }

        Debug.Log($"GameStateDebugger: jumped to {state}");
    }

    void PlayPassage(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        SkipNameScreen();
        stateManager.SetState(GameState.IntroChat); // makes sure the chat camera is showing
        chatView.PlayFrom(title.Trim());
        Debug.Log($"GameStateDebugger: playing passage '{title.Trim()}'");
    }

    // ---------- Panel ----------

    void OnGUI()
    {
        if (!visible) return;

        GUI.matrix = Matrix4x4.Scale(new Vector3(panelScale, panelScale, 1f));
        GUILayout.BeginArea(new Rect(10, 10, 260, 420), GUI.skin.box);

        GUILayout.Label($"DEBUG ({toggleKey} to hide)");
        GUILayout.Label("State: " + stateManager.CurrentState);

        GUILayout.Space(6);
        GUILayout.Label("Jump to state");
        foreach (GameState state in System.Enum.GetValues(typeof(GameState)))
        {
            if (GUILayout.Button(state.ToString()))
                JumpTo(state);
        }

        GUILayout.Space(6);
        GUILayout.Label("Play a passage by name");
        passageToPlay = GUILayout.TextField(passageToPlay);
        if (GUILayout.Button("Play passage"))
            PlayPassage(passageToPlay);

        GUILayout.Space(6);
        GUILayout.Label($"Dialogue speed: {Time.timeScale}x");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1x")) Time.timeScale = 1f;
        if (GUILayout.Button("4x")) Time.timeScale = 4f;
        if (GUILayout.Button("10x")) Time.timeScale = 10f;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}
