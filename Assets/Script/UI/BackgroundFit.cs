using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class BackgroundFit : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private RectTransform parentRect;
    [SerializeField] private bool centerAnchors = true;
    [SerializeField] private bool runEveryFrame = false;

    private RectTransform selfRect;

    private void Awake()
    {
        CacheRefs();
        Apply();
    }

    private void OnEnable()
    {
        CacheRefs();
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    private void Update()
    {
        if (runEveryFrame)
        {
            Apply();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheRefs();
        Apply();
    }
#endif

    private void CacheRefs()
    {
        if (selfRect == null)
        {
            selfRect = GetComponent<RectTransform>();
        }

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (parentRect == null)
        {
            parentRect = transform.parent as RectTransform;
        }
    }

    private void Apply()
    {
        if (selfRect == null || targetImage == null || parentRect == null)
        {
            return;
        }

        var sprite = targetImage.sprite;
        if (sprite == null)
        {
            return;
        }

        var parentSize = parentRect.rect.size;
        if (parentSize.x <= 0f || parentSize.y <= 0f)
        {
            return;
        }

        float spriteAspect = sprite.rect.width / sprite.rect.height;
        float parentAspect = parentSize.x / parentSize.y;

        float width;
        float height;
        if (parentAspect >= spriteAspect)
        {
            height = parentSize.y;
            width = height * spriteAspect;
        }
        else
        {
            width = parentSize.x;
            height = width / spriteAspect;
        }

        if (centerAnchors)
        {
            selfRect.anchorMin = new Vector2(0.5f, 0.5f);
            selfRect.anchorMax = new Vector2(0.5f, 0.5f);
            selfRect.anchoredPosition = Vector2.zero;
        }

        selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        if (!targetImage.preserveAspect)
        {
            targetImage.preserveAspect = true;
        }
    }
}
