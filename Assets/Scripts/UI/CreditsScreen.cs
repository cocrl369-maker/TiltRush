using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// CREDITS: reads Assets/Resources/credits.txt (a normal text file you can edit any time).
    /// Lines starting with "#" are headings, every other line is shown as it is. Long lists scroll.
    /// Example:
    ///   # MUSIC
    ///   Main menu: "Song Name" by Artist (newgrounds.com/art/view/artist)
    ///   # GAME
    ///   Game design: Your Name
    /// </summary>
    public class CreditsScreen : UIScreen
    {
        CardDeck deck;
        protected override bool HasVignette { get { return true; } }

        protected override void Build()
        {
            var title = UIKit.Title(transform, "CREDITS", 90);
            UIKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(1200, 115));
            var panel = UIKit.Panel(transform, 0, 140, 1640, 780);
            deck = CardDeck.Create(panel, new Vector2(1500, 730));

            var c = deck.NewCard();
            var file = Resources.Load<TextAsset>("credits") ?? Resources.Load<TextAsset>("Music/credits");
            if (file != null && file.text.Trim().Length > 0)
            {
                foreach (var raw in file.text.Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#")) c.Heading(line.TrimStart('#').Trim().ToUpper(), Neon.Aqua);
                    else c.Para(line);
                }
            }
            else
            {
                c.Heading("MUSIC", Neon.Aqua);
                c.Para("No credits yet. Create the text file Assets/Resources/credits.txt and write one line per track, " +
                       "e.g.  Main menu: \"Song Name\" by Artist (newgrounds.com)");
            }
            c.Heading("MADE WITH", Neon.Aqua);
            c.Para("Unity");
            deck.Finish();

            MenuUtil.AddBack(transform, () => UI.Show<MainMenuScreen>());
        }

        public override void OnShow() { deck.Show(0, false); }
    }
}
