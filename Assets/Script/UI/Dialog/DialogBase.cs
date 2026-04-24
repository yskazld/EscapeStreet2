using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Dialog
{
	/// <summary>
	/// ダイアログ一個単位の挙動
	/// </summary>
	public class DialogBase : MonoBehaviour
	{
		/// <summary>
		/// 表示するテキスト
		/// </summary>
		[SerializeField] private TextMeshProUGUI _dialogText;

		/// <summary>
		/// ボタン
		/// </summary>
		[SerializeField] private Button _oneSelectButton;

		[SerializeField] private bool _enableTypewriter = true;
		[SerializeField] private float _secondsPerChar = 0.01f;

		/// <summary>
		/// 表示する画像
		/// </summary>
		[SerializeField] protected Image _image;

		public Action OnClose;

		private Coroutine _typewriterCoroutine;
		private bool _isTyping;
		private string _fullText = "";
		private RectTransform _imageRectTransform;
		private Vector2 _imageMaxSize;
		private Vector2 _imageAnchoredPosition;
		private bool _hasImageMaxSize;
		private Sprite _runtimeImageSprite;

		private void Start()
		{
			if (_oneSelectButton != null)
			{
				_oneSelectButton.onClick.AddListener(() =>
				{
					if (_isTyping)
					{
						CompleteTypewriter();
						return;
					}
					Close();
				});
			}
		}


		/// <summary>
		/// 文字の設定
		/// </summary>
		/// <param name="text"></param>
		public void SetText(string text)
		{
			_fullText = text ?? "";
			if (_typewriterCoroutine != null)
			{
				StopCoroutine(_typewriterCoroutine);
				_typewriterCoroutine = null;
			}

			if (!_enableTypewriter)
			{
				_dialogText.text = _fullText;
				_isTyping = false;
				return;
			}

			_typewriterCoroutine = StartCoroutine(TypewriterRoutine(_fullText));
		}

		/// <summary>
		/// 文字サイズの設定
		/// </summary>
		/// <param name="size"></param>
		/// <param name="enableAutoSize"></param>
		public void SetFontSize(float size, bool enableAutoSize = false)
		{
			if (_dialogText == null)
			{
				return;
			}

			_dialogText.enableAutoSizing = enableAutoSize;
			_dialogText.fontSize = size;
		}

		/// <summary>
		/// 消したときの処理
		/// </summary>
		public void Close()
		{
			ReleaseRuntimeImageSprite();
			OnClose?.Invoke();
			Destroy(gameObject);
		}

		public void OffImage()
		{
			_image.gameObject.SetActive(false);
		}

		/// <summary>
		/// 画像を表示する
		/// </summary>
		/// <param name="link"></param>
		public void SetImage(string link)
		{
			ReleaseRuntimeImageSprite();

			// 分割Spriteではなくテクスチャ全体をそのまま表示する
			var texture = Resources.Load<Texture2D>(link);
			Sprite sprite = null;
			if (texture != null)
			{
				_runtimeImageSprite = Sprite.Create(
					texture,
					new Rect(0f, 0f, texture.width, texture.height),
					new Vector2(0.5f, 0.5f),
					100f);
				sprite = _runtimeImageSprite;
			}
			else
			{
				sprite = Resources.Load<Sprite>(link);
			}

			if (sprite != null)
			{
				CacheImageBounds();

				//画像が存在するなら表示
				_image.sprite = sprite;
				_image.preserveAspect = true;
				FitImageToBounds(sprite);
				_image.gameObject.SetActive(true);
			}
			else
			{
				//ないなら消す
				_image.gameObject.SetActive(false);
			}
		}

		private void ReleaseRuntimeImageSprite()
		{
			if (_runtimeImageSprite == null)
			{
				return;
			}

			Destroy(_runtimeImageSprite);
			_runtimeImageSprite = null;
		}

		private void CacheImageBounds()
		{
			if (_hasImageMaxSize || _image == null)
			{
				return;
			}

			_imageRectTransform = _image.rectTransform;
			if (_imageRectTransform == null)
			{
				return;
			}

			_imageMaxSize = _imageRectTransform.rect.size;
			_imageAnchoredPosition = _imageRectTransform.anchoredPosition;
			_hasImageMaxSize = _imageMaxSize.x > 0f && _imageMaxSize.y > 0f;
		}

		private void FitImageToBounds(Sprite sprite)
		{
			if (!_hasImageMaxSize || _imageRectTransform == null || sprite == null)
			{
				return;
			}

			float spriteWidth = sprite.rect.width;
			float spriteHeight = sprite.rect.height;
			if (spriteWidth <= 0f || spriteHeight <= 0f)
			{
				return;
			}

			float widthScale = _imageMaxSize.x / spriteWidth;
			float heightScale = _imageMaxSize.y / spriteHeight;
			float scale = Mathf.Min(widthScale, heightScale);

			_imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, spriteWidth * scale);
			_imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spriteHeight * scale);
			_imageRectTransform.anchoredPosition = _imageAnchoredPosition;
		}

		private System.Collections.IEnumerator TypewriterRoutine(string text)
		{
			_isTyping = true;
			_dialogText.text = "";
			if (string.IsNullOrEmpty(text))
			{
				_isTyping = false;
				yield break;
			}

			var wait = (_secondsPerChar > 0f) ? new WaitForSecondsRealtime(_secondsPerChar) : null;
			for (int i = 0; i < text.Length; i++)
			{
				_dialogText.text += text[i];
				if (wait != null)
				{
					yield return wait;
				}
				else
				{
					yield return null;
				}
			}
			_isTyping = false;
		}

		private void CompleteTypewriter()
		{
			if (!_isTyping)
			{
				return;
			}
			if (_typewriterCoroutine != null)
			{
				StopCoroutine(_typewriterCoroutine);
				_typewriterCoroutine = null;
			}
			_dialogText.text = _fullText;
			_isTyping = false;
		}
	}
}
