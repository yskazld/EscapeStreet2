using UnityEngine;

namespace ObjectAssets
{
	/// <summary>
	/// フラグを代入するオブジェクト
	/// </summary>
	[CreateAssetMenu(fileName = "フラグ.asset", menuName = "Escape/Object/Flag")]
	[System.Serializable]
	public class ObjectAssetFlagControl : ObjectAssetBase
	{
		public Save.SaveData.SaveFlag[] FlagKind;
		public int SetNum;
	}
}
