using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Dialog
{
	public class YesNoDialog : DialogBase
	{

		/// <summary>
		/// YESボタン
		/// </summary>
		[SerializeField] private Button _yesButton;

		/// <summary>
		/// NOボタン
		/// </summary>
		[SerializeField] private Button _noButton;

		public Action OnYes;
		public Action OnNo;
		public Action OnThird;

		private Button _thirdButton;
		private TextMeshProUGUI _yesLabel;
		private TextMeshProUGUI _noLabel;
		private TextMeshProUGUI _thirdLabel;
		private string _yesButtonText = "YES";
		private string _noButtonText = "NO";
		private string _thirdButtonText = "";
		private bool _useThirdButton;
		private const float TwoButtonWidth = 275f;
		private const float TwoButtonXOffset = 155f;
		private const float ThreeButtonWidth = 275f;
		private const float ThreeButtonGap = 24f;

		private void Awake()
		{
			_yesLabel = FindLabel(_yesButton);
			_noLabel = FindLabel(_noButton);
		}

		private void Start()
		{
			if (_yesButton != null)
			{
				_yesButton.onClick.AddListener(() =>
				{
					OnYes?.Invoke();
					Close();
				});
			}

			if (_noButton != null)
			{
				_noButton.onClick.AddListener(() =>
				{
					OnNo?.Invoke();
					Close();
				});
			}

			ApplyButtonTexts();
			RefreshThirdButton();
		}

		public void SetButtonTexts(string yesText, string noText, string thirdText = null)
		{
			_yesButtonText = yesText;
			_noButtonText = noText;
			_thirdButtonText = thirdText ?? "";
			_useThirdButton = !string.IsNullOrEmpty(thirdText);
			ApplyButtonTexts();
			RefreshThirdButton();
		}

		private void ApplyButtonTexts()
		{
			if (_yesLabel != null)
			{
				_yesLabel.text = _yesButtonText;
			}

			if (_noLabel != null)
			{
				_noLabel.text = _noButtonText;
			}

			if (_thirdLabel != null)
			{
				_thirdLabel.text = _thirdButtonText;
			}
		}

		private void RefreshThirdButton()
		{
			if (!_useThirdButton)
			{
				RestoreTwoButtonLayout();
				if (_thirdButton != null)
				{
					_thirdButton.gameObject.SetActive(false);
				}
				return;
			}

			EnsureThirdButton();
			if (_thirdButton == null)
			{
				return;
			}

			_thirdButton.gameObject.SetActive(true);

			var yesRect = _yesButton != null ? _yesButton.GetComponent<RectTransform>() : null;
			var noRect = _noButton != null ? _noButton.GetComponent<RectTransform>() : null;
			var thirdRect = _thirdButton.GetComponent<RectTransform>();
			if (yesRect != null && noRect != null && thirdRect != null)
			{
				var baseY = yesRect.anchoredPosition.y;
				var offset = ThreeButtonWidth + ThreeButtonGap;
				yesRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ThreeButtonWidth);
				noRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ThreeButtonWidth);
				thirdRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ThreeButtonWidth);
				yesRect.anchoredPosition = new Vector2(-offset, baseY);
				thirdRect.anchoredPosition = new Vector2(0f, baseY);
				noRect.anchoredPosition = new Vector2(offset, baseY);
			}

			ApplyButtonTexts();
		}

		private void RestoreTwoButtonLayout()
		{
			var yesRect = _yesButton != null ? _yesButton.GetComponent<RectTransform>() : null;
			var noRect = _noButton != null ? _noButton.GetComponent<RectTransform>() : null;
			if (yesRect == null || noRect == null)
			{
				return;
			}

			var baseY = yesRect.anchoredPosition.y;
			yesRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TwoButtonWidth);
			noRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TwoButtonWidth);
			yesRect.anchoredPosition = new Vector2(-TwoButtonXOffset, baseY);
			noRect.anchoredPosition = new Vector2(TwoButtonXOffset, baseY);
		}

		private void EnsureThirdButton()
		{
			if (_thirdButton != null || _noButton == null)
			{
				return;
			}

			var thirdObject = Instantiate(_noButton.gameObject, _noButton.transform.parent);
			thirdObject.name = "ThirdButton";
			_thirdButton = thirdObject.GetComponent<Button>();
			_thirdLabel = FindLabel(_thirdButton);
			if (_thirdButton != null)
			{
				_thirdButton.onClick.RemoveAllListeners();
				_thirdButton.onClick.AddListener(() =>
				{
					OnThird?.Invoke();
					Close();
				});
			}
		}

		private static TextMeshProUGUI FindLabel(Button button)
		{
			if (button == null)
			{
				return null;
			}

			return button.GetComponentInChildren<TextMeshProUGUI>(true);
		}
	}
}
