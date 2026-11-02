using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SimpleTwineDialogue
{
    /// <summary>
    /// A chat character defined in the Twine "startup" passage, e.g.
    /// (set: $zen to (dm: "name", "Zen MysticMessenger", "color", "#cf0e6b", "avatar", "zen"))
    /// </summary>
    public class ChatCharacter
    {
        /// <summary>Variable name without the $, e.g. "zen"</summary>
        public string Key;

        /// <summary>Display name, e.g. "Zen MysticMessenger"</summary>
        public string Name;

        /// <summary>Name color</summary>
        public Color Color = Color.white;

        /// <summary>Sprite name in Assets/Resources/Avatars/, e.g. "zen" (null if none)</summary>
        public string AvatarKey;

        /// <summary>Loaded avatar sprite (null if none or not found)</summary>
        public Sprite Avatar;

        /// <summary>"left" (default, NPCs) or "right" (the player)</summary>
        public string Side = "left";

        /// <summary>True when this character's messages go on the right.</summary>
        public bool IsRight => Side == "right";
    }

    public enum ChatEventType
    {
        Message,       // ($say: $who, "text")
        SystemNotice   // $systemnotif[text]
    }

    /// <summary>One thing that appears in the chat window.</summary>
    public class ChatEvent
    {
        public ChatEventType Type;

        /// <summary>Character key for messages, e.g. "zen". Null for system notices.</summary>
        public string SpeakerKey;

        /// <summary>The text to show, with $name and (print: $x's name) filled in.</summary>
        public string Text;

        /// <summary>Seconds after the passage starts, taken from (after: Ns). 0 if none.</summary>
        public float Delay;
    }

    /// <summary>
    /// An automatic jump to another passage, from (live: 5s)[(goto: "Next")]
    /// or (after: 5s)[(goto: "Next")], or a plain (goto: "Next").
    /// </summary>
    public class AutoGoto
    {
        /// <summary>The passage to jump to.</summary>
        public string Target;

        /// <summary>Seconds after the passage starts. 0 for a plain (goto:).</summary>
        public float Delay;
    }

    /// <summary>
    /// Reads Harlowe chat lines out of passages parsed by TweeParser.
    ///
    /// Expected writing style in Twine (one message per line):
    ///   ($say: $zen, "Meow meow~")
    ///   (after: 2s)[($say: $shinji, "So you're a cat.")]
    ///   (after: 5s)[$systemnotif[(print: $zen's name) has entered the chatroom.]]
    ///   (after: 7s)[ [[Reply->PlayerReply]] ]
    ///
    /// Like Harlowe, every (after:) delay counts from the start of the passage.
    /// Message text must not contain double quotes (apostrophes are fine).
    /// </summary>
    public static class ChatScriptReader
    {
        /// <summary>Folder under Assets/Resources/ that holds the avatar sprites.</summary>
        public static string AvatarFolder = "Avatars";

        // (set: $zen to (dm: ... ))   -- the dm contents must not contain parentheses
        static readonly Regex CharacterRegex = new Regex(
            @"\(set:\s*\$(?<key>\w+)\s+to\s+\(dm:(?<body>[^()]*)\)\)",
            RegexOptions.Singleline);

        // "key", "value" pairs inside a (dm: ...)
        static readonly Regex PairRegex = new Regex(
            @"""(?<k>[^""]*)""\s*,\s*""(?<v>[^""]*)""");

        // ($say: $zen, "text")
        static readonly Regex SayRegex = new Regex(
            @"\(\$say:\s*\$(?<who>\w+)\s*,\s*""(?<text>[^""]*)""\s*\)");

        // $systemnotif[text]  -- text may contain (print: ...) but no square brackets
        static readonly Regex NoticeRegex = new Regex(
            @"\$systemnotif\[(?<text>[^\[\]]*)\]");

        // (after: 2s) or (after: 1500ms)
        static readonly Regex AfterRegex = new Regex(
            @"\(after:\s*(?<n>[\d.]+)\s*(?<unit>ms|s)\b");

        // (live: 5s)[(goto: "Next")]  or  (after: 5s)[(goto: "Next")]
        static readonly Regex TimedGotoRegex = new Regex(
            @"\((?:live|after):\s*(?<n>[\d.]+)\s*(?<unit>ms|s)\s*\)\s*\[\s*\(goto:\s*""(?<target>[^""]*)""\s*\)");

        // a plain (goto: "Next")
        static readonly Regex GotoRegex = new Regex(
            @"\(goto:\s*""(?<target>[^""]*)""\s*\)");

        // any Harlowe macro call, e.g. (align:"=><=") or (text-style:"fade-in-out") or (stop:)
        static readonly Regex AnyMacroRegex = new Regex(
            @"\([\w-]+:(?:[^()""]|""[^""]*"")*\)");

        // a line left with nothing but leftover hook symbols, e.g. "+[=" or "[ ]"
        static readonly Regex OnlySymbolsRegex = new Regex(@"^[\s\[\]=+*/']*$");

        // (print: $zen's name)
        static readonly Regex PrintCharNameRegex = new Regex(
            @"\(print:\s*\$(?<key>\w+)'s\s+name\s*\)");

        // (print: $name) or bare $name
        static readonly Regex PrintPlayerNameRegex = new Regex(@"\(print:\s*\$name\s*\)");
        static readonly Regex BarePlayerNameRegex = new Regex(@"\$name\b");

        /// <summary>
        /// Finds every passage tagged "startup" and reads the character datamaps in it.
        /// Only datamaps with a "name" entry are treated as characters.
        /// </summary>
        public static Dictionary<string, ChatCharacter> LoadCharacters(
            Dictionary<string, TweeParser.Passage> passages)
        {
            var characters = new Dictionary<string, ChatCharacter>();

            foreach (var passage in passages.Values)
            {
                if (passage.Tags == null || !passage.Tags.Contains("startup"))
                    continue;

                foreach (Match m in CharacterRegex.Matches(passage.Body))
                {
                    var values = new Dictionary<string, string>();
                    foreach (Match pair in PairRegex.Matches(m.Groups["body"].Value))
                        values[pair.Groups["k"].Value] = pair.Groups["v"].Value;

                    if (!values.ContainsKey("name"))
                        continue;

                    var character = new ChatCharacter
                    {
                        Key = m.Groups["key"].Value,
                        Name = values["name"]
                    };

                    if (values.TryGetValue("color", out var hex) &&
                        ColorUtility.TryParseHtmlString(hex, out var color))
                        character.Color = color;

                    if (values.TryGetValue("side", out var side) && side.Length > 0)
                        character.Side = side.ToLowerInvariant();

                    if (values.TryGetValue("avatar", out var avatarKey) && avatarKey.Length > 0)
                    {
                        character.AvatarKey = avatarKey;
                        character.Avatar = Resources.Load<Sprite>(AvatarFolder + "/" + avatarKey);
                        if (character.Avatar == null)
                            Debug.LogWarning($"ChatScriptReader: no sprite found at Resources/{AvatarFolder}/{avatarKey}");
                    }

                    characters[character.Key] = character;
                }
            }

            return characters;
        }

        /// <summary>
        /// Turns a passage body into chat events, ordered by when they should appear.
        /// </summary>
        public static List<ChatEvent> GetEvents(
            TweeParser.Passage passage,
            Dictionary<string, ChatCharacter> characters,
            string playerName)
        {
            var events = new List<ChatEvent>();

            foreach (var line in passage.Body.Split('\n'))
            {
                float delay = 0f;
                var after = AfterRegex.Match(line);
                if (after.Success)
                {
                    delay = float.Parse(after.Groups["n"].Value, CultureInfo.InvariantCulture);
                    if (after.Groups["unit"].Value == "ms")
                        delay /= 1000f;
                }

                foreach (Match say in SayRegex.Matches(line))
                {
                    var who = say.Groups["who"].Value;
                    if (!characters.ContainsKey(who))
                        Debug.LogWarning($"ChatScriptReader: unknown speaker ${who} in passage '{passage.Title}'");

                    events.Add(new ChatEvent
                    {
                        Type = ChatEventType.Message,
                        SpeakerKey = who,
                        Text = ResolveText(say.Groups["text"].Value, characters, playerName),
                        Delay = delay
                    });
                }

                foreach (Match notice in NoticeRegex.Matches(line))
                {
                    events.Add(new ChatEvent
                    {
                        Type = ChatEventType.SystemNotice,
                        Text = ResolveText(notice.Groups["text"].Value, characters, playerName),
                        Delay = delay
                    });
                }
            }

            // A passage with no chat lines (like a "Booting up..." splash) shows its
            // plain text as system notices instead, so it isn't blank in Unity.
            if (events.Count == 0)
            {
                foreach (var text in GetLooseText(passage))
                {
                    events.Add(new ChatEvent
                    {
                        Type = ChatEventType.SystemNotice,
                        Text = ResolveText(text, characters, playerName),
                        Delay = 0f
                    });
                }
            }

            // OrderBy is stable, so lines with the same delay keep their written order.
            return events.OrderBy(e => e.Delay).ToList();
        }

        /// <summary>
        /// Finds an automatic jump in the passage, such as (live: 5s)[(goto: "Next")].
        /// Returns null if the passage has none.
        /// </summary>
        public static AutoGoto GetAutoGoto(TweeParser.Passage passage)
        {
            var timed = TimedGotoRegex.Match(passage.Body);
            if (timed.Success)
            {
                float delay = float.Parse(timed.Groups["n"].Value, CultureInfo.InvariantCulture);
                if (timed.Groups["unit"].Value == "ms")
                    delay /= 1000f;
                return new AutoGoto { Target = timed.Groups["target"].Value.Trim(), Delay = delay };
            }

            var plain = GotoRegex.Match(passage.Body);
            if (plain.Success)
                return new AutoGoto { Target = plain.Groups["target"].Value.Trim(), Delay = 0f };

            return null;
        }

        /// <summary>
        /// The readable text left in a passage once Harlowe code is removed.
        /// (align:"=><=")+(box:"X=")[=(text-style:"fade-in-out")[*...Booting up...*]
        /// becomes "...Booting up...". Lines with (set:) or (goto:) are skipped.
        /// </summary>
        public static List<string> GetLooseText(TweeParser.Passage passage)
        {
            var lines = new List<string>();

            foreach (var raw in passage.Body.Split('\n'))
            {
                var line = raw;

                // Remove macro calls repeatedly, so nested ones disappear too.
                string before;
                do
                {
                    before = line;
                    line = AnyMacroRegex.Replace(line, "");
                } while (line != before);

                if (OnlySymbolsRegex.IsMatch(line))
                    continue;

                // Strip leftover hook brackets and simple formatting marks.
                line = line.Replace("[=", "").Replace("[", "").Replace("]", "")
                           .Replace("**", "").Replace("*", "").Replace("//", "").Replace("''", "");
                line = line.Trim(' ', '\t', '\r', '+');

                if (line.Length > 0)
                    lines.Add(line);
            }

            return lines;
        }

        /// <summary>Fills in (print: $zen's name), (print: $name) and $name.</summary>
        public static string ResolveText(
            string text,
            Dictionary<string, ChatCharacter> characters,
            string playerName)
        {
            text = PrintCharNameRegex.Replace(text, m =>
                characters.TryGetValue(m.Groups["key"].Value, out var c) ? c.Name : m.Value);
            text = PrintPlayerNameRegex.Replace(text, playerName ?? "");
            text = BarePlayerNameRegex.Replace(text, playerName ?? "");
            return text.Trim();
        }
    }
}