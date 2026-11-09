using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleTwineDialogue
{
    public class EndingDisplay : MonoBehaviour
    {
        [Header("Story")]
        public ChatView chatView;

        [Header("Text box")]
        public TMP_Text nameText;
        public TMP_Text lineText;

        [Header("Choices")]
        public Button choiceButtonPrefab;
        public Transform choiceButtonContainer;

        TweeParser.Passage currentPassage;   // the passage being shown
        List<ChatEvent> lines;               // its lines, in order
        int lineIndex;                       // which line is on screen

        public void PlayPassage(string title)
        {
            title = title.Trim();
            
            // Find the passage in the story that ChatView.cs already loaded

            if (!chatView.Passages.TryGetValue(title, out var passage))
            {
                Debug.LogError("EndingDisplay: no passage named '" + title + "'" );
                return;
            }

            currentPassage = passage; // Remember what passage, for ShowChoices
            ClearChoices();

            // Turn the twine passage into a list of lines
            lines = ChatScriptReader.GetEvents(passage, chatView.Characters, chatView.playerName);
            lineIndex = 0;

            // If there is nothing to say, go straight to the choices
            if (lines.Count == 0)
            {
                ShowChoices();
                return;
            }

            ShowCurrentLine();
        }

        void ShowCurrentLine()
        {
            var line = lines[lineIndex];   // get the current line

            // Look up who's speaking. TryGetValue is true if the speaker was found.
            if (chatView.Characters.TryGetValue(line.SpeakerKey, out var speaker))
                {
                nameText.text = speaker.Name;
                nameText.color = speaker.Color;
                }
            else
                {
                nameText.text = "";
                }

            lineText.text = line.Text;
        }

        public void Next()
        {
            Debug.Log("EndingDisplay: Next clicked");

            if (lines == null) return; // no ending started yet
            if (choiceButtonContainer.childCount > 0) return; // choices are showing

            lineIndex++;

            if (lineIndex < lines.Count)
            {
                ShowCurrentLine();
            }
            else
            {
                ShowChoices();
            }
        }

        void ShowChoices()
        {
            foreach (var choice in currentPassage.ParsedChoices)
            {
                var choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                choiceButton.GetComponentInChildren<TMP_Text>().text = choice.Text;

             string targetPassage = choice.Target;
             choiceButton.onClick.AddListener(() => PlayPassage(targetPassage));
            }
        }

        void ClearChoices()
        {
            foreach (Transform child in choiceButtonContainer)
                Destroy(child.gameObject);
        }

    }
}