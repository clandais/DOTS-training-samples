using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AntsPheromones.Behaviours
{
	public class SimulationSpeedSlider : MonoBehaviour
	{
		[SerializeField] private Slider slider;
		[SerializeField] private TMP_Text sliderValueText;
		
		public float Value => slider.value;
		
		private void Start()
		{
			slider.onValueChanged.AddListener(OnSliderValueChanged);
			OnSliderValueChanged(slider.value);
		}

		private void OnDestroy()
		{
			slider.onValueChanged.RemoveListener(OnSliderValueChanged);
		}

		void OnSliderValueChanged(float value)
		{
			sliderValueText.text = $"{value}";
		}
	}
}