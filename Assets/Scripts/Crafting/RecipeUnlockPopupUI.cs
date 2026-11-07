using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeUnlockPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Canvas Group of the notification panel for fading effects")]
    public CanvasGroup popupCanvasGroup;

    [Tooltip("The Grid Layout or Horizontal Layout container for icons")]
    public Transform iconGridContainer;

    [Tooltip("A simple Image UI prefab to display the recipe result")]
    public GameObject iconPrefab;

    [Header("Settings")]
    public float displayDuration = 4f;
    public float fadeSpeed = 5f;

    // Buffer to collect all recipes unlocked in the same frame/action
    private List<CraftingRecipe> currentBatch = new List<CraftingRecipe>();
    private bool isGathering = false;
    private bool isDisplaying = false;

    private void OnEnable()
    {
        popupCanvasGroup.alpha = 0;
        popupCanvasGroup.interactable = false;
        popupCanvasGroup.blocksRaycasts = false;

        StartCoroutine(RegisterEventRoutine());
    }
    private System.Collections.IEnumerator RegisterEventRoutine()
    {
        yield return new WaitUntil(() => PlayerRecipeManager.Instance != null);
        PlayerRecipeManager.Instance.OnShowUnlockNotification += HandleUnlockNotification;
    }

    private void OnDisable()
    {
        if (PlayerRecipeManager.Instance != null)
        {
            PlayerRecipeManager.Instance.OnShowUnlockNotification -= HandleUnlockNotification;
        }
    }

    private void HandleUnlockNotification(CraftingRecipe recipe)
    {
        currentBatch.Add(recipe);

        // If we aren't already waiting to display a batch, start the coroutine
        if (!isGathering && !isDisplaying)
        {
            StartCoroutine(GatherAndDisplayBatch());
        }
    }

    private IEnumerator GatherAndDisplayBatch()
    {
        isGathering = true;

        // Wait 0.1 seconds to allow all simultaneous unlocks to be added to the batch
        yield return new WaitForSeconds(0.1f);

        isGathering = false;
        isDisplaying = true;

        // 1. Clear any old icons from previous notifications
        foreach (Transform child in iconGridContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Instantiate new icons for the current batch
        foreach (var recipe in currentBatch)
        {
            if (recipe != null && recipe.resultItem != null)
            {
                GameObject newIcon = Instantiate(iconPrefab, iconGridContainer);
                Image img = newIcon.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = recipe.resultItem.Icon;
                }
            }
        }

        // Clear the batch buffer for future unlocks
        currentBatch.Clear();

        // 3. Fade In
        while (popupCanvasGroup.alpha < 1f)
        {
            popupCanvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        popupCanvasGroup.alpha = 1f;

        // 4. Wait for duration OR until Left Mouse Click
        float timer = 0f;
        while (timer < displayDuration)
        {
            // Close immediately if Left Mouse Button (0) is clicked
            if (Input.GetMouseButtonDown(0))
            {
                break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // 5. Fade Out
        while (popupCanvasGroup.alpha > 0f)
        {
            popupCanvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        popupCanvasGroup.alpha = 0f;

        isDisplaying = false;

        // If new unlocks came in while we were fading out, process the next batch
        if (currentBatch.Count > 0)
        {
            StartCoroutine(GatherAndDisplayBatch());
        }
    }
}