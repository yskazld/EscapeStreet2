using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Dialog
{
	/// <summary>
	/// ダイアログ一個単位の挙動
	/// </summary>
	public class ItemDialog : DialogBase
	{
		/// <summary>
		/// アイテム画像の設定をする
		/// </summary>
		/// <param name="kind"></param>
		public void SettingItem(Save.SaveData.ItemKind kind)
		{
			//Textureをロード
			var texture = Resources.Load(kind.ToString()) as Texture2D;
			//spriteに入れる
			_image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
		}

	}
}
