using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// Draws the playfield: darker interior + glowing border. The border reacts to the game: its colour shifts from
    /// cool white-cyan to hot red as the difficulty rises, and it pulses with the music (or a calm heartbeat
    /// when no music is playing).
    /// </summary>
    public class ArenaView : MonoBehaviour
    {
        SpriteRenderer[] cores, glows;

        public static Transform Build(Transform parent)
        {
            var root = new GameObject("Arena").transform;
            root.SetParent(parent, false);
            var v = root.gameObject.AddComponent<ArenaView>();
            Vector2 s = Config.ArenaSize;
            const float t = 0.1f, g = 0.9f;

            Quad(root, SpriteFactory.Pixel(), Vector2.zero, s, 0f, new Color(0, 0, 0, 0.6f), -50);

            // top, bottom, right, left
            Vector2[] pos = { new Vector2(0, s.y / 2 + t / 2), new Vector2(0, -s.y / 2 - t / 2), new Vector2(s.x / 2 + t / 2, 0), new Vector2(-s.x / 2 - t / 2, 0) };
            float[] len = { s.x + t * 2, s.x + t * 2, s.y + t * 2, s.y + t * 2 };
            v.cores = new SpriteRenderer[4];
            v.glows = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                float rot = i < 2 ? 0f : 90f;
                v.glows[i] = Quad(root, SpriteFactory.SoftBand(), pos[i], new Vector2(len[i] + g, g), rot, Color.clear, -41);
                v.cores[i] = Quad(root, SpriteFactory.Pixel(), pos[i], new Vector2(len[i], t), rot, Color.clear, -40);
            }
            v.Apply(0f);
            return root;
        }

        static SpriteRenderer Quad(Transform parent, Sprite sprite, Vector2 pos, Vector2 size, float rot, Color c, int order)
        {
            var go = new GameObject("Quad");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 0, rot);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite; r.color = c; r.sortingOrder = order;
            return r;
        }

        void Update()
        {
            var audio = App.I != null ? App.I.Audio : null;
            float d = NeonBackground.Intensity;
            float pulse;
            if (audio != null && audio.HasMusic) pulse = audio.Beat;
            else
            {
                // no music: calm double heartbeat that gets faster with the difficulty
                float ph = Mathf.Repeat(Time.unscaledTime * (0.7f + 1.3f * d), 1f);
                pulse = Mathf.Exp(-ph * 14f) + 0.6f * Mathf.Exp(-Mathf.Max(0f, ph - 0.22f) * 14f) * (ph > 0.22f ? 1f : 0f);
                pulse = Mathf.Clamp01(pulse) * 0.7f;
            }
            Apply(pulse);
        }

        void Apply(float pulse)
        {
            float d = NeonBackground.Intensity;
            float hue = Mathf.Lerp(0.52f, 0.98f, Mathf.SmoothStep(0f, 1f, d));   // cyan -> violet -> red
            Color c = Color.HSVToRGB(hue, Mathf.Lerp(0.12f, 0.75f, d), 1f);
            float core = Mathf.Clamp01(0.5f + 0.4f * pulse);
            float glow = (0.08f + 0.35f * pulse) * (0.45f + 0.55f * d);
            for (int i = 0; i < 4; i++)
            {
                cores[i].color = new Color(c.r, c.g, c.b, core);
                glows[i].color = new Color(c.r, c.g, c.b, glow);
            }
        }
    }
}
