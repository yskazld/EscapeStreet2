using ObjectAssets.Condition;
using UnityEngine;

namespace Stage.Object
{
	/// <summary>
	/// 部屋に入ったら時に起動指せるオブジェクト
	/// 部屋に入ったらギミックリセットしたい時などに使う
	/// </summary>
	public class RoomEnterPlayObject : ObjectBase
	{
		[SerializeField] ObjectConditionBase[] _autoPlayConsitionList;

		public override void EnterRoomToUpdate()
		{
			base.EnterRoomToUpdate();
			//クリックしなくても起動する条件があるなら
			if (IsCondition(_autoPlayConsitionList))
			{
				PlayAction();
			}
		}
	}
}
