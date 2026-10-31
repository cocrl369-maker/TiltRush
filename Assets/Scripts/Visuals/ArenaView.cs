using UnityEngine;

namespace TiltRush
{
    /// <summary>Draws the playfield: darker interior + glowing white border.</summary>
    public static class ArenaView
    {
        public static Transform Build(Transform parent)
        {
            var root = new GameObject("Arena").transform;
            root.SetParent(parent, false);
            Vector2 s = Config.ArenaSize;
            const float t = 0.1f;

            Quad(root, Vector2.zero, s, new Color(0, 0, 0, 0.6f), -50);
            Color edge = new Color(1, 1, 1, 0.55f);
            Quad(root, new Vector2(0, s.y / 2 + t / 2), new Vector2(s.x + t * 2, t), edge, -40);
            Quad(root, new Vector2(0, -s.y / 2 - t / 2), new Vector2(s.x + t * 2, t), edge, -40);
            Quad(root, new Vector2(s.x / 2 + t / 2, 0), new Vector2(t, s.y), edge, -40);
            Quad(root, new Vector2(-s.x / 2 - t / 2, 0), new Vector2(t, s.y), edge, -40);
            return root;
        }

        static void Quad(Transform parent, Vector2 pos, Vector2 size, Color c, int order)
        {
            var go = new GameObject("Quad");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = SpriteFactory.Pixel(); r.color = c; r.sortingOrder = order;
        }
    }
}
