using UnityEngine;

namespace TiltRush
{
    /// <summary>A Geometry-Dash style shape: coloured fill + bright outline + soft glow behind.</summary>
    public class ShapeView : MonoBehaviour
    {
        SpriteRenderer fill, line, glow;
        Color fillC, lineC;
        float glowA, alpha = 1f;

        public float Alpha { get { return alpha; } set { alpha = value; Apply(); } }

        public static ShapeView Create(Transform parent, ShapeType shape, float radius,
                                       Color fillColor, Color lineColor, float glowAlpha = 0.35f, int order = 0)
        {
            var go = new GameObject("Shape");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<ShapeView>();
            v.glow = v.Make("Glow", SpriteFactory.Glow(), order - 1);
            v.fill = v.Make("Fill", SpriteFactory.Fill(shape), order);
            v.line = v.Make("Line", SpriteFactory.Outline(shape), order + 1);
            v.glowA = glowAlpha;
            v.SetRadius(radius);
            v.SetColors(fillColor, lineColor);
            return v;
        }

        SpriteRenderer Make(string n, Sprite s, int order)
        {
            var g = new GameObject(n);
            g.transform.SetParent(transform, false);
            var r = g.AddComponent<SpriteRenderer>();
            r.sprite = s;
            r.sortingOrder = order;
            return r;
        }

        public void SetRadius(float r)
        {
            float s = r * 2f / 0.9f;   // shapes fill ~90% of their sprite
            fill.transform.localScale = line.transform.localScale = Vector3.one * s;
            glow.transform.localScale = Vector3.one * r * 6f;
        }

        public void SetColors(Color fillColor, Color lineColor) { fillC = fillColor; lineC = lineColor; Apply(); }

        void Apply()
        {
            Color f = fillC; f.a *= alpha; fill.color = f;
            Color l = lineC; l.a *= alpha; line.color = l;
            glow.color = new Color(lineC.r, lineC.g, lineC.b, glowA * alpha * lineC.a);
        }
    }
}
