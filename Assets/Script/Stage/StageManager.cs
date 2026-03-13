using System;
using System.Collections;
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
		private SaveManager _saveManager;

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
		private readonly HashSet<SaveData.SaveFlag> _alreadyNotifiedClearFlags = new HashSet<SaveData.SaveFlag>();
		private readonly List<SaveData.SaveFlag> _clearFlagCandidates = new List<SaveData.SaveFlag>();
		private Coroutine _moveRoutine;
		private bool _inputEnabled = true;

		private void Update()
		{
			CheckStageClearFlags();

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
					var currentRoom = GetNowRoom();
					if (currentRoom == null)
					{
						return;
					}
					if(_moveDistance - 300 > _firstDistance)
					{
						//ピンチアウト
						var room = currentRoom.PinchOutRoom;
						if (room != null)
						{
							EnterRoom(room.ID);
							_isPinch = true;
						}
					}
					else if (_moveDistance  < _firstDistance - 300)
					{
						//ピンチイン
						var room = currentRoom.PinchInRoom;
						if (room != null)
						{
							EnterRoom(room.ID);
							_isPinch = true;
						}
					}
				}
			}
		}

		private void CheckStageClearFlags()
		{
			if (_saveData == null)
			{
				return;
			}

			foreach (var flag in _clearFlagCandidates)
			{
				if (_saveData.GetFlagNum(flag) > 0)
				{
					NotifyStageClear(flag);
				}
			}
		}

		private void HandleStage1Progress(int step)
		{
			const int goalStep = 4;
			if (step < goalStep)
			{
				return;
			}
			var isAlreadyCleared = _saveData.GetFlagNum(SaveData.SaveFlag.STAGE_1_CLEAR) > 0;
			if (isAlreadyCleared)
			{
				return;
			}
			_saveData.SetFlagNum(SaveData.SaveFlag.STAGE_1_CLEAR, 1);
			Debug.Log($"[Stage1 Sequence] Stage 1 clear triggered. STEP={step}");
			NotifyStageClear(SaveData.SaveFlag.STAGE_1_CLEAR);
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

			_roomList.Clear();
			foreach (var roomChild in roomChildrenList)
			{
				if (_roomList.TryGetValue(roomChild.ID, out var alreadyRegistered))
				{
					Debug.LogError($"[StageManager] Room ID {roomChild.ID} is duplicated. Existing: {alreadyRegistered.name}, Duplicate: {roomChild.name}. Please assign unique IDs.");
					continue;
				}
				_roomList.Add(roomChild.ID, roomChild);
			}
			
			_saveManager = gameManager.SaveManagerInstance;
			_saveData = _saveManager.SaveDataInstance;
			_alreadyNotifiedClearFlags.Clear();

			_clearFlagCandidates.Clear();
			foreach (SaveData.SaveFlag flag in Enum.GetValues(typeof(SaveData.SaveFlag)))
			{
				if (IsClearFlag(flag))
				{
					_clearFlagCandidates.Add(flag);
				}
			}

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
					var currentRoom = GetNowRoom();
					if (currentRoom == null)
					{
						return;
					}
					var isCurrentRoom = objectBase.RoomID == currentRoom.ID;
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
						if (flag == SaveData.SaveFlag.STAGE11_ON_BATIN1 || flag == SaveData.SaveFlag.STAGE11_ON_BATIN2)
						{
							Debug.Log($"[Stage11 Battery] {flag} set {before} -> {after} (requested {value})");
						}
						if (after > 0 && IsClearFlag(flag))
						{
							NotifyStageClear(flag);
						}
						_saveManager.Save();
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
							HandleStage1Progress(after);
						}
						_saveManager.Save();
						OnUpdateFlag?.Invoke();
					};

					objectBase.OnEnterRoom += (int id) =>
					{
						//入室時
						EnterRoom(id);
					};
					objectBase.OnEnterRoomWithDelay += (int id, float delay, float moveSeconds) =>
					{
						EnterRoom(id, delay, moveSeconds);
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
						_saveManager.Save();
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
			if (_moveRoutine != null)
			{
				StopCoroutine(_moveRoutine);
				_moveRoutine = null;
			}
			EnterRoomImmediate(roomID);
		}

		/// <summary>
		/// ステージ全体の入力を有効/無効にする
		/// </summary>
		/// <param name="isEnabled"></param>
		public void SetInputEnabled(bool isEnabled)
		{
			_inputEnabled = isEnabled;
			if (!_inputEnabled)
			{
				DisableAllRoomTouches();
				return;
			}

			var currentRoom = GetNowRoom();
			if (currentRoom != null)
			{
				currentRoom.SettingTouch(true);
			}
		}

		/// <summary>
		/// 遅延と移動時間を指定してルーム入室
		/// </summary>
		public void EnterRoom(int roomID, float delaySeconds, float moveSeconds)
		{
			if (_moveRoutine != null)
			{
				StopCoroutine(_moveRoutine);
				_moveRoutine = null;
			}
			_moveRoutine = StartCoroutine(EnterRoomRoutine(roomID, delaySeconds, moveSeconds));
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

		private static bool IsClearFlag(SaveData.SaveFlag flag)
		{
			return flag.ToString().Contains("_CLEAR");
		}

		public void ShowRoomClearPanel(int roomID, string message = null)
		{
			if (!_roomList.TryGetValue(roomID, out var room))
			{
				Debug.LogWarning($"[StageManager] Room ID {roomID} not found when trying to show clear panel.");
				return;
			}
			room.ShowClearPanel(message);
		}

		public void NotifyStageClear(SaveData.SaveFlag flag, string message = null)
		{
			if (!IsClearFlag(flag))
			{
				return;
			}
			if (!_alreadyNotifiedClearFlags.Add(flag))
			{
				return;
			}
			ShowClearPanelForCurrentRoom(message);
		}

		private void ShowClearPanelForCurrentRoom(string message)
		{
			var currentRoom = GetNowRoom();
			if (currentRoom == null)
			{
				Debug.LogWarning("[StageManager] Current room is not available to show clear panel.");
				return;
			}
			currentRoom.ShowClearPanel(message);
		}

		/// <summary>
		/// 現在のルームを取得
		/// </summary>
		/// <returns></returns>
		private RoomManager GetNowRoom()
		{
			if (_saveData == null)
			{
				Debug.LogWarning("[StageManager] SaveData is not initialized.");
				return null;
			}
			_roomList.TryGetValue(_saveData.GetNowRoom(), out var result);
			return result;
		}

		/// <summary>
		/// 右のルームに移動
		/// </summary>
		/// <returns></returns>
		public void MoveRight()
		{
			var currentRoom = GetNowRoom();
			if (currentRoom == null)
			{
				return;
			}
			var rightRoom = currentRoom._rightRoom;
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
			var currentRoom = GetNowRoom();
			if (currentRoom == null)
			{
				return;
			}
			var leftRoom = currentRoom._leftRoom;
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
			var currentRoom = GetNowRoom();
			if (currentRoom == null)
			{
				return;
			}
			var backRoom = currentRoom._backRoom;
			if (backRoom != null)
			{
				EnterRoom(backRoom.ID);
			}
		}

		private void EnterRoomImmediate(int roomID)
		{
			DisableAllRoomTouches();
			
			if (!_roomList.TryGetValue(roomID, out var targetRoom))
			{
				return;
			}
			targetRoom.SettingTouch(_inputEnabled);
			targetRoom.EnterRoom();
			OnEnterRoom?.Invoke(roomID);

			_rectTransform.localPosition = GetRoomPosition(roomID);
		}

		private IEnumerator EnterRoomRoutine(int roomID, float delaySeconds, float moveSeconds)
		{
			DisableAllRoomTouches();

			if (!_roomList.TryGetValue(roomID, out var targetRoom))
			{
				_moveRoutine = null;
				yield break;
			}

			if (delaySeconds > 0f)
			{
				yield return new WaitForSeconds(delaySeconds);
			}

			targetRoom.SettingTouch(_inputEnabled);
			targetRoom.EnterRoom();
			OnEnterRoom?.Invoke(roomID);

			var startPos = _rectTransform.localPosition;
			var targetPos = GetRoomPosition(roomID);

			if (moveSeconds <= 0f)
			{
				_rectTransform.localPosition = targetPos;
			}
			else
			{
				var elapsed = 0f;
				while (elapsed < moveSeconds)
				{
					elapsed += Time.deltaTime;
					var t = Mathf.Clamp01(elapsed / moveSeconds);
					_rectTransform.localPosition = Vector2.Lerp(startPos, targetPos, t);
					yield return null;
				}
			}

			_moveRoutine = null;
		}

		private void DisableAllRoomTouches()
		{
			foreach (var room in _roomList)
			{
				room.Value.SettingTouch(false);
			}
		}

		private Vector2 GetRoomPosition(int roomID)
		{
			if (!_roomList.TryGetValue(roomID, out var room))
			{
				return _rectTransform.localPosition;
			}
			Vector2 position = room.Position;
			position.x *= -1;
			position.y *= -1;
			return position;
		}
	}
}
