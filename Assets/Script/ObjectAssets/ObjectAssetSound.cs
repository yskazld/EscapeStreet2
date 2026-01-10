using UnityEngine;

namespace ObjectAssets
{
	/// <summary>
	/// 音を鳴らす
	/// </summary>
	[CreateAssetMenu(fileName = "音を鳴らす.asset", menuName = "Escape/Object/Sound")]
	[System.Serializable]
	public class ObjectAssetSound : ObjectAssetBase
	{
		public SoundManager.SOUND_TYPE Sound;
	}
}
