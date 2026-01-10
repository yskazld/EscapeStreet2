using UnityEngine;
using Save;

namespace ObjectAssets.Condition
{
	/// <summary>
	/// フラグの条件データ
	/// </summary>
	[CreateAssetMenu(fileName = "ConditionFlag.asset", menuName = "Escape/Condition/Flag")]
	[System.Serializable]
	public class ObjectConditionFlag : ObjectConditionBase
	{
		/// <summary>
		/// フラグ種類
		/// </summary>
		public SaveData.SaveFlag FlagKind;
		/// <summary>
		/// = > < 
		/// </summary>
		public COMPARISON Comparison;
		/// <summary>
		/// 比較する値
		/// </summary>
		public int Num;
	}
}
