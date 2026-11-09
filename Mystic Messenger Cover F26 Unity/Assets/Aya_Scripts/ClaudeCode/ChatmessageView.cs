using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleTwineDialogue
{
    /// <summary>
    /// Put this on the root of each chat prefab (left message, right message, system notice).
    /// Drag the prefab's own Image and TextMeshPro objects into these slots in the Inspector.
    /// </summary>
    public class ChatMessageView : MonoBehaviour
    {
        [Tooltip("The avatar picture. Leave empty on the system notice prefab.")]
        public Image avatarImage;

        [Tooltip("The speaker's name. Leave empty on the system notice prefab.")]
        public TMP_Text nameLabel;

        [Tooltip("The message text (inside the bubble), or the notice text.")]
        public TMP_Text bodyLabel;

        [Tooltip("Layout Element on the message text. Lets short messages get small bubbles " +
                 "and long ones wrap at Max Bubble Text Width. Leave empty on the system notice prefab.")]
        public LayoutElement bodyLayout;

        [Tooltip("Widest the message text may get before it wraps to a new line.")]
        public float maxBubbleTextWidth = 650f;

        /// <summary>Fills in a normal message bubble.</summary>
        public void ShowMessage(ChatCharacter speaker, string text)
        {
            if (nameLabel != null)
            {
                nameLabel.text = speaker.Name;
                nameLabel.color = speaker.Color;
            }

            bodyLabel.text = text;

            if (bodyLayout != null)
            {
                // Bubble as wide as the text on one line, but never wider than the max.
                float oneLineWidth = bodyLabel.GetPreferredValues(text).x;
                bodyLayout.preferredWidth = Mathf.Min(oneLineWidth, maxBubbleTextWidth);
            }

            if (avatarImage != null)
            {
                avatarImage.sprite = speaker.Avatar;
                // Hide the avatar slot entirely when the character has no picture.
                avatarImage.gameObject.SetActive(speaker.Avatar != null);
            }
        }

        /// <summary>Fills in a system notice bar ("Zen has entered the chatroom.").</summary>
        public void ShowNotice(string text)
        {
            bodyLabel.text = text;
        }
    }
}