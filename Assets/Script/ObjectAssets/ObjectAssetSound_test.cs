using UnityEngine;

namespace ObjectAssets
{
	/// <summary>
	/// 音を鳴らす
	/// </summary>
	[CreateAssetMenu(fileName = "音を鳴らす.asset", menuName = "Escape/Object/Sound_test")]
	[System.Serializable]
	public class ObjectAssetSound_test : ObjectAssetBase
	{
        public SoundManager.SOUND_TYPE Sound_test;
	}
}
