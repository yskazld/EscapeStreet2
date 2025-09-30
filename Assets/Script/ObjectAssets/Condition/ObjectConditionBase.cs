using UnityEngine;
namespace ObjectAssets.Condition
{
	/// <summary>
	/// オブジェクト条件の基底クラス。
	/// このクラスを継承することで、ObjectConditionBaseで検索できるようにする。
	/// </summary>
	public class ObjectConditionBase : ScriptableObject
	{
		/// <summary>
		/// 比較方法
		/// </summary>
		public enum COMPARISON
		{
			/// <summary>
			/// 等しい
			/// </summary>
			EQUAL,
			/// <summary>
			/// より上
			/// </summary>
			UPPER,
			/// <summary>
			/// より下
			/// </summary>
			UNDER,
		}
	}
}
