using UnityEngine;
using SimpleTwineDialogue;   // lets this script use ChatView

/// <summary>
/// The four stages of the game, in order.
/// The names must match the words used in Twine: (set: $gameState to "HomeAfterIntro")
/// </summary>
public enum GameState
{
    IntroChat,
    HomeAfterIntro,
    SecondChat,
    HomeAfterSecondChat
}

/// <summary>
/// Switches between the group chat and the home screen by turning
/// cameras and canvases on and off. Put this on an empty GameObject.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("Scripts")]
    public ChatView chatView;

    [Header("Cameras (drag in the camera GameObjects)")]
    public GameObject chatCamera;
    public GameObject homeCamera;

    [Header("Canvases")]
    public GameObject chatCanvas;
    public GameObject homeCanvas;

    [Header("Story")]
    [Tooltip("Exact name of the first passage of the second chat, e.g. Chat2 Start")]
    public string secondChatFirstPassage = "Chat2 Start";

    // ------ CURRENT STATE --------
    GameState currentState;
    public GameState CurrentState => currentState;

    void Start()
    {
        SetState(GameState.IntroChat);
    }

    /// <summary>Changes the state and shows the matching camera and canvas.</summary>
    public void SetState(GameState newState)
    {
        currentState = newState;
        Debug.Log("GameStateManager: state is now " + newState);

        switch (newState)
        {
            case GameState.IntroChat:
            case GameState.SecondChat:
                ShowChatView(true);
                break;

            case GameState.HomeAfterIntro:
            case GameState.HomeAfterSecondChat:
                ShowChatView(false);
                break;
        }
    }

    /// <summary>
    /// Called by ChatView when a passage contains (set: $gameState to "...").
    /// Turns the word from Twine into a GameState.
    /// </summary>
    public void SetStateFromTwine(string stateName)
    {
        if (System.Enum.TryParse(stateName, out GameState parsed))
        {
            SetState(parsed);
        }
        else
        {
            Debug.LogError($"GameStateManager: no state called '{stateName}'. Check the spelling in Twine.");
        }
    }

    /// <summary>Hook this to the home screen button's On Click.</summary>
    public void OpenSecondChat()
    {
        // Only works from the first home screen, so the button can't restart the second chat.
        if (currentState != GameState.HomeAfterIntro) return;

        SetState(GameState.SecondChat);
        chatView.PlayFrom(secondChatFirstPassage);
    }

    /// <summary>true = show the chat, false = show the home screen.</summary>
    void ShowChatView(bool showChat)
    {
        chatCamera.SetActive(showChat);
        chatCanvas.SetActive(showChat);

        homeCamera.SetActive(!showChat);
        homeCanvas.SetActive(!showChat);
    }
}