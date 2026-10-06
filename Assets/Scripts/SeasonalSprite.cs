using System.Collections.Generic;
using UnityEngine;

// Đổi sprite của một object theo mùa (cây, bụi...). Mỗi mùa có một danh sách sprite (các danh sách nên dài bằng nhau):
// object giữ nguyên "biến thể" của mình qua các mùa - ví dụ bụi số 3 mùa Xuân sẽ thành bụi số 3 của mùa Thu.
// SeasonMapManager gọi ApplyAll khi đổi mùa; object sinh ra sau đó tự áp mùa hiện tại khi được bật.
// Được gắn và điền sprite tự động bằng menu Tools > Map > Build Season Trees & Bushes.
[DisallowMultipleComponent]
public class SeasonalSprite : MonoBehaviour
{
    [Tooltip("SpriteRenderer cần đổi (để trống = SpriteRenderer trên object này).")]
    [SerializeField] private SpriteRenderer target;

    [SerializeField] private Sprite[] spring;
    [SerializeField] private Sprite[] summer;
    [SerializeField] private Sprite[] autumn;
    [SerializeField] private Sprite[] winter;

    private static readonly HashSet<SeasonalSprite> active = new HashSet<SeasonalSprite>();
    private int variant = -1;

    private void OnEnable()
    {
        active.Add(this);
        Apply(SeasonMapManager.ActiveSeason);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    public static void ApplyAll(Season season)
    {
        foreach (SeasonalSprite seasonal in active)
        {
            seasonal.Apply(season);
        }
    }

    public void Apply(Season season)
    {
        SpriteRenderer spriteRenderer = Renderer();
        Sprite[] sprites = Sprites(season);
        if (spriteRenderer == null || sprites == null || sprites.Length == 0) return;

        if (variant < 0) variant = FindVariant(spriteRenderer.sprite);
        Sprite sprite = sprites[variant % sprites.Length];
        if (sprite != null && spriteRenderer.sprite != sprite) spriteRenderer.sprite = sprite;
    }

    // Dùng trong Editor (tool tạo sprite mùa).
    public void SetSprites(SpriteRenderer renderer, Sprite[] springSprites, Sprite[] summerSprites, Sprite[] autumnSprites, Sprite[] winterSprites)
    {
        target = renderer;
        spring = springSprites;
        summer = summerSprites;
        autumn = autumnSprites;
        winter = winterSprites;
        variant = -1;
    }

    private SpriteRenderer Renderer()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        return target;
    }

    private Sprite[] Sprites(Season season)
    {
        switch (season)
        {
            case Season.Summer: return summer;
            case Season.Autumn: return autumn;
            case Season.Winter: return winter;
            default: return spring;
        }
    }

    // Vị trí sprite hiện tại trong danh sách của mùa nào đó; không có thì chọn theo vị trí object (ổn định giữa các lần chạy).
    private int FindVariant(Sprite current)
    {
        foreach (Sprite[] sprites in new[] { spring, summer, autumn, winter })
        {
            if (sprites == null) continue;
            int index = System.Array.IndexOf(sprites, current);
            if (index >= 0) return index;
        }

        Vector3 position = transform.position;
        int hash = Mathf.RoundToInt(position.x * 7.31f) * 73856093 ^ Mathf.RoundToInt(position.y * 7.31f) * 19349663;
        return (hash & int.MaxValue) % 997;
    }
}
