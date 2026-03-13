using System.Collections.Generic;

namespace Save
{
	/// <summary>
	/// セーブデータ バイナリ化するので[System.Serializable]をつけないといけない
	/// </summary>
	[System.Serializable]
	public class SaveData
	{
		public const int MAX_FLAG = 1000;
        /// <summary>
        /// フラグ
        /// 新規フラグを用意する場合は末尾から追加を推奨します
        /// (すでに作成したScriptableObjectの値がずれるので)
        /// </summary>
        public enum SaveFlag
        {
            LIGHT_1,
            LIGHT_2,
            ROOM_1_CLEAR,
            KEY_GET,
            ROOM_2_CLEAR,
            KEY_GET2,
            ADD_TEST,
            STAGE3_PASS1,
            STAGE3_PASS2,
            STAGE3_PASS3,
            STAGE3_PASS4,
            ROOM_2_1_CLEAR,
            HAMMER_1_GET,
            HAMMER_2_GET,
            TEST_KEY,
            STAGE_1_STEP,
            STAGE_1_CLEAR,
            STAGE_2_CLEAR,
            STAGE_2_SLOT_1,
            STAGE_2_SLOT_2,
            STAGE_2_SLOT_3,
            STAGE_2_SLOT_4,
            STAGE_3_CLEAR,
            STAGE_4_CLEAR,
            STAGE_5_CLEAR,
            STAGE_6_CLEAR,
            STAGE_7_CLEAR,
            STAGE_8_CLEAR,
            STAGE_9_CLEAR,
            STAGE_10_CLEAR,
            STAGE_11_CLEAR,
            STAGE_12_CLEAR,
            STAGE_13_CLEAR,
            STAGE_14_CLEAR,
            STAGE_15_CLEAR,
            STAGE_16_CLEAR,
            STAGE_17_CLEAR,
            STAGE_18_CLEAR,
            STAGE_19_CLEAR,
            STAGE_20_CLEAR,
            STAGE_21_CLEAR,
            STAGE_4_SLOT_1,
            STAGE_4_SLOT_2,
            STAGE_4_SLOT_3,
            STAGE5_COLOR1,
            STAGE5_COLOR2,
            STAGE5_COLOR3,
            STAGE5_COLOR4,
            STAGE_6_SLOT_1,
            STAGE_6_SLOT_2,
            STAGE_6_SLOT_3,
            STAGE_6_SLOT_4,
            STAGE_7_SLOT_1,
            STAGE_7_SLOT_2,
            STAGE_7_SLOT_3,
            STAGE_7_SLOT_4,
            STAGE_8_SLOT_1,
            STAGE_8_SLOT_2,
            STAGE_8_SLOT_3,
            STAGE_8_SLOT_4,
            STAGE_8_SLOT_5,
            STAGE_8_SLOT_6,
            COIN8,
            COIN8_TOUCH,
            KEY9,
            KEY9_TOUCH,
            STAGE9_PASS1,
            STAGE9_PASS2,
            STAGE9_PASS3,
            STAGE9_PASS4,
            STAGE9_PASS5,
            STAGE9_BOX_OPEN,
            STAGE10_ONTANSU,
            STAGE_10_SLOT_1,
            STAGE_10_SLOT_2,
            STAGE_10_SLOT_3,
            STAGE_10_SLOT_4,
            STAGE_10_SLOT_5,
            STAGE10_ON_MIRROR,
            STAGE10_PASS1,
            STAGE10_PASS2,
            STAGE10_PASS3,
            STAGE10_PASS4,
            STAGE11_ON_BATTERY1,
            STAGE11_ON_KINKO1,
            STAGE_11_SLOT_1_round,
            STAGE_11_SLOT_2_round,
            STAGE_11_SLOT_3_round,
            STAGE11_ON_Tana,
            STAGE11_ON_BATTERY2,
            STAGE11_PASS1,
            STAGE11_PASS2,
            STAGE11_PASS3,
            STAGE11_PASS4,
            STAGE11_ON_BATIN1,
            STAGE11_ON_BATIN2,
            STAGE12_BOX_OPEN,
            STAGE_12_SLOT_1,
            STAGE_12_SLOT_2,
            STAGE_12_SLOT_3,
            STAGE_12_SLOT_4,
            STAGE_12_SLOT_5,
            STAGE_12_SLOT_6,

