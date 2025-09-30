using UnityEngine;
using Save;

namespace ObjectAssets.Condition
{
	/// <summary>
	/// アイテム選択の条件データ
	/// UIでアイテムを選択しているかを比較する
	/// </summary>
	[CreateAssetMenu(fileName = "アイテム選択.asset", menuName = "Escape/Condition/ItemSelect")]
	[System.Serializable]
	public class ObjectConditionItemSelect : ObjectConditionBase
	{
		/// <summary>
		/// アイテム種類
		/// </summary>
		public SaveData.ItemKind ItemKind;
	}
}
