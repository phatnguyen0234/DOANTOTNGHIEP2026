using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour
{
    [Header("Time Settings")]
    public float currentHour = 12f;
    public float timeSpeed = 1f;

    [Header("Sprite and Lighting Settings")]
    public Image clockHandImage;
    public Sprite[] clockHandSprites;
    public Light2D globalLight;
    public Gradient lightColor;
    public AnimationCurve lightIntensity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        currentHour += Time.deltaTime * timeSpeed;
        if (currentHour >= 24f)
        {
            currentHour = 0f;
            //Them event ngay moi o day
        }
        UpdateClockUI();
        UpdateLighting();
    }

    private void UpdateClockUI()
    {
        if(clockHandImage == null || clockHandSprites.Length == 0) return;
        
        float dayProgress = currentHour / 24f;

        int spriteIndex = Mathf.FloorToInt(dayProgress * clockHandSprites.Length);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, clockHandSprites.Length - 1); //ep gia tri trong khoang 0 den clockHandSprites.Length - 1
        clockHandImage.sprite = clockHandSprites[spriteIndex];
    }

    private void UpdateLighting()
    {
        if(globalLight == null) return;

        float dayProgress = currentHour / 24f;

        globalLight.color = lightColor.Evaluate(dayProgress); //gan mau sac anh sang
        globalLight.intensity = lightIntensity.Evaluate(dayProgress); //gan cuong do anh sang   
    }
}
