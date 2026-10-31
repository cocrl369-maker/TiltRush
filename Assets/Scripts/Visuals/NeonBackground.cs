using UnityEngine;

namespace TiltRush
{
    /// <summary>
    /// Black background with a few thin, softly glowing lines that criss-cross like a net, with glowing
    /// nodes where they meet. Colours form a gradient and drift; everything gets brighter and livelier
    /// as Intensity (= difficulty) rises.
    /// </summary>
    public class NeonBackground : MonoBehaviour
    {
        public static float Intensity = 0.1f;   // 0..1, set by GameWorld

        const int Vertical = 7, Horizontal = 5;   // number of lines per direction (fewer = calmer)

        class NetLine
        {
            public SpriteRenderer Core, Glow;
            public bool IsVertical;
            public float Pos01, Angle0, Phase, Alpha;
            public Vector2 Center, Dir;
        }

        NetLine[] lines;
        SpriteRenderer[] nodes;
        float shown = 0.1f;
        Camera cam;

        void Start()
        {
            var rnd = new System.Random(11);   // fixed seed -> same irregular net every time
            lines = new NetLine[Vertical + Horizontal];
            for (int i = 0; i < lines.Length; i++)
            {
                bool v = i < Vertical;
                int idx = v ? i : i - Vertical, cnt = v ? Vertical : Horizontal;
                lines[i] = new NetLine
                {
                    IsVertical = v,
                    Pos01 = (idx + 0.5f + ((float)rnd.NextDouble() - 0.5f) * 0.8f) / cnt,      // irregular spacing
                    Angle0 = ((float)rnd.NextDouble() * 2f - 1f) * 0.55f,                      // random tilt
                    Phase = (float)rnd.NextDouble() * 6.28f,
                    Glow = Make("Glow", SpriteFactory.SoftBand(), -102),
                    Core = Make("Core", SpriteFactory.Pixel(), -101)
                };
            }
            nodes = new SpriteRenderer[Vertical * Horizontal];
            for (int i = 0; i < nodes.Length; i++) nodes[i] = Make("Node", SpriteFactory.Glow(), -100);
        }

        SpriteRenderer Make(string n, Sprite s, int order)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = s; r.sortingOrder = order;
            return r;
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || lines == null) return;

            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            shown = Mathf.Lerp(shown, Intensity, Time.unscaledDeltaTime * 1.5f);
            float h = cam.orthographicSize * 2f, w = h * cam.aspect, t = Time.time;
            float len = Mathf.Sqrt(w * w + h * h) * 1.3f;

            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i];
                float a = l.Angle0 + 0.12f * Mathf.Sin(t * 0.25f + l.Phase);
                l.Dir = l.IsVertical ? new Vector2(Mathf.Sin(a), Mathf.Cos(a)) : new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                l.Center = l.IsVertical
                    ? new Vector2((l.Pos01 - 0.5f) * w + 0.6f * Mathf.Sin(t * 0.2f + l.Phase), 0f)
                    : new Vector2(0f, (l.Pos01 - 0.5f) * h + 0.4f * Mathf.Sin(t * 0.23f + l.Phase));

                float hue = Mathf.Repeat(0.5f + i / (float)lines.Length * 0.4f + shown * 0.25f + t * 0.02f, 1f);
                float wave = 0.5f + 0.5f * Mathf.Sin(t * (1f + shown * 1.5f) - i * 0.9f);
                l.Alpha = Mathf.Lerp(0.06f, 0.5f, shown) * (0.55f + 0.45f * wave);
                Color c = Color.HSVToRGB(hue, 0.75f, 1f);
                float rot = Mathf.Atan2(l.Dir.y, l.Dir.x) * Mathf.Rad2Deg;

                l.Core.transform.localPosition = l.Center;
                l.Core.transform.localRotation = Quaternion.Euler(0, 0, rot);
                l.Core.transform.localScale = new Vector3(len, 0.045f, 1f);
                l.Core.color = new Color(c.r, c.g, c.b, l.Alpha);

                l.Glow.transform.localPosition = l.Center;
                l.Glow.transform.localRotation = Quaternion.Euler(0, 0, rot);
                l.Glow.transform.localScale = new Vector3(len, 0.9f, 1f);
                l.Glow.color = new Color(c.r, c.g, c.b, l.Alpha * 0.45f);
            }

            // Glowing nodes where a vertical-ish and a horizontal-ish line cross
            for (int i = 0; i < Vertical; i++)
                for (int j = 0; j < Horizontal; j++)
                {
                    var l1 = lines[i]; var l2 = lines[Vertical + j];
                    var node = nodes[i * Horizontal + j];
                    float den = l1.Dir.x * l2.Dir.y - l1.Dir.y * l2.Dir.x;
                    if (Mathf.Abs(den) < 0.1f) { node.color = Color.clear; continue; }
                    float tt = ((l2.Center.x - l1.Center.x) * l2.Dir.y - (l2.Center.y - l1.Center.y) * l2.Dir.x) / den;
                    Vector2 p = l1.Center + l1.Dir * tt;
                    node.transform.localPosition = p;
                    node.transform.localScale = Vector3.one * (0.7f + 0.3f * Mathf.Sin(t * 1.3f + i + j));
                    float al = (l1.Alpha + l2.Alpha) * 0.5f;
                    float hue = Mathf.Repeat(0.5f + (i + j) / (float)(Vertical + Horizontal) * 0.4f + shown * 0.25f + t * 0.02f, 1f);
                    Color c = Color.HSVToRGB(hue, 0.6f, 1f);
                    node.color = new Color(c.r, c.g, c.b, al * 1.2f);
                }
        }
    }
}
