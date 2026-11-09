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
    HomeAfterSecondChat,
    ThirdChat,
    BadEndingWesker,
    EndingWesker,
    EndingZen,
    EndingShinji
}


public enum ScreenView { Chat, Home, Ending };





/// <summary>
/// Switches between the group chat and the home screen by turning
/// cameras and canvases on and off. Put this on an empty GameObject.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [Header("Scripts")]
    public ChatView chatView;
    public EndingDisplay endingDisplay;

    [Header("Cameras (drag in the camera GameObjects)")]
    public GameObject chatCamera;
    public GameObject homeCamera;
    public GameObject endCamera;

    [Header("Canvases")]
    public GameObject chatCanvas;
    public GameObject homeCanvas;
    public GameObject endCanvas;

    [Header ("Ending Images")]
    //The assigned visuals for each ending type
    //Set them false in the beginning
    //They will turn true during their assigned states
    public GameObject badEndingWeskerImages;
    public GameObject endingWeskerImages;
    public GameObject endingZenImages;
    public GameObject endingShinjiImages;

    [Header("Story")]
    [Tooltip("Exact name of the first passage of the second chat, e.g. Chat2 Start")]
    public string secondChatFirstPassage = "Chat2 Start";
    public string thirdChatFirstPassage = "Chat3 Start";
    public string badEndingWeskerPassage = "You gave wesker your personal information.";
    public string endingWeskerPassage = "have not written yet";
    public string endingZenPassage = "have not written yet";
    public string endingShinjiPassage = "have not written yet";

    // ------ CURRENT STATE --------
    GameState currentState;
    public GameState CurrentState => currentState;

    void Start()
    {
        SetState(GameState.IntroChat);

        badEndingWeskerImages.SetActive(false);
        endingWeskerImages.SetActive(false);
        endingZenImages.SetActive(false);
        endingShinjiImages.SetActive(false);
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
            case GameState.ThirdChat:
                ShowView(ScreenView.Chat);
                break;

            case GameState.HomeAfterIntro:
            case GameState.HomeAfterSecondChat:
                ShowView(ScreenView.Home);
                break;

            case GameState.BadEndingWesker:
                ShowView(ScreenView.Ending);
                endingDisplay.PlayPassage(badEndingWeskerPassage);
                badEndingWeskerImages.SetActive(true);
                break;

            case GameState.EndingWesker:
                ShowView(ScreenView.Ending);
                endingDisplay.PlayPassage(endingWeskerPassage);
                endingWeskerImages.SetActive(true);
                break;

            case GameState.EndingZen:
                ShowView(ScreenView.Ending);
                endingDisplay.PlayPassage(endingZenPassage);
                endingZenImages.SetActive(true);
                break;
            
            case GameState.EndingShinji:
                ShowView(ScreenView.Ending);
                endingDisplay.PlayPassage(endingShinjiPassage);
                endingShinjiImages.SetActive(true);
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

    public void OpenThirdChat()
    {
        if (currentState != GameState.HomeAfterSecondChat) return;

        SetState(GameState.ThirdChat);
        chatView.PlayFrom(thirdChatFirstPassage);
    }

   void ShowView(ScreenView view)
   {
    chatCamera.SetActive(view == ScreenView.Chat);
    chatCanvas.SetActive(view == ScreenView.Chat);

    homeCamera.SetActive(view == ScreenView.Home);
    homeCanvas.SetActive(view == ScreenView.Home);

    endCamera.SetActive(view == ScreenView.Ending);
    endCanvas.SetActive(view == ScreenView.Ending);
   }
}