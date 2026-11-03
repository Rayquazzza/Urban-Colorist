using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using Unity.Plastic.Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static PlasticPipe.PlasticProtocol.Client.ConnectionCreator.PlasticProtoSocketConnection;

public enum Suit { Coeur, Carreau, Trefle, Pique }
public enum Rank { Un = 1, Deux, Trois, Quatre, Cinq, Six, Sept }

[RequireComponent(typeof(RectTransform))]
[RequireComponent (typeof(Image))]
public class Card : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Image _image;
    private RectTransform _rectTransform;


    //--- LOGIC ---
    private Suit _suit;
    private Rank _rank;
    public Suit Suit => _suit;
    public Rank Rank => _rank;


    //--- VISUALS ---
    [BoxGroup("Sélection"), Tooltip("Décalage vertical (pixels) quand la carte est sélectionnée par clic.")]
    [SerializeField] private float _selectedRaiseAmount = 50f;
    [BoxGroup("Sélection")]
    [SerializeField] private float _selectionAnimDuration = 0.15f;

    [BoxGroup("Drag")]
    [SerializeField] private float _dragUpThreshold = 80f;
    [BoxGroup("Drag"), Tooltip("Plus c'est grand, plus la carte 'traîne' derrière la souris.")]
    [SerializeField] private float _followSmoothTime = 0.08f;
    [BoxGroup("Drag")]
    [SerializeField] private float _returnDuration = 0.25f;

    [BoxGroup("Limites de drag"), Tooltip("Distance verticale maximale vers le HAUT autorisée pendant le drag.")]
    [SerializeField] private float _maxDragUpDistance = 220f;
    [BoxGroup("Limites de drag"), Tooltip("Distance verticale maximale vers le BAS autorisée (généralement petite, juste pour un léger feedback).")]
    [SerializeField] private float _maxDragDownDistance = 40f;
    [BoxGroup("Limites de drag"), Tooltip("Distance horizontale maximale autorisée dans un sens ou l'autre.")]
    [SerializeField] private float _maxDragHorizontalDistance = 60f;

    [BoxGroup("Locked State"), Tooltip("Teinte appliquée à la carte quand elle n'est pas jouable (tour ennemi, résolution en cours).")]
    [SerializeField] private Color _lockedTintColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [BoxGroup("Locked State"), Tooltip("Décalage vertical vers le bas quand la carte est verrouillée.")]
    [SerializeField] private float _lockedLowerAmount = 30f;
    [BoxGroup("Locked State")]
    [SerializeField] private float _lockedAnimDuration = 0.25f;

    private CardSelection _selection;
    private bool _isSelected;
    private bool _isDragging;
    private bool _isLocked;

    private Canvas _parentCanvas;
    private Vector3 _arrangedLocalPosition;   // position de base donnée par CardHandLayout
    private Quaternion _arrangedLocalRotation;
    private Vector2 _dragStartScreenPosition;
    private Vector3 _dragTargetLocalPosition;
    private Vector3 _dragVelocity;
    private Color _originalColor;


    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        _originalColor = _image.color;
    }
    public void SetCard(Suit suit, Rank rank, CardSelection selection)
    {
        _suit = suit;
        _rank = rank;

        _selection = selection;
        SetSelected(false);
    }

    private void Update()
    {
        if (!_isDragging) return;

        _rectTransform.localPosition = Vector3.SmoothDamp(_rectTransform.localPosition, _dragTargetLocalPosition, ref _dragVelocity, _followSmoothTime);
    }

    private void OnDisable()
    {
        if (this.gameObject != null) _rectTransform.DOKill();
    }

    private void OnDestroy()
    {
        if(this.gameObject != null) _rectTransform.DOKill();
    }

    public void SetSprite(Sprite sprite)
    {
        _image.sprite = sprite;
    }

    public void SetArrangedTransform(Vector3 position, Quaternion rotation, bool animate, float duration)
    {
        _arrangedLocalPosition = position;
        _arrangedLocalRotation = rotation;

        if (_isDragging) return;

        Vector3 restingPosition = GetRestingPosition();

        _rectTransform.DOKill();
        if (!animate)
        {
            _rectTransform.anchoredPosition = position;
            _rectTransform.localRotation = rotation;
            return;
        }
        _rectTransform.DOAnchorPos(restingPosition, duration).SetEase(Ease.OutQuad);
        _rectTransform.DOLocalRotateQuaternion(_arrangedLocalRotation, duration).SetEase(Ease.OutQuad);
    }

    private Vector3 GetRestingPosition()
    {
        Vector3 basePos = _arrangedLocalPosition + (_isSelected ? Vector3.up * _selectedRaiseAmount : Vector3.zero);
        return _isLocked ? basePos + Vector3.down * _lockedLowerAmount : basePos;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isLocked) return;
        _selection.ToggleCard(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if(_isLocked) return;

        _rectTransform.DOKill();

        _dragStartScreenPosition = eventData.position;
        _dragTargetLocalPosition = _rectTransform.localPosition;
        _dragVelocity = Vector3.zero;
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isLocked) return;

        float scaleFactor = _parentCanvas != null ? _parentCanvas.scaleFactor : 1f;
        Vector2 screenDelta = eventData.position -_dragStartScreenPosition;
        Vector3 localDelta = new Vector3(screenDelta.x, screenDelta.y, 0f) / scaleFactor;

        localDelta.x = Mathf.Clamp(localDelta.x, -_maxDragHorizontalDistance, _maxDragHorizontalDistance);
        localDelta.y = Mathf.Clamp(localDelta.y, -_maxDragDownDistance, _maxDragUpDistance);

        _dragTargetLocalPosition = GetRestingPosition() + localDelta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(_isLocked) return;

        _isDragging = false;
        float verticalDelta = eventData.position.y - _dragStartScreenPosition.y;

        bool played = false;
        if(verticalDelta >= _dragUpThreshold)
        {
            //selection
            //played = 
        }

        if(!played)
        {
            ReturnToRestingPosition();
        }
    }

    private void ReturnToRestingPosition()
    {
        _rectTransform.DOKill();
        _rectTransform.DOLocalMove(GetRestingPosition(), _returnDuration).SetEase(Ease.OutBack);
        _rectTransform.DOLocalRotateQuaternion(_arrangedLocalRotation,_returnDuration).SetEase(Ease.OutBack);
    }

    public void SetSelected(bool selected)
    {
        if(_isSelected ==  selected) return;

        _isSelected = selected;

        if (_isDragging) return;

        _rectTransform.DOKill();
        _rectTransform.DOLocalMove(GetRestingPosition(), _selectionAnimDuration).SetEase(Ease.OutQuad);
    }

    public void SetLocked(bool locked)
    {
        if (_isLocked == locked) return;

        _isLocked = locked;

        Color targetColor = locked ? _lockedTintColor : _originalColor;
        DOTween.To(() => _image.color, c => _image.color = c, targetColor, _lockedAnimDuration);

        if (_isDragging) return;

        _rectTransform.DOKill();
        _rectTransform.DOLocalMove(GetRestingPosition(), _lockedAnimDuration).SetEase(Ease.OutQuad);
    }
}