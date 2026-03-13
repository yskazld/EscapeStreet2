using Item;
using UI.Dialog;
using UnityEngine;

/// <summary>
/// ゲーム全体を管理
/// </summary>
public class GameManager : MonoBehaviour
{
	/// <summary>
	/// ステージ全体管理
	/// </summary>
	public Stage.StageManager StageManagerInstance;
	/// <summary>
	/// セーブデータの管理
	/// </summary>
	public Save.SaveManager SaveManagerInstance;

	/// <summary>
	/// アイテムの情報管理
	/// 名前などを格納している
	/// </summary>
	public ItemAssetsDataBase ItemAssetsDataBaseInstance = new ItemAssetsDataBase();
	
	/// <summary>
	/// csv読み込み管理
	/// </summary>
	private Csv.HintMasterReader _hintMasterReaderInstance;
	/// <summary>
	/// 外部からアクセスできるようにする
	/// </summary>
	private static GameManager _main;
	/// <summary>
	/// ルームのUI
	/// </summary>
	[SerializeField] private UI.Room.RoomUI _roomUI = null;

	/// <summary>
	/// ダイアログの管理
	/// </summary>
	[SerializeField] private DialogManager _dialogManager;
	
	void Awake()
	{
		if (StageManagerInstance == null)
		{
			//優先的に子階層から探し、なければシーン全体から取得
			StageManagerInstance = GetComponentInChildren<Stage.StageManager>();
			if (StageManagerInstance == null)
			{
				StageManagerInstance = FindObjectOfType<Stage.StageManager>();
			}
		}
		if (StageManagerInstance == null)
		{
			Debug.LogError("StageManagerInstance が見つかりません。GameManager の Inspector で割り当てるか、シーンに配置してください。");
			return;
		}

		if (_dialogManager == null)
		{
			_dialogManager = FindObjectOfType<DialogManager>();
		}
		if (_dialogManager == null)
		{
			Debug.LogError("DialogManager が見つかりません。GameManager の Inspector で割り当ててください。");
			return;
		}

		if (_roomUI == null)
		{
			_roomUI = FindObjectOfType<UI.Room.RoomUI>();
		}
		if (_roomUI == null)
		{
			Debug.LogError("RoomUI が見つかりません。GameManager の Inspector で割り当ててください。");
			return;
		}

		//ダイアログの初期化
		_dialogManager.Init();
		//自信を格納し外部からアクセスできるようにする
		_main = this;
		//セーブクラス初期化
		SaveManagerInstance = new Save.SaveManager();

		//ロード
		SaveManagerInstance.LoadOrInitializeReturnInitialize();
		
		//初回フラグを無効化
		//タイトルでContinueを消すために使う
		SaveManagerInstance.SaveDataInstance.FirstOff();

		//アイテム情報の初期化
		ItemAssetsDataBaseInstance.Init();
		
		//csvを読み込み
		_hintMasterReaderInstance = new Csv.HintMasterReader("HintMaster_Street");
		_hintMasterReaderInstance.Load();

		
		//ステージ初期化
		StageManagerInstance.Init(this);
		//UIの初期化
		_roomUI.Init(StageManagerInstance, SaveManagerInstance, _hintMasterReaderInstance,ItemAssetsDataBaseInstance);

		//ルーム入室時のイベント登録
		StageManagerInstance.OnEnterRoom += (int roomID) => 
		{
			//現状のルームを設定
			SaveManagerInstance.SaveDataInstance.SetNowRoom(roomID);
			//ルーム進入したらセーブをする
			SaveManagerInstance.Save();
		};

		//ルームでアイテムを選択した
		_roomUI.OnSelectedItem += () =>
		{
			//今いるルームを更新
			StageManagerInstance.UpdateNowRoom();
		};
		
		//アイテムを選択した
		_roomUI.OnSelectedItem += () =>
		{
			//今いるルームを更新
			StageManagerInstance.UpdateNowRoom();
		};
		
		//アイテムを合成した
		_roomUI.OnUnisonItem += (item) =>
		{
			//合成結果
			var unionItemResult = ItemAssetsDataBaseInstance.GetUnionItemResult(item);
			var saveData = SaveManagerInstance.SaveDataInstance;
			//アイテムを消費する===========
			//元アイテム
			saveData.RemoveItemNum(item);
			//合成素材アイテム
			saveData.RemoveItemNum(ItemAssetsDataBaseInstance.GetUnionItem(item));
			//==========================
			//合成結果アイテムを追加する
			saveData.AddItemNum(unionItemResult);
			//今いるルームアイテム欄を更新
			_roomUI.UpdateItemView();
		};

		//ルームに入室
		StageManagerInstance.EnterRoom(SaveManagerInstance.SaveDataInstance.GetNowRoom());
	}

	/// <summary>
	/// 外部からGameManagerをアクセスする際に使う
	/// </summary>
	/// <returns></returns>
	public static GameManager GetInstance()
	{
		return _main;
	}

	/// <summary>
	/// ホームキーを押したときアプリ終了時などはfalseを取得
	/// </summary>
	/// <param name="focus"></param>
	private void OnApplicationFocus(bool focus)
	{
		if(!focus)
		{
			//進行保存
			//Saveはゲームクリア時など任意のタイミングでも行う
			SaveManagerInstance.Save();
		}
	}
}
