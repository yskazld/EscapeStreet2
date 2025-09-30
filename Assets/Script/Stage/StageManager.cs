using System;
using System.Collections.Generic;
using UnityEngine;
using Save;
using Stage.Object;
using TMPro;

namespace Stage
{
	/// <summary>
	/// ステージ全体を管理
	/// ステージ内のルームの管理をする
	/// </summary>
	public class StageManager : MonoBehaviour
	{
		private RectTransform _rectTransform;
		/// <summary>
		/// ルーム一覧
		/// </summary>
		private Dictionary<int,RoomManager> _roomList = new Dictionary<int, RoomManager>();

		/// <summary>
		/// GameManagerから持ってきたSaveを格納
		/// </summary>
		private SaveData _saveData;

		/// <summary>
		/// ルーム入室
		/// </summary>
		public Action<int> OnEnterRoom;
		/// <summary>
		/// アイテム更新されたのを検知
		/// 主にUIの表示更新通知に使っている
		/// </summary>
		public Action OnUpdateItem;

		/// <summary>
		/// フラグが更新されたのを検知
		/// ヒントボタンの更新に使っている
		/// </summary>
		public Action OnUpdateFlag;
		private float _firstDistance = 0f;
		private float _moveDistance = 0f;
		private bool _isPinch = false;

		private void Update()
		{
			
			//ピンチインアウトを検知する
			if (Input.touchCount >= 2)
			{
				Touch touch1 = Input.GetTouch(0);
				Touch touch2 = Input.GetTouch(1);
				if (touch2.phase == TouchPhase.Began)
				{
					_firstDistance = Vector2.Distance(touch1.position, touch2.position);
					_isPinch = false;
				}
				else if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
				{
					if (_isPinch)
					{
						return;
					}
					//動かしている
					_moveDistance = Vector2.Distance(touch1.position, touch2.position);
					if(_moveDistance - 300 > _firstDistance)
					{
						//ピンチアウト
						var room = GetNowRoom().PinchOutRoom;
						if (room != null)
						{
							EnterRoom(room.ID);
							_isPinch = true;
						}
					}
					else if (_moveDistance  < _firstDistance - 300)
					{
						//ピンチイン
						var room = GetNowRoom().PinchInRoom;
						if (room != null)
						{
							EnterRoom(room.ID);
							_isPinch = true;
						}
					}
				}
			}
		}