            STAGE_12KAGO_SLOT_1,
            STAGE_12KAGO_SLOT_2,
            STAGE_12KAGO_SLOT_3,
            STAGE_12KAGO_SLOT_4,
            STAGE12_KAGO_OPEN,
            SCISSOR_12_ON,
            STAGE13_ON_PUZREDIN,
            STAGE13_ON_PUZGREENIN,
            STAGE13_ON_PUZRED,
            STAGE13_ON_PUZGREEN,
            STAGE13_ON_BOX1,
            STAGE13_ON_BOX2,
            STAGE13_SEQ_STEP,
            STAGE13_NEXT_COLOR,
            STAGE_13BOX2_SLOT_1,
            STAGE_13BOX2_SLOT_2,
            STAGE_13BOX2_SLOT_3,
            STAGE_13BOX2_SLOT_4,
            //追加
            STAGE_1_SLOT_1,
            STAGE_1_SLOT_2,
            STAGE_1_SLOT_3,
            STAGE_1_SLOT_4,
            STAGE_4_Tana,
            STAGE4_ON_KEY,
            STAGE_4_SLOT_4,
            STAGE5_COLOR5,
            STAGE5_COLOR6,
            STAGE5_COLOR7,
            STAGE5_COLOR8,
            STAGE5_COLOR9,
            STAGE5_ALFABET1,
            STAGE5_ALFABET2,
            STAGE5_ALFABET3,
            STAGE5_ALFABET4,
            STAGE5_ALFABET5,
            STAGE5_ALFABET6,
            STAGE_5_Door, 
            STAGE_6_CURTAIN,
            STAGE_6_SLOT_5,
            STAGE_6_NUMBUT_1,
            STAGE_6_NUMBUT_2,
            STAGE_6_NUMBUT_3,
            STAGE_6_NUMBUT_4,
            STAGE_6_NUMBUT_5,
            STAGE7_SEQ_STEP,
            STAGE7_NEXT_COLOR,
            STAGE7_ON_EYE_ROBOT1,
            STAGE7_ON_EYE_ROBOT2, 
            STAGE7_APPEAR_EYE_ROBOT1,
            STAGE7_APPEAR_EYE_ROBOT2,
            STAGE_7_SLOT_5,
            STAGE7_ON_EYE1_IN,
            STAGE7_ON_EYE2_IN,
            STAGE8_APPEAR_COIN, 
            STAGE_8_SLOT_1_Doll,
            STAGE_8_SLOT_2_Doll,
            STAGE_8_SLOT_3_Doll,
            STAGE_8_SLOT_4_Doll,
            STAGE_8_SLOT_5_Doll,
            STAGE_8_SLOT_6_Doll,
            STAGE_8_SLOT_7_Doll,
            STAGE9_PASS6,
            STAGE9_DOORKNOB,
            STAGE9_ON_TANA_OPEN,
            STAGE9_APPEAR_KEY,
            STAGE9_SLOT1_BOOTS,
            STAGE9_SLOT2_BOOTS,
            STAGE9_SLOT3_BOOTS,
            STAGE9_SLOT4_BOOTS,
            STAGE9_SLOT5_BOOTS,

            STAGE10_SLOT1_FASHION,
            STAGE10_SLOT2_FASHION,
            STAGE10_SLOT3_FASHION,
            STAGE10_SLOT4_FASHION,
            STAGE10_ON_HIKIDASHI,

            STAGE10_SLOT1_ALFABET,
            STAGE10_SLOT2_ALFABET,
            STAGE10_SLOT3_ALFABET,
            STAGE10_SLOT4_ALFABET,
            STAGE10_SLOT5_ALFABET,
            STAGE10_SLOT6_ALFABET,
            STAGE10_ON_CURTAIN,
            STAGE11_ON_ARROW1, 
            STAGE11_ON_ARROW2,
            STAGE11_ON_ARROW3,
            STAGE11_ON_ARROW4,
            STAGE12_ON_FOUNTAIN,
            STAGE12_ON_ARROW1,
            STAGE12_ON_ARROW2,
            STAGE12_ON_ARROW3,
            STAGE12_ON_ARROW4,
            STAGE12_ON_ARROW5,
            STAGE13_SLOT1_TRUMP,
            STAGE13_SLOT2_TRUMP,
            STAGE13_SLOT3_TRUMP,
            STAGE13_SLOT4_TRUMP,
            STAGE13_ON_DOLL,
            STAGE13_SLOT1_TUTA,
            STAGE13_SLOT2_TUTA,
            STAGE13_SLOT3_TUTA,
            STAGE13_SLOT4_TUTA,
            STAGE13_ON_TUTA_TOWEL,
            STAGE4_ON_4_KEY,

