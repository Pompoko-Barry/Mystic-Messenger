using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleTwineDialogue
{
    /// <summary>
    /// Plays the chat: reads the Twee file, shows each passage's messages on their
    /// (after:) timing, then shows the passage's links as reply buttons.
    /// Put this on an empty GameObject in your scene and fill in the Inspector slots.
    /// </summary>
    public class ChatView : MonoBehaviour
    {
        [Header("Story")]
        [Tooltip("Your exported Twee file, renamed to end in .txt so Unity imports it.")]
        public TextAsset tweeFile;

        [Tooltip("Exact name of the passage to start in.")]
        public string firstPassage = "Start";

        [Tooltip("Passages with this Twine tag show Unity's name screen instead of their contents.")]
        public string nameInputTag = "nameinput";

        [Tooltip("Exact name of the passage to play after the player confirms their name.")]
        public string afterNamePassage = "Logging in...";

        [Tooltip("Used until StartChat(name) is called with the player's typed name.")]
        public string playerName = "You";

        [Tooltip("Start playing First Passage as soon as the scene loads.")]
        public bool playOnStart = true;

        [Header("Name screen (optional)")]
        [Tooltip("The TMP Input Field where the player types their name.")]
        public TMP_InputField nameInput;

        [Tooltip("The panel holding the name screen. Hidden once the chat starts.")]
        public GameObject nameScreen;

        [Header("Chat area")]
        public ScrollRect scrollRect;
        public Transform messageContainer;
        public ChatMessageView leftMessagePrefab;
        public ChatMessageView rightMessagePrefab;
        public ChatMessageView systemNoticePrefab;

        [Header("Reply buttons")]
        public Transform choiceContainer;
        public Button choiceButtonPrefab;

        [Header("Game state (optional)")]
        [Tooltip("Receives (set: $gameState to \"...\") from Twine passages.")]
        public GameStateManager stateManager;

        [Tooltip("Seconds to wait after a passage's last message before the state changes.")]
        public float stateDelay = 2f;

        Dictionary<string, TweeParser.Passage> passages;
        Dictionary<string, ChatCharacter> characters;

        public Dictionary<string, TweeParser.Passage> Passages => passages;
        public Dictionary<string, ChatCharacter> Characters => characters;

        void Awake()
        {
            // Read the whole story once, and the characters from the "startup" passage.
            passages = new TweeParser().ParseTweeFileFromText(tweeFile.text);
            characters = ChatScriptReader.LoadCharacters(passages);
        }

        void Start()
        {
            if (playOnStart)
            {
                // The name screen stays hidden until the story reaches a "nameinput" passage.
                if (nameScreen != null)
                    nameScreen.SetActive(false);

                StartCoroutine(PlayPassage(firstPassage));
            }
        }

        /// <summary>Sets the player's name. Same job as (set: $player's name to $name) in Twine.</summary>
        public void SetPlayerName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                playerName = name.Trim();

            if (characters.TryGetValue("player", out var player))
                player.Name = playerName;
        }

        /// <summary>
        /// Sets the name and plays First Passage. For starting the chat from your own code.
        /// </summary>
        public void StartChat(string name)
        {
            SetPlayerName(name);
            StartCoroutine(PlayPassage(firstPassage));
        }

        /// <summary>
        /// Hook this to the name screen's Confirm button (On Click).
        /// Reads the typed name, hides the name screen and plays After Name Passage.
        /// </summary>
        public void StartChatFromInput()
        {
            if (nameInput != null && string.IsNullOrWhiteSpace(nameInput.text))
                return; // nothing typed yet; stay on the name screen

            if (nameScreen != null)
                nameScreen.SetActive(false);

            SetPlayerName(nameInput != null ? nameInput.text : playerName);

            var next = string.IsNullOrWhiteSpace(afterNamePassage) ? firstPassage : afterNamePassage;
            StartCoroutine(PlayPassage(next));
        }

        /// <summary>
        /// Clears the chat and plays a passage. Used by GameStateManager to start the second chat.
        /// </summary>
        public void PlayFrom(string title)
        {
            StopAllCoroutines();
            ClearChoices();
            foreach (Transform child in messageContainer)
                Destroy(child.gameObject);

            StartCoroutine(PlayPassage(title));
        }

        void ShowNameScreen()
        {
            if (nameScreen == null)
            {
                Debug.LogError("ChatView: reached a name passage, but the Name Screen slot is empty.");
                return;
            }

            nameScreen.SetActive(true);
            if (nameInput != null)
            {
                nameInput.text = "";
                nameInput.ActivateInputField(); // put the cursor in the box
            }
        }

        IEnumerator PlayPassage(string title)
        {
            title = title.Trim();   // ignore accidental spaces at the start or end
            if (!passages.TryGetValue(title, out var passage))
            {
                Debug.LogError(
                    $"ChatView: no passage named '{title}' in '{tweeFile.name}'. " +
                    $"Check spelling, spaces and '...' vs '…'.\n" +
                    $"Passages found ({passages.Count}): " + string.Join(" | ", passages.Keys));
                yield break;
            }

            ClearChoices();

            // Twine's name-entry passage: Unity uses its own name screen instead.
            if (passage.Tags.Contains(nameInputTag))
            {
                ShowNameScreen();
                yield break;
            }

            var events = ChatScriptReader.GetEvents(passage, characters, playerName);

            // Delays count from the start of the passage, like Harlowe's (after:),
            // so we only wait for the difference since the last message.
            float elapsed = 0f;
            foreach (var e in events)
            {
                if (e.Delay > elapsed)
                {
                    yield return new WaitForSeconds(e.Delay - elapsed);
                    elapsed = e.Delay;
                }
                ShowEvent(e);
            }

            // (set: $gameState to "HomeAfterIntro") hands control to the GameStateManager.
            var newState = ChatScriptReader.GetStateChange(passage);
            if (newState != null && stateManager != null)
            {
                yield return new WaitForSeconds(stateDelay); // let the last message be read
                stateManager.SetStateFromTwine(newState);
                yield break;
            }

            // (live: 5s)[(goto: "Next")] moves on by itself, like in Twine.
            var autoGoto = ChatScriptReader.GetAutoGoto(passage);
            if (autoGoto != null)
            {
                if (autoGoto.Delay > elapsed)
                    yield return new WaitForSeconds(autoGoto.Delay - elapsed);

                StartCoroutine(PlayPassage(autoGoto.Target));
                yield break;
            }

            ShowChoices(passage);
        }

        void ShowEvent(ChatEvent e)
        {
            if (e.Type == ChatEventType.SystemNotice)
            {
                Instantiate(systemNoticePrefab, messageContainer).ShowNotice(e.Text);
            }
            else
            {
                if (!characters.TryGetValue(e.SpeakerKey, out var speaker))
                    return; // ChatScriptReader already logged a warning for unknown speakers.

                var prefab = speaker.IsRight ? rightMessagePrefab : leftMessagePrefab;
                Instantiate(prefab, messageContainer).ShowMessage(speaker, e.Text);
            }

            StartCoroutine(ScrollToBottom());
        }

        IEnumerator ScrollToBottom()
        {
            yield return null; // wait a frame so the layout includes the new message
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // 0 = bottom
        }

        void ShowChoices(TweeParser.Passage passage)
        {
            foreach (var choice in passage.ParsedChoices)
            {
                var button = Instantiate(choiceButtonPrefab, choiceContainer);
                button.GetComponentInChildren<TMP_Text>().text = choice.Text;

                string target = choice.Target; // copy for the click handler
                button.onClick.AddListener(() => StartCoroutine(PlayPassage(target)));
            }
        }

        void ClearChoices()
        {
            foreach (Transform child in choiceContainer)
                Destroy(child.gameObject);
        }
    }
}