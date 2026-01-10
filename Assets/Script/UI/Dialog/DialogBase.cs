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

		/// <summary>
		/// 表示する画像
		/// </summary>
		[SerializeField] protected Image _image;

		public Action OnClose;

		private void Start()
		{
			if (_oneSelectButton != null)
			{
				_oneSelectButton.onClick.AddListener(() =>
				{
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
			_dialogText.text = text;
		}

		/// <summary>
		/// 消したときの処理
		/// </summary>
		public void Close()
		{
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
			//リンク先の画像ファイルを見る
			var sprite = Resources.Load<Sprite>(link);
			if (sprite != null)
			{
				//画像が存在するなら表示
				_image.sprite = sprite;
				_image.gameObject.SetActive(true);
			}
			else
			{
				//ないなら消す
				_image.gameObject.SetActive(false);
			}
		}
	}
}