            INTRO_SEQUENCE_DONE,
            RETURNING_DIALOG_SHOWN,
            STAGE4_KEY4_TUTORIAL_SHOWN,
            ENDING_SEQUENCE_DONE,

            STAGE11_ON_CURTAIN,


        }

        /// <summary>
        /// アイテムの種類
        /// フラグ同様に注意する
        /// </summary>
        public enum ItemKind
        {
            NONE,
            KEY_1,
            KEY_2,
            KEY_3,
            KEY_4,
            KEY_5,
            HAMMER_1,
            HAMMER_2,
            HAMMER_UNION,
            TEST_KEY,
            COIN8,
            KEY9,
            BATTERY11_1,
            BATTERY11_2,
            SCISSOR_12,
            PUZRED13,
            PUZGREEN13,
            STAGE7_ON_EYE_ROBOT1,
            STAGE7_ON_EYE_ROBOT2,
            DOORKNOB9,
            STAGE_4_KEY,





        }

        /// <summary>
        /// 言語種類
        /// </summary>
        public enum LANGUAGE
		{
			JAPAN,
			ENGLISH
		}

		/// <summary>
		/// フラグ格納
		/// </summary>
		private int[] _saveFlagDataList;
		
		/// <summary>
		/// ヒントを見たフラグ格納
		/// </summary>
		private bool[] _usedHintList;
		
		/// <summary>
		/// アイテムの所持状況
		/// </summary>
		private List<ItemKind> _itemDataList;

		/// <summary>
		/// 今いるルームのID
		/// </summary>
		private int _nowRoom;

		/// <summary>
		/// 現在の言語
		/// </summary>
		private LANGUAGE _language;

		/// <summary>
		/// 初期化フラグ　メインゲームに行くとオンになる
		/// </summary>
		public bool IsFirst { get; private set; } = true;
		
		/// <summary>
		/// 初期化
		/// 初回起動時とセーブデータクリアで呼ぶ
		/// </summary>
		public void Init()
		{
			_saveFlagDataList = new int[MAX_FLAG];
			_usedHintList = new bool[MAX_FLAG];
			_itemDataList = new List<ItemKind>();
			_nowRoom = 0;
			IsFirst = true;
		}

		public void FirstOff()
		{
			IsFirst = false;
		}

		public int GetFlagNum(SaveFlag flag)
		{
			return _saveFlagDataList[(int)flag];
		}

		public void SetFlagNum(SaveFlag flag, int value)
		{
			_saveFlagDataList[(int)flag] = value;
		}

		/// <summary>
		/// ヒントを見たフラグをオン
		/// </summary>
		/// <param name="flag"></param>
		public void SetUsedHint(SaveFlag flag)
		{
			_usedHintList[(int)flag] = true;
		}

		/// <summary>
		/// ヒントを見たフラグを取得
		/// </summary>
		/// <param name="flag"></param>
		/// <returns></returns>
		public bool GetUsedHint(SaveFlag flag)
		{
			return _usedHintList[(int)flag];
		}

		public void AddFlagNum(SaveFlag flag, int value)
		{
			_saveFlagDataList[(int)flag] += value;
		}

		public List<ItemKind> GetItemNum()
		{
			return _itemDataList;
		}

		public void AddItemNum(ItemKind flag)
		{
			_itemDataList.Add(flag);
		}

		
		public void RemoveItemNum(ItemKind flag)
		{
			_itemDataList.Remove(flag);
		}
		
		public int GetNowRoom()
		{
			return _nowRoom;
		}
		public void SetNowRoom(int room)
		{
			_nowRoom = room;
		}

		public void SetLanguage(LANGUAGE language)
		{
			_language = language;
		}

		public LANGUAGE GetLanguage()
		{
			return _language;
		}

		public void SetNowRoom(LANGUAGE language)
		{
			_language = language;
		}
	}
}
