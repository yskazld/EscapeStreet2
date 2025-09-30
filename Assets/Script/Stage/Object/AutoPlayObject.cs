using ObjectAssets.Condition;
using UnityEngine;

namespace Stage.Object
{
	/// <summary>
	/// 条件が揃ったら起動する
	/// サンプルだと　Room1ClearCheck
	/// で明かりが２個ついてるときにフラグを変える処理で使用
	/// </summary>
	public class AutoPlayObject : ObjectBase
	{
		/// <summary>
		/// 条件が揃ったら自動起動
		/// </summary>
		[SerializeField] ObjectConditionBase[] _autoPlayConsitionList;

		public override void UpdateObject()
		{
			base.UpdateObject();
			//クリックしなくても起動する条件があるなら
			if (IsCondition(_autoPlayConsitionList))
			{
				PlayAction();
			}
		}
	}
}
