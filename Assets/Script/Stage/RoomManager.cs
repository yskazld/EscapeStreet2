using System.Collections;
using Stage.Object;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Stage
{
	/// <summary>
	/// 部屋の管理
	/// </summary>
	public class RoomManager : MonoBehaviour
	{
		[SerializeField] private RectTransform _rectTransform;
		
		public Vector2 Position { private set;get; }
		
		public int ID;

		/// <summary>
		/// オブジェクトの一覧
		/// StageManagerで参照するのでpublic
		/// </summary>
		public ObjectBase[] ObjectList { get; private set; } = null;

		/// <summary>
		/// 自動で動作をするオブジェクトの一覧
		/// クリックしなくても動作をする
		/// </summary>
		private AutoPlayObject[] _autoObjectList = null;

		/// <summary>
		/// ルーム進入時に動作をするオブジェクトの一覧
		/// </summary>
		private RoomEnterPlayObject[] _roomEnterObjectList = null;

		/// <summary>
		/// 左にある部屋
		/// </summary>
		[SerializeField] public RoomManager _leftRoom;

		/// <summary>
		/// 右にある部屋
		/// </summary>
		[SerializeField] public RoomManager _rightRoom;

		/// <summary>
		/// 手前にある部屋
		/// </summary>
		[SerializeField] public RoomManager _backRoom;
		
		/// <summary>
		/// ピンチインした時に移動するルーム
		/// </summary>
		[SerializeField] public RoomManager PinchOutRoom;

		/// <summary>
		/// ピンチアウトした時に移動するルーム
		/// </summary>
		[SerializeField] public RoomManager PinchInRoom;

		/// <summary>
		/// クリア演出用のパネル
		/// </summary>
		[SerializeField] private GameObject _clearPanelRoot;

		/// <summary>
		/// パネルに表示するテキスト
		/// </summary>
		[SerializeField] private TextMeshProUGUI _clearPanelText;
		[SerializeField] private float _clearPanelAutoHideSeconds = 2f;
		private Coroutine _clearPanelAutoHideCoroutine;

		
		/// <summary>
		/// 部屋の初期化
		/// 部屋に含まれているオブジェクトを登録する
		/// </summary>
		public void Init()
		{
			Position = _rectTransform.localPosition;
			//直下のオブジェクトを取得
			ObjectList = GetComponentsInChildren<ObjectBase>();
			_autoObjectList = GetComponentsInChildren<AutoPlayObject>();
			_roomEnterObjectList = GetComponentsInChildren<RoomEnterPlayObject>();
			//初期化
			foreach (var objectData in ObjectList)
			{
				objectData.Init(ID);
			}
				if (_clearPanelRoot != null)
				{
					_clearPanelRoot.SetActive(false);
					if (_clearPanelAutoHideCoroutine != null)
					{
						StopCoroutine(_clearPanelAutoHideCoroutine);
						_clearPanelAutoHideCoroutine = null;
					}
				}
			}

		/// <summary>
		/// クリック可能かを切り替える
		/// </summary>
		/// <param name="isActive"></param>
		public void SettingTouch(bool isActive)
		{
			foreach (var objectData in ObjectList)
			{
				objectData.SettingTouch(isActive);
			}
		}

		/// <summary>
		/// 入室時の処理
		/// </summary>
		public void EnterRoom()
		{
			UpdateAllObject();

			foreach (var objectData in _roomEnterObjectList)
			{
				objectData.EnterRoomToUpdate();
			}
		}

		/// <summary>
		/// オブジェクト更新
		/// </summary>
		public void UpdateAllObject()
		{
			//自動起動オブジェクトをチェック
			foreach (var objectData in _autoObjectList)
			{
				objectData.UpdateObject();
			}

			//見た目の更新
			foreach (var objectData in ObjectList)
			{
				objectData.UpdateObject();
			}
		}

		/// <summary>
		/// クリアパネルを表示する
		/// </summary>
		/// <param name="message">null または空なら既存の文言を維持</param>
		public void ShowClearPanel(string message = null)
		{
			if (_clearPanelRoot == null)
			{
				Debug.LogWarning($"Room ID {ID} にクリアパネルが設定されていません。");
				return;
			}
			if (!string.IsNullOrEmpty(message) && _clearPanelText != null)
			{
				_clearPanelText.text = message;
			}
			_clearPanelRoot.SetActive(true);
			if (_clearPanelAutoHideSeconds > 0f)
			{
				if (_clearPanelAutoHideCoroutine != null)
				{
					StopCoroutine(_clearPanelAutoHideCoroutine);
				}
				_clearPanelAutoHideCoroutine = StartCoroutine(AutoHideClearPanel());
			}
		}

		/// <summary>
		/// クリアパネルを隠す
		/// </summary>
		public void HideClearPanel()
		{
			if (_clearPanelRoot == null)
			{
				return;
			}
			_clearPanelRoot.SetActive(false);
			if (_clearPanelAutoHideCoroutine != null)
			{
				StopCoroutine(_clearPanelAutoHideCoroutine);
				_clearPanelAutoHideCoroutine = null;
			}
		}

		private IEnumerator AutoHideClearPanel()
		{
			yield return new WaitForSecondsRealtime(_clearPanelAutoHideSeconds);
			_clearPanelRoot.SetActive(false);
			_clearPanelAutoHideCoroutine = null;
		}
	}
}
