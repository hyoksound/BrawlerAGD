using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderChange : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI sliderText;
    // Initializes the Slider Text Fields
    void Start()
    {
        sliderText.text = slider.value.ToString("0.00");

    }

    // Updates Text Field Depending on Slider Value;
    void Update()
    {
        if(slider.enabled == false) {
            sliderText.text = "∞";
        } else {
            sliderText.text = slider.value.ToString("0.00");

        }
    }
}
