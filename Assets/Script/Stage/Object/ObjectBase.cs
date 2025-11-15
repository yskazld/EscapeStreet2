using ObjectAssets;
using ObjectAssets.Condition;
using UnityEngine;
using Save;
using System;
using UI.Room;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Stage.Object
{
	/// <summary>
	/// ルーム内に配置されるオブジェクト一個単位の処理
	/// 明かりをつけるスイッチのフラグ操作や
	/// 落ちてるアイテムの取得など
	/// </summary>
	public class ObjectBase : MonoBehaviour
	{
		/// <summary>
		/// 使用しない　コメント
		/// </summary>
		[Multiline(5)]		
		[SerializeField] private string _comment;
		/// <summary>
		/// クリックによる起動条件
		/// 見た目のオンオフにも使用
		/// </summary>
		[FormerlySerializedAs("_clickConsitionList")] [SerializeField] ObjectConditionBase[] _clicktConditionList;
		/// <summary>
		/// 条件を満たさなくなっても見た目を残すか
		/// </summary>
		[SerializeField] private bool _keepVisibleWhenDisabled = false;

		/// <summary>
		/// 起動内容
		/// </summary>
		[SerializeField] ObjectAssetBase[] _objectDataList;

		/// <summary>
		/// オブジェクト操作でフラグ変更したとき
		/// </summary>
		public Action<SaveData.SaveFlag, int> OnFlagChange;

		
		/// <summary>
		/// オブジェクト操作でフラグ変更したとき
		/// </summary>
		public Action<SaveData.SaveFlag, int> OnAddFlagChange;
		
		/// <summary>
		/// オブジェクト操作で入室したとき
		/// </summary>
		public Action<int> OnEnterRoom;

		/// <summary>
		/// クリックした
		/// </summary>
		public Action OnClick;
		
		/// <summary>
		/// オブジェクト操作でアイテムに更新があった場合
		/// </summary>
		public Action<SaveData.ItemKind, bool> OnUpdateItem;		

		private SaveData _saveDataInstance;

		/// <summary>
		/// 表示用オブジェクト一覧
		/// ObjectBase実装の際はからのオブジェクトに入れてからその子オブジェクトに
		/// 表示用オブジェクトを置く
		/// </summary>
		private Transform[] _viewObjects;
		private bool _canInteractByCondition = true;
		private bool _isTouchEnabled = true;
		private bool _didOverrideButtonColor = false;

		public int RoomID { get; private set; } = 0;

		private Button _button;

		/// <summary>
		/// 初期化 Startなどは使わずに必ずここで初期化
		/// </summary>
		/// <param name="roomID"></param>
		public void Init(int roomID)
		{
			RoomID = roomID;
			_saveDataInstance = GameManager.GetInstance().SaveManagerInstance.SaveDataInstance;
			//子オブジェクトをすべて取得
			_viewObjects = transform.GetComponentsInChildren<Transform>();
			if (!_keepVisibleWhenDisabled)
			{
				foreach (var objectData in _objectDataList)
				{
					if (objectData is ObjectAssetAddFlagControl addFlag)
					{
						foreach (var flag in addFlag.FlagKind)
						{
							if (flag == SaveData.SaveFlag.STAGE_1_STEP)
							{
								_keepVisibleWhenDisabled = true;
								break;
							}
						}
						if (_keepVisibleWhenDisabled)
						{
							break;
						}
					}
				}
			}
			var button = GetComponent<Button>();
			if (button != null)
			{
				_button = button;
				if (_keepVisibleWhenDisabled)
				{
					OverrideButtonDisabledColor();
				}
			}
			
		if (_button != null)
		{
			_button.onClick.AddListener(() =>
			{
				OnClick?.Invoke();
			});
		}
	}

	private void OverrideButtonDisabledColor()
	{
		if (_button == null || _didOverrideButtonColor)
		{
			return;
		}
		var colors = _button.colors;
		colors.disabledColor = new Color(colors.normalColor.r, colors.normalColor.g, colors.normalColor.b, colors.normalColor.a);
		_button.colors = colors;
		_didOverrideButtonColor = true;
	}

		/// <summary>
		/// 表示非表示更新 自動起動の確認
		/// オブジェクト自体もactive false になるのでクリックできないようにもする
		/// </summary>
		public virtual void UpdateObject()
		{
			//見た目のオンオフ
			bool canInteract = IsCondition(_clicktConditionList);
			_canInteractByCondition = canInteract;
			bool shouldDisplay = canInteract || _keepVisibleWhenDisabled;
			foreach (var objectData in _viewObjects)
			{
				objectData.gameObject.SetActive(shouldDisplay);
			}
			if (_button != null)
			{
				_button.interactable = _isTouchEnabled && canInteract;
			}
		}

		public virtual void EnterRoomToUpdate()
		{
			
		}
		
		/// <summary>
		/// クリック時の挙動
		/// </summary>
		/// <returns>何かしらのアクションがあったらtrue</returns>
		public bool ClickAction()
		{
			bool canClick = IsCondition(_clicktConditionList);
			var gameManager = GameManager.GetInstance();
			if (gameManager != null)
			{
				var saveData = gameManager.SaveManagerInstance.SaveDataInstance;
				var step = saveData.GetFlagNum(SaveData.SaveFlag.STAGE_1_STEP);
				var clear = saveData.GetFlagNum(SaveData.SaveFlag.STAGE_1_CLEAR);
				Debug.Log($"[Stage1 Sequence] Click attempt {name} (RoomID={RoomID}). canClick={canClick} STEP={step} CLEAR={clear}");
			}
			if (canClick)
			{
				PlayAction();
				return true;
			}
			return false;
		}

		/// <summary>
		/// クリックできるかできないかを切り替える
		/// </summary>
		/// <param name="isActive"></param>
		public void SettingTouch(bool isActive)
		{
			_isTouchEnabled = isActive;
			if (_button != null)
			{
				_button.interactable = _isTouchEnabled && _canInteractByCondition;
			}
		}

		/// <summary>
		/// オブジェクト登録内容を起動
		/// </summary>
		protected void PlayAction()
		{
            //起動内容実行
            foreach (var objectData in _objectDataList)
            {
                if (objectData is ObjectAssetFlagControl)
                {
                    //フラグを設定
                    var flagData = objectData as ObjectAssetFlagControl;
                    foreach (var flag in flagData.FlagKind)
                    {
                        //Debug.Log("フラグを設定 " + flag + " " + flagData.SetNum);
                        OnFlagChange?.Invoke(flag, flagData.SetNum);
                    }
                }
                else if (objectData is ObjectAssetAddFlagControl)
                {
                    //フラグを増加設定
                    var flagData = objectData as ObjectAssetAddFlagControl;
                    foreach (var flag in flagData.FlagKind)
                    {
                        Debug.Log("フラグを増加 " + flag + " " + flagData.AddNum);
                        OnAddFlagChange?.Invoke(flag, flagData.AddNum);
                    }
                }
                else if (objectData is ObjectAssetEnterRoom)
                {
                    //部屋移動
                    var roomData = objectData as ObjectAssetEnterRoom;
                    Debug.Log("部屋移動 " + roomData.RoomID);
                    OnEnterRoom?.Invoke(roomData.RoomID);
                }
                else if (objectData is ObjectAssetItem)
                {
                    //アイテム取得
                    var itemData = objectData as ObjectAssetItem;
                    Debug.Log("アイテム取得 " + itemData.ItemKind + " " + itemData.GetOrLose);
                    OnUpdateItem?.Invoke(itemData.ItemKind, itemData.GetOrLose);
                }
                else if (objectData is ObjectAssetGoTitle)
                {
                    SceneManager.LoadScene("title");
                }
                else if (objectData is ObjectAssetSound)
                {
                    //サウンドを鳴らす
                    var sound = objectData as ObjectAssetSound;
                    SoundManager.GetInstance().Play(sound.Sound);
                }
                else if (objectData is ObjectAssetSound_test)
                {
                    //サウンドを鳴らす
                    var flagData = objectData as ObjectAssetSound_test;
                    // SoundManager.GetInstance().Play(sound.Sound_test);
                    SoundManager.GetInstance().Play(flagData.Sound_test);
                }
            }
		}

		/// <summary>
		/// 条件に合ってるか
		/// 配列内のすべての条件があってるならtrue
		/// </summary>
		/// <returns></returns>
		protected bool IsCondition(ObjectConditionBase[] conditionList)
		{
			foreach (var conditionData in conditionList)
			{
				bool isOk = false;
				//フラグを検査
				if (conditionData is ObjectConditionFlag)
				{
					var conditionFlag = conditionData as ObjectConditionFlag;
					var comparison = conditionFlag.Comparison;
					var flagNum = _saveDataInstance.GetFlagNum(conditionFlag.FlagKind);
					var comparisonNum = conditionFlag.Num;
					//Debug.Log("フラグを判定 " +conditionFlag.FlagKind);
					switch (comparison)
					{
						case ObjectConditionBase.COMPARISON.EQUAL:
							isOk = flagNum == comparisonNum;
							break;
						case ObjectConditionBase.COMPARISON.UNDER:
							isOk = flagNum < comparisonNum;
							break;
						case ObjectConditionBase.COMPARISON.UPPER:
							isOk = flagNum > comparisonNum;
							break;
					}
				}
				//アイテムを検査
				else if (conditionData is ObjectConditionItem)
				{
					var conditionItem = conditionData as ObjectConditionItem;
					//Debug.Log("アイテムを判定 " +conditionItem.ItemKind);
					isOk = _saveDataInstance.GetItemNum().Exists(x => x == conditionItem.ItemKind);
				}
				//選択アイテムを検査
				else if (conditionData is ObjectConditionItemSelect)
				{
					var conditionItemSelect = conditionData as ObjectConditionItemSelect;
					//Debug.Log("アイテムを選択中 " + conditionItemSelect.ItemKind);
					isOk = RoomUI.Instance.IsUseItem(conditionItemSelect.ItemKind);
				}
				//条件が合わなかった
				if (!isOk)
				{
					return false;
				}
			}
			return true;
		}
	}
}
