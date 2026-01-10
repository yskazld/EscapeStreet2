using UnityEngine;

namespace Save
{
	/// <summary>
	/// セーブロードを管理
	/// </summary>
	public class SaveManager
	{
		public const string FileName = "EscapeRoom";

		public SaveData SaveDataInstance { get; private set; }

		/// <summary>
		/// 自動で初期化かロードか選択
		/// 初期化したかを返す
		/// </summary>
		public bool LoadOrInitializeReturnInitialize()
		{			
			if (SerializeConverter.Exist(GetNowSaveKey()))
			{
				try
				{
					SaveDataInstance = SerializeConverter.Load<SaveData>(GetNowSaveKey());
				}
				catch
				{
					Debug.LogError("読み込み失敗");
					//失敗初期化
					InitializeSaveData();
					return true;
				}
			}
			else
			{
				InitializeSaveData();
				return true;
			}
			return false;
		}

		/// <summary>
		/// セーブする
		/// </summary>
		public void Save()
		{
			var saveBinary = SerializeConverter.ConvertClassToBinaryData(SaveDataInstance);
			PlayerPrefs.SetString(GetNowSaveKey(), saveBinary);
			// PlayerPrefs は即座に書き出さないため強制保存する
			PlayerPrefs.Save();
		}

		/// <summary>
		/// 初期化
		/// </summary>
		public void InitializeSaveData()
		{
			SaveDataInstance = new SaveData();
			SaveDataInstance.Init();
		}

		/// <summary>
		/// セーブ用のキー
		/// </summary>
		/// <returns></returns>
		private string GetNowSaveKey()
		{
			//セーブデータを分けたりするなら0以降の数字を入れる
			var msg = FileName + "0";
			return msg;
		}
	}
}
