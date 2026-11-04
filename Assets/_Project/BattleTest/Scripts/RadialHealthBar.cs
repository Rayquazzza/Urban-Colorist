using UnityEngine;
using UnityEngine.UI;
using Core.Tools;
[RequireComponent(typeof(Image))]
public class RadialHealthBar : MonoBehaviour
{
    /// the start fill amount value 
    public float StartValue = 1f;
    /// the end goad fill amount value
    public float EndValue = 0f;
    /// the distance to the start or end value at which the class should start lerping
    public float Tolerance = 0.01f;

    private float _newPercent;

    private Image _radialImage;

    private void Awake()
    {
        _radialImage = GetComponent<Image>(); 
    }
    public void UpdateBar(float currentValue, float minValue, float maxValue)
    {
        _newPercent = Maths.Remap(currentValue, minValue, maxValue, StartValue, EndValue);
        if (_radialImage == null) { return; }
        _radialImage.fillAmount = _newPercent;
        if (_radialImage.fillAmount > 1 - Tolerance)
        {
            _radialImage.fillAmount = 1;
        }
        if (_radialImage.fillAmount < Tolerance)
        {
            _radialImage.fillAmount = 0;
        }
    }
}