		/// <summary>
		/// 初期化　
		/// ステージに含まれている部屋を登録する
		/// </summary>
		/// <param name="gameManager"></param>
		public void Init(GameManager gameManager)
		{
			_rectTransform = GetComponent<RectTransform>();
			//直下のルームを取得
			var roomChildrenList = GetComponentsInChildren<RoomManager>();

			foreach (var roomChild in roomChildrenList)
			{
				_roomList.Add(roomChild.ID,roomChild);
			}
			
			_saveData = gameManager.SaveManagerInstance.SaveDataInstance;

			foreach (var room in _roomList)
			{
				//ルーム初期化
				room.Value.Init();
				//ルームは以下のオブジェクト挙動によるイベントを登録
				foreach (var objectBase in room.Value.ObjectList)
				{
				objectBase.OnClick += () =>
				{
					//ダイアログがあるならクリック操作しない
					if(UI.Dialog.DialogManager.GetInstance().IsDialogEnable())
					{
						return;
					}
					var isCurrentRoom = objectBase.RoomID == GetNowRoom().ID;
					if (!isCurrentRoom)
					{
						return;
					}

					var shouldLogStage1 = true;
					var beforeStep = 0;
					var beforeClear = 0;
					if (shouldLogStage1)
					{
						beforeStep = _saveData.GetFlagNum(SaveData.SaveFlag.STAGE_1_STEP);
						beforeClear = _saveData.GetFlagNum(SaveData.SaveFlag.STAGE_1_CLEAR);
					}

					var clickSuccess = objectBase.ClickAction();

					if (shouldLogStage1)
					{
						if (clickSuccess)
						{
							var afterStep = _saveData.GetFlagNum(SaveData.SaveFlag.STAGE_1_STEP);
							var afterClear = _saveData.GetFlagNum(SaveData.SaveFlag.STAGE_1_CLEAR);
							Debug.Log($"[Stage1 Sequence] {objectBase.name} (RoomID={objectBase.RoomID}) clicked. STEP {beforeStep} -> {afterStep}, CLEAR {beforeClear} -> {afterClear}");
						}
						else
						{
							var currentStep = _saveData.GetFlagNum(SaveData.SaveFlag.STAGE_1_STEP);
							Debug.Log($"[Stage1 Sequence] {objectBase.name} (RoomID={objectBase.RoomID}) click ignored. STEP remains {currentStep}");
						}
					}

					if (clickSuccess)
					{
						//何かしらの挙動が行われたなら表示更新
						_roomList[_saveData.GetNowRoom()].UpdateAllObject();
					}
				};
					
					objectBase.OnFlagChange += (SaveData.SaveFlag flag, int value) =>
					{
						var before = _saveData.GetFlagNum(flag);
						//フラグ代入
						_saveData.SetFlagNum(flag, value);
						var after = _saveData.GetFlagNum(flag);
						if (flag == SaveData.SaveFlag.STAGE_1_CLEAR)
						{
							Debug.Log($"[Stage1 Sequence] Flag {flag} set {before} -> {after}");
						}
						OnUpdateFlag?.Invoke();
					};

					objectBase.OnAddFlagChange += (SaveData.SaveFlag flag, int value) =>
					{
						var before = _saveData.GetFlagNum(flag);
						//フラグ加算
						_saveData.AddFlagNum(flag, value);
						var after = _saveData.GetFlagNum(flag);
						if (flag == SaveData.SaveFlag.STAGE_1_STEP)
						{
							Debug.Log($"[Stage1 Sequence] Flag {flag} add {value}. {before} -> {after}");
						}
						OnUpdateFlag?.Invoke();
					};

					objectBase.OnEnterRoom += (int id) =>
					{
						//入室時
						EnterRoom(id);
					};
					objectBase.OnUpdateItem += (SaveData.ItemKind kind, bool value) =>
					{
						//アイテム更新時
						if (value)
						{
							//アイテム追加
							_saveData.AddItemNum(kind);
						}
						else
						{
							//アイテム除去
							_saveData.RemoveItemNum(kind);
						}
						OnUpdateItem?.Invoke();
					};
				}
			}
		}
		
		/// <summary>
		/// 現在自身がいるルームを更新する
		/// </summary>
		public void UpdateNowRoom()
		{
			_roomList[_saveData.GetNowRoom()].UpdateAllObject();
		}

		/// <summary>
		/// ルーム入室
		/// </summary>
		public void EnterRoom(int roomID)
		{
			//タッチを無効化
			foreach (var room in _roomList)
			{
				room.Value.SettingTouch(false);
				room.Value.UpdateAllObject();
			}
			
			_roomList.TryGetValue(roomID, out var targetRoom);
			//選択中のルームのみタッチ可能
			targetRoom.SettingTouch(true);
			//入室
			targetRoom.EnterRoom();
			//入室時
			OnEnterRoom?.Invoke(roomID);

			Vector2 position = targetRoom.Position;
			position.x *= -1;
			position.y *= -1;
			_rectTransform.localPosition = position;
		}

		/// <summary>
		/// IDからルームを取得
		/// </summary>
		/// <param name="roomID"></param>
		/// <returns></returns>
		public RoomManager GetRoom(int roomID)
		{
			_roomList.TryGetValue(roomID, out var result);
			return result;
		}

		/// <summary>
		/// 現在のルームを取得
		/// </summary>
		/// <returns></returns>
		private RoomManager GetNowRoom()
		{
			_roomList.TryGetValue(_saveData.GetNowRoom(), out var result);
			return result;
		}

		/// <summary>
		/// 右のルームに移動
		/// </summary>
		/// <returns></returns>
		public void MoveRight()
		{
			var rightRoom = GetNowRoom()._rightRoom;
			if (rightRoom != null)
			{
				EnterRoom(rightRoom.ID);
			}
		}

		/// <summary>
		/// 左のルームに移動
		/// </summary>
		/// <returns></returns>
		public void MoveLeft()
		{
			var leftRoom = GetNowRoom()._leftRoom;
			if (leftRoom != null)
			{
				EnterRoom(leftRoom.ID);
			}
		}
		
		
		/// <summary>
		/// 手前のルームに移動
		/// </summary>
		/// <returns></returns>
		public void MoveBack()
		{
			var backRoom = GetNowRoom()._backRoom;
			if (backRoom != null)
			{
				EnterRoom(backRoom.ID);
			}
		}
	}
}
