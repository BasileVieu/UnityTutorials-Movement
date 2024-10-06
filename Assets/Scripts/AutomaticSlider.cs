using UnityEngine;
using UnityEngine.Events;

public class AutomaticSlider : MonoBehaviour
{
    [System.Serializable] public class OnValueChangedEvent : UnityEvent<float>
    {

    }
    
    [SerializeField][Min(0.01f)] private float duration = 1.0f;
    [SerializeField] private OnValueChangedEvent onValueChanged;

    private float value;

    private void FixedUpdate()
    {
        value += Time.deltaTime / duration;

        if (value >= 1.0f)
        {
            value = 1.0f;

            enabled = false;
        }

        onValueChanged.Invoke(value);
    }
}