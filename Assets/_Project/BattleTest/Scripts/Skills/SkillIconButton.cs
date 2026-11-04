using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class SkillIconButton : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Preview")]
    [SerializeField] private float dimmedAlpha = 0.4f;

    private PokerSkillData skillData;
    private CardSelection cardSelectionUI;
    private RectTransform rectTransform;

    private bool isDimmed;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    public void Setup(PokerSkillData skillData, CardSelection cardSelectionUI)
    {
        this.skillData = skillData;
        this.cardSelectionUI = cardSelectionUI;
        iconImage.sprite = skillData.icon;
    }

    /// <summary>Place l'icône directement à sa position finale, mais invisible (pour les nouvelles icônes).</summary>
    public void SetInitialPosition(float x)
    {
        rectTransform.anchoredPosition = new Vector2(x, rectTransform.anchoredPosition.y);
        canvasGroup.alpha = 0f;
    }

    /// <summary>Glisse vers x si besoin, et fait réapparaître en fondu si l'icône était invisible.</summary>
    public void AnimateToPosition(float x)
    {
        rectTransform.DOKill();
        canvasGroup.DOKill();

        rectTransform.DOAnchorPosX(x, moveDuration).SetEase(Ease.OutQuad);

        if (canvasGroup.alpha < 1f)
            canvasGroup.DOFade(1f, fadeDuration);
    }

    public void AnimateOut(Action onComplete)
    {
        rectTransform.DOKill();
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, fadeDuration).OnComplete(() => onComplete?.Invoke());
    }

    private void OnClicked()
    {
        cardSelectionUI.AutoSelectForCombo(skillData.requiredCombo);
    }

    public void SetPreviewDimmed(bool dimmed)
    {
        if (isDimmed == dimmed) return;
        isDimmed = dimmed;

        canvasGroup.DOKill();
        canvasGroup.DOFade(dimmed ? dimmedAlpha : 1f, fadeDuration);
    }
}