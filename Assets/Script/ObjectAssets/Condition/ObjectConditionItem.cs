using UnityEngine;
using Save;

namespace ObjectAssets.Condition
{
	/// <summary>
	/// アイテム所持の条件データ
	/// </summary>
	[CreateAssetMenu(fileName = "アイテム条件.asset", menuName = "Escape/Condition/Item")]
	[System.Serializable]
	public class ObjectConditionItem : ObjectConditionBase
	{
		/// <summary>
		/// アイテム種類
		/// </summary>
		public SaveData.ItemKind ItemKind;
	}
}
