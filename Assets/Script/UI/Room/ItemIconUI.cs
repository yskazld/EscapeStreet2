using System;
using Save;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Room
{
	/// <summary>
	/// アイテムの表示UI
	/// </summary>
	public class ItemIconUI : MonoBehaviour
	{
		[SerializeField] private Button _button;
		[SerializeField] private GameObject _itemIcon;
		[SerializeField] private Image _image;

		/// <summary>
		/// 選択パネル
		/// </summary>
		[SerializeField] private GameObject _selectObject;

		/// <summary>
		/// アイテムの種類
		/// </summary>
		private SaveData.ItemKind _kind = SaveData.ItemKind.NONE;

		public Action<int> OnPush;

		public void Init(int index)
		{
			_button.onClick.AddListener(() => { OnPush(index); });
		}

		private void Start()
		{
			_selectObject.SetActive(false);
		}

		/// <summary>
		/// アイテム表示更新
		/// アイテムの絵を変えたい場合はここにIDなどを受け取るようにして
		/// imageの更新をするとよさげ
		/// </summary>
		/// <param name="isActive"></param>
		public void SettingItemIcon(bool isActive)
		{
			_itemIcon.gameObject.SetActive(isActive);
		}

		/// <summary>
		/// アイテム画像を切り替える
		/// </summary>
		/// <param name="kind"></param>
		public void SettingSprite(Save.SaveData.ItemKind kind)
		{
			//前と違う種類の時だけ切り替える
			if (_kind != kind && kind != SaveData.ItemKind.NONE)
			{
				var texture = Resources.Load(kind.ToString()) as Texture2D;
				_image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
			}
			_kind = kind;
		}

		/// <summary>
		/// 選択中かどうかを切り替える
		/// </summary>
		/// <param name="isActive"></param>
		public void SetSelectActiveView(bool isActive)
		{
			_selectObject.gameObject.SetActive(isActive);
		}

		/// <summary>
		/// アイテムの種類を取得
		/// </summary>
		/// <returns></returns>
		public SaveData.ItemKind GetItem()
		{
			return _kind;
		}
	}
}
