using UnityEngine;

namespace ObjectAssets
{
	/// <summary>
	/// アイテムを取得させるオブジェクト
	/// </summary>
	[CreateAssetMenu(fileName = "アイテム.asset", menuName = "Escape/Object/Item")]
	[System.Serializable]

	public class ObjectAssetItem : ObjectAssetBase
	{
		/// <summary>
		/// 手に入れるアイテムID
		/// </summary>
		public Save.SaveData.ItemKind ItemKind;
		/// <summary>
		/// 手に入れるか失うか
		/// </summary>
		public bool GetOrLose;
	}
}