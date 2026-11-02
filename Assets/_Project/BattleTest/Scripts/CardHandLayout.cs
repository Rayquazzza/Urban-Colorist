using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    public class CardHandLayout : MonoBehaviour
    {
        [Header("Écartement horizontal")]
        [SerializeField] private float spacing = 140f;

        [Header("Courbe verticale (indépendante de l'écartement)")]
        [SerializeField] private float curveHeight = 60f;

        [Header("Rotation")]
        [SerializeField] private float rotationPerCard = 4f;

        [Header("Animation")]
        [SerializeField] private bool animateTransition = true;
        [SerializeField] private float transitionDuration = 0.25f;

        //public void ArrangeCards(List<CardView> cards)
        //{
        //    int count = cards.Count;
        //    if (count == 0) return;

        //    float middle = (count - 1) / 2f;

        //    for (int i = 0; i < count; i++)
        //    {
        //        float offset = i - middle;
        //        float normalizedOffset = middle > 0 ? offset / middle : 0f;

        //        float x = offset * spacing;
        //        float y = -curveHeight * (normalizedOffset * normalizedOffset);
        //        float zRotation = offset * rotationPerCard;

        //        Vector3 targetPosition = new Vector3(x, y, 0f);
        //        Quaternion targetRotation = Quaternion.Euler(0f, 0f, -zRotation);

        //        cards[i].SetArrangedTransform(targetPosition, targetRotation, animateTransition, transitionDuration);
        //    }
        //}
    }
}