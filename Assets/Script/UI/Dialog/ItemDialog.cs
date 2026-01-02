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
		private static readonly System.Collections.Generic.Dictionary<Save.SaveData.ItemKind, string> SpriteResourceOverrides =
			new System.Collections.Generic.Dictionary<Save.SaveData.ItemKind, string>
			{
				{ Save.SaveData.ItemKind.DOORKNOB9, "Sprite/9_Doorknob" },
				{ Save.SaveData.ItemKind.KEY9, "Sprite/KEY_9" },
			};

		/// <summary>
		/// アイテム画像の設定をする
		/// </summary>
		/// <param name="kind"></param>
		public void SettingItem(Save.SaveData.ItemKind kind)
		{
			//Textureをロード
			var texture = Resources.Load(kind.ToString()) as Texture2D;
			if (texture == null && SpriteResourceOverrides.TryGetValue(kind, out var overridePath))
			{
				texture = Resources.Load(overridePath) as Texture2D;
			}
			if (texture == null)
			{
				return;
			}
			//spriteに入れる
			_image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
		}

	}
}
