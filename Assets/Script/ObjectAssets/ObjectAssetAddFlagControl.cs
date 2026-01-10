using UnityEngine;

namespace ObjectAssets
{
	/// <summary>
	/// フラグを加算させるオブジェクト
	/// </summary>
	[CreateAssetMenu(fileName = "フラグ増減.asset", menuName = "Escape/Object/AddFlag")]
	[System.Serializable]
	public class ObjectAssetAddFlagControl : ObjectAssetBase
	{
		public Save.SaveData.SaveFlag[] FlagKind;
		public int AddNum;
	}
}
