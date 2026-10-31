using UnityEngine;

namespace TiltRush
{
    /// <summary>A Geometry-Dash style shape: coloured fill + bright outline + soft glow behind.</summary>
    public class ShapeView : MonoBehaviour
    {
        SpriteRenderer fill, line, glow, backing;
        float backingScale;
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
            if (backing != null) backing.transform.localScale = Vector3.one * s * backingScale;
        }

        /// <summary>Swap the shape (used when pooled objects are reused as a different type).</summary>
        public void SetShape(ShapeType shape)
        {
            fill.sprite = SpriteFactory.Fill(shape);
            line.sprite = SpriteFactory.Outline(shape);
            if (backing != null) backing.sprite = fill.sprite;
        }

        public void SetGlow(float alpha) { glowA = alpha; Apply(); }

        /// <summary>
        /// Dark silhouette slightly bigger than the shape, drawn behind it - a thick dark outline like the icons in GD.
        /// Keeps the shape readable on any background colour.
        /// </summary>
        public void AddBacking(Color color, float scale)
        {
            var g = new GameObject("Backing");
            g.transform.SetParent(transform, false);
            backing = g.AddComponent<SpriteRenderer>();
            backing.sprite = fill.sprite;
            backing.sortingOrder = fill.sortingOrder - 1;
            glow.sortingOrder = fill.sortingOrder - 2;
            backing.color = color;
            backingScale = scale;
            backing.transform.localScale = fill.transform.localScale * scale;
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
