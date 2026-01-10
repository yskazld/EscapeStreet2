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
		/// 連続実行を避けるためのガード
		/// </summary>
		[SerializeField] private bool _playOnlyOnce = true;
		private bool _hasPlayed = false;
		private bool _isPlaying = false;

		/// <summary>
		/// 条件が揃ったら自動起動
		/// </summary>
		[SerializeField] ObjectConditionBase[] _autoPlayConsitionList;

		public override void UpdateObject()
		{
			base.UpdateObject();
			if (_isPlaying)
			{
				return;
			}
			if (_playOnlyOnce && _hasPlayed)
			{
				return;
			}
			//クリックしなくても起動する条件があるなら
			if (IsCondition(_autoPlayConsitionList))
			{
				_isPlaying = true;
				PlayAction();
				_hasPlayed = true;
				_isPlaying = false;
			}
		}
	}
}
