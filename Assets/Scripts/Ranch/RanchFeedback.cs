using System.Collections;
using TMPro;
using UnityEngine;

// Phản hồi nhỏ cho người chơi khi chăn nuôi: chữ nổi lên rồi mờ dần, và hình trái tim vẽ bằng code.
public static class RanchFeedback
{
    private static Sprite heart;
    private static Sprite bubble;

    public static readonly Color Good = new Color(0.55f, 1f, 0.55f);
    public static readonly Color Info = new Color(1f, 0.95f, 0.75f);
    public static readonly Color Warn = new Color(1f, 0.6f, 0.5f);

    public static void Text(Vector3 position, string message, Color color)
    {
        GameObject go = new GameObject("RanchText");
        go.transform.position = position + new Vector3(0f, 1.2f, 0f);

        TextMeshPro text = go.AddComponent<TextMeshPro>();
        text.text = message;
        text.fontSize = 3f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(30, 20, 15, 255);
        text.rectTransform.sizeDelta = new Vector2(8f, 2f);
        text.sortingOrder = 500;

        RanchTextMover mover = go.AddComponent<RanchTextMover>();
        mover.Play(text);
    }

    // Trái tim 9x8 điểm ảnh, tạo một lần.
    public static Sprite Heart
    {
        get
        {
            if (heart != null) return heart;
            string[] rows =
            {
                ".oo...oo.",
                "oRRo.oRRo",
                "oRhRoRRRo",
                "oRRRRRRRo",
                ".oRRRRRo.",
                "..oRRRo..",
                "...oRo...",
                "....o....",
            };
            heart = FromRows(rows, new Color32(120, 20, 40, 255), new Color32(235, 60, 80, 255), new Color32(255, 190, 200, 255));
            return heart;
        }
    }

    // Nền bong bóng trắng 12x12 phía sau biểu tượng sản phẩm.
    public static Sprite Bubble
    {
        get
        {
            if (bubble != null) return bubble;
            string[] rows =
            {
                "..oooooooo..",
                ".oRRRRRRRRo.",
                "oRRRRRRRRRRo",
                "oRRRRRRRRRRo",
                "oRRRRRRRRRRo",
                "oRRRRRRRRRRo",
                "oRRRRRRRRRRo",
                "oRRRRRRRRRRo",
                ".oRRRRRRRRo.",
                "..oooRRoooo.",
                ".....oRo....",
                "......o.....",
            };
            bubble = FromRows(rows, new Color32(60, 45, 40, 255), new Color32(255, 250, 235, 255), new Color32(255, 255, 255, 255));
            return bubble;
        }
    }

    private static Sprite FromRows(string[] rows, Color32 outline, Color32 fill, Color32 shine)
    {
        int h = rows.Length, w = rows[0].Length;
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                char c = rows[h - 1 - y][x];
                Color32 color = c == 'o' ? outline : c == 'R' ? fill : c == 'h' ? shine : new Color32(0, 0, 0, 0);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
    }
}

// Cho chữ nổi lên 0,8 ô trong 1,4 giây rồi biến mất.
public class RanchTextMover : MonoBehaviour
{
    public void Play(TextMeshPro text) => StartCoroutine(Run(text));

    private IEnumerator Run(TextMeshPro text)
    {
        Vector3 start = transform.position;
        Color color = text.color;
        const float duration = 1.4f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            transform.position = start + new Vector3(0f, 0.8f * k, 0f);
            color.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            text.color = color;
            yield return null;
        }
        Destroy(gameObject);
    }
}
