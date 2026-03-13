using UnityEngine;
using System;

/// <summary>
/// 音を鳴らすクラス
/// 例
/// SoundManager.GetInstance().Play(SOUND_TYPE.Decision);
/// </summary>
public class SoundManager : MonoBehaviour
{
	[SerializeField] AudioSource[] audioSources;

	private int _audioIndex = 0;
	private AudioClip[] _audioClips;
	private static SoundManager _main;

    public enum SOUND_TYPE
    {
        /// 決定
        Decision,

        /// ブザー
        Buzzer,

        /// 選択
        Select,

        /// コイン
        Coin,
        /// 部屋移動(左右)
        MoveRoom,
        /// 部屋移動(Back)
        MoveRoomBack,
        /// ステージに入る
        EnterRoom,
        /// ステージに入る(ステージ１２)
        EnterRoomWalk,
        /// ボタンを押した時の音
        PushButton,
        /// ギミッククリア時にドアが開く音
        OpenDoor,
        /// 棚の扉が開く音
        OpenTana,
        /// アイテム入手時
        GetItem,
        /// 鍵を開けた時
        UnlockKey,
        ///　レジを開けた時
        OpenRegister,
        ///　カーテンが開く音
        OpenCurtain,
        ///　アイテム出現音
        AppearItem,
        ///　ロボットの目を装備
        RobotEyeEquip,
        ///　アニメーション色変え
        DollColorChange,
        ///　ダイヤルを回す音
        TurnDial,
        ///　電池を入れる音
        BatteryON,
        ///　箱を開ける音
        OpenBox, 
        ///　はさみで着る音
        UseSissors,







        Num,

    }
	public static SoundManager GetInstance()
	{
		return _main;
	}

	void Start()
	{
		_main = this;
		_audioClips = new AudioClip[(int)SOUND_TYPE.Num];
		for (int f = 0; f < (int)SOUND_TYPE.Num; f++)
		{
			SOUND_TYPE Type = (SOUND_TYPE)Enum.Parse(typeof(SOUND_TYPE), f.ToString());
			string typename = "Sound/" + Type.ToString();
			_audioClips[f] = (AudioClip)Resources.Load(typename);
		}
	}

	/// <summary>
	/// 音を鳴らす
	/// </summary>
	/// <param name="sound">音のタイプ</param>
	/// <param name="pitch">音の速度</param>
	/// <param name="volume">音の大きさ</param>
	public void Play(SOUND_TYPE sound, float pitch = 1.0f, float volume = 0.4f)
	{
		bool ok = false;
		try
		{
			for (int i = 0; i < _audioClips.Length; i++)
			{
				if (_audioClips[i].name == sound.ToString())
				{
					audioSources[_audioIndex].clip = _audioClips[i];

					audioSources[_audioIndex].volume = volume;
					audioSources[_audioIndex].pitch = pitch;

					audioSources[_audioIndex].Play();
					_audioIndex++;
					if (_audioIndex >= audioSources.Length)
					{
						_audioIndex = 0;
					}
					ok = true;
					break;
				}
			}
		}
		catch
		{
			Debug.LogError("Sound Play Errorr!!!" + sound.ToString());
		}

		if (ok == false)
		{
			Debug.LogError("NOTSOUND!!!" + sound.ToString());
		}
	}

	/// <summary>
	/// 音を止める
	/// </summary>
	public void AllStop()
	{
		for (int i = 0; i < audioSources.Length; i++)
		{
			audioSources[i].Stop();
		}
	}

	/// <summary>
	/// 指定した音を止める
	/// </summary>
	public void Stop(SOUND_TYPE sound)
	{
		for (int i = 0; i < audioSources.Length; i++)
		{
			var clip = audioSources[i].clip;
			if (clip != null && clip.name == sound.ToString())
			{
				audioSources[i].Stop();
			}
		}
	}
}
