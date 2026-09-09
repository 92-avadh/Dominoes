using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AnimationManager - DOTween-free animation system for UI Toolkit
/// Uses coroutines and custom easing for tile animations, transitions, and feedback
/// Compatible with Unity UI Toolkit (not legacy Transform)
/// </summary>
public class AnimationManager : MonoBehaviour
{
    public static AnimationManager Instance { get; private set; }

    [Header("Animation Settings")]
    [SerializeField] private float defaultDuration = 0.15f;
    [SerializeField] private float bounceDuration = 0.2f;
    [SerializeField] private float transitionDuration = 0.25f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    #region Easing Functions

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private float EaseOutCubic(float t) => 1f - (1f - t) * (1f - t) * (1f - t);
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * (t - 1f) * (t - 1f) * (t - 1f) + c1 * (t - 1f) * (t - 1f);
    }
    private float EaseOutElastic(float t)
    {
        if (t == 0f || t == 1f) return t;
        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }
    private float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if (t < 1f / d1) return n1 * t * t;
        if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f;
        if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }

    #endregion

    #region Tile Animations

    /// <summary>
    /// Animate tile pickup - scale up with slight rotation
    /// </summary>
    public void AnimateTilePickup(VisualElement tile)
    {
        if (tile == null) return;
        StopAllCoroutines();
        StartCoroutine(ScaleElement(tile, Vector3.one, new Vector3(1.15f, 1.15f, 1f), 0.15f, EaseOutQuad));
    }

    /// <summary>
    /// Animate tile placement - snap to position with bounce
    /// </summary>
    public void AnimateTilePlace(VisualElement tile)
    {
        if (tile == null) return;
        Vector3 currentScale = tile.transform.scale;
        Vector3 targetScale = new Vector3(1f, 1f, 1f);
        StartCoroutine(ScaleElement(tile, currentScale, targetScale, 0.2f, EaseOutBounce));
    }

    /// <summary>
    /// Animate invalid placement - shake + red flash
    /// </summary>
    public void AnimateInvalidPlacement(VisualElement tile)
    {
        if (tile == null) return;
        StartCoroutine(ShakeCoroutine(tile, 0.3f, 5f));
    }

    /// <summary>
    /// Animate tile selection - lift with glow
    /// </summary>
    public void AnimateTileSelect(VisualElement tile)
    {
        if (tile == null) return;
        StartCoroutine(ScaleElement(tile, tile.transform.scale, new Vector3(1.08f, 1.08f, 1f), 0.12f, EaseOutQuad));
    }

    /// <summary>
    /// Animate tile deselection
    /// </summary>
    public void AnimateTileDeselect(VisualElement tile)
    {
        if (tile == null) return;
        StartCoroutine(ScaleElement(tile, tile.transform.scale, new Vector3(1f, 1f, 1f), 0.1f, EaseOutQuad));
    }

    #endregion

    #region Screen Transitions

    /// <summary>
    /// Transition between screens with scale + fade
    /// </summary>
    public void TransitionScreens(VisualElement from, VisualElement to)
    {
        StartCoroutine(TransitionCoroutine(from, to));
    }

    private IEnumerator TransitionCoroutine(VisualElement from, VisualElement to)
    {
        // Exit animation
        if (from != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.25f;
                float ease = EaseOutQuad(t);
                from.style.opacity = Mathf.Lerp(1f, 0f, ease);
                from.transform.scale = Vector3.Lerp(Vector3.one, new Vector3(0.95f, 0.95f, 1f), ease);
                yield return null;
            }
            from.style.display = DisplayStyle.None;
        }

        // Enter animation
        if (to != null)
        {
            to.style.display = DisplayStyle.Flex;
            to.style.opacity = 0;
            to.transform.scale = new Vector3(1.05f, 1.05f, 1f);

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.3f;
                float ease = EaseOutBack(t);
                to.style.opacity = Mathf.Lerp(0f, 1f, ease);
                to.transform.scale = Vector3.Lerp(new Vector3(1.05f, 1.05f, 1f), Vector3.one, ease);
                yield return null;
            }
            to.style.opacity = 1;
            to.transform.scale = Vector3.one;
        }
    }

    /// <summary>
    /// Animate modal open - elastic scale + fade
    /// </summary>
    public void AnimateModalOpen(VisualElement modal)
    {
        StartCoroutine(ModalOpenCoroutine(modal));
    }

    private IEnumerator ModalOpenCoroutine(VisualElement modal)
    {
        if (modal == null) yield break;
        modal.style.display = DisplayStyle.Flex;
        modal.style.opacity = 0;
        modal.transform.scale = new Vector3(0.8f, 0.8f, 1f);

        float elapsed = 0f;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ease = EaseOutBack(t);
            modal.style.opacity = Mathf.Lerp(0f, 1f, t);
            modal.transform.scale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 1f), Vector3.one, ease);
            yield return null;
        }
        modal.style.opacity = 1;
        modal.transform.scale = Vector3.one;
    }

    /// <summary>
    /// Animate modal close - scale down + fade
    /// </summary>
    public void AnimateModalClose(VisualElement modal, System.Action onComplete = null)
    {
        StartCoroutine(ModalCloseCoroutine(modal, onComplete));
    }

    private IEnumerator ModalCloseCoroutine(VisualElement modal, System.Action onComplete)
    {
        if (modal == null) yield break;
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startScale = modal.transform.scale;
        float startOpacity = modal.style.opacity.value;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ease = EaseOutQuad(t);
            modal.style.opacity = Mathf.Lerp(startOpacity, 0f, t);
            modal.transform.scale = Vector3.Lerp(startScale, new Vector3(0.8f, 0.8f, 1f), ease);
            yield return null;
        }
        modal.style.display = DisplayStyle.None;
        onComplete?.Invoke();
    }

    #endregion

    #region Micro-interactions

    /// <summary>
    /// Button press feedback - scale down
    /// </summary>
    public void AnimateButtonPress(VisualElement button)
    {
        if (button == null) return;
        StartCoroutine(ScaleElement(button, button.transform.scale, new Vector3(0.95f, 0.95f, 1f), 0.05f, EaseOutQuad));
    }

    /// <summary>
    /// Button release feedback - scale back with bounce
    /// </summary>
    public void AnimateButtonRelease(VisualElement button)
    {
        if (button == null) return;
        StartCoroutine(ScaleElement(button, button.transform.scale, Vector3.one, 0.1f, EaseOutBack));
    }

    /// <summary>
    /// Pulse animation for turn indicator
    /// </summary>
    public void PulseElement(VisualElement element)
    {
        StartCoroutine(PulseCoroutine(element));
    }

    private IEnumerator PulseCoroutine(VisualElement element)
    {
        if (element == null) yield break;
        float elapsed = 0f;
        float duration = 0.6f;
        while (true)
        {
            elapsed += Time.deltaTime;
            float t = (Mathf.Sin(elapsed * Mathf.PI * 2f / duration) + 1f) / 2f;
            element.transform.scale = Vector3.Lerp(Vector3.one, new Vector3(1.05f, 1.05f, 1f), t);
            yield return null;
        }
    }

    /// <summary>
    /// Shake animation for errors
    /// </summary>
    public void ShakeElement(VisualElement element, float duration = 0.3f, float strength = 5f)
    {
        StartCoroutine(ShakeCoroutine(element, duration, strength));
    }

    private IEnumerator ShakeCoroutine(VisualElement element, float duration, float strength)
    {
        if (element == null) yield break;
        Vector2 originalPos = element.layout.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / duration);
            float x = Random.Range(-strength, strength) * decay;
            element.transform.position = new Vector2(originalPos.x + x, originalPos.y);
            yield return null;
        }
        element.transform.position = originalPos;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Generic scale animation coroutine for UI Toolkit elements
    /// </summary>
    private IEnumerator ScaleElement(VisualElement element, Vector3 from, Vector3 to, float duration, System.Func<float, float> ease)
    {
        if (element == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = ease(t);
            element.transform.scale = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }
        element.transform.scale = to;
    }

    /// <summary>
    /// Fade animation
    /// </summary>
    public void FadeElement(VisualElement element, float fromOpacity, float toOpacity, float duration)
    {
        StartCoroutine(FadeCoroutine(element, fromOpacity, toOpacity, duration));
    }

    private IEnumerator FadeCoroutine(VisualElement element, float from, float to, float duration)
    {
        if (element == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            element.style.opacity = Mathf.Lerp(from, to, EaseOutQuad(elapsed / duration));
            yield return null;
        }
        element.style.opacity = to;
    }

    /// <summary>
    /// Move animation
    /// </summary>
    public void MoveElement(VisualElement element, Vector2 from, Vector2 to, float duration)
    {
        StartCoroutine(MoveCoroutine(element, from, to, duration));
    }

    private IEnumerator MoveCoroutine(VisualElement element, Vector2 from, Vector2 to, float duration)
    {
        if (element == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(elapsed / duration);
            element.transform.position = Vector2.Lerp(from, to, t);
            yield return null;
        }
        element.transform.position = to;
    }

    #endregion
}
