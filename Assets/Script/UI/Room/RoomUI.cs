using System;
using System.Collections.Generic;
using Item;
using Save;
using TMPro;
using UI.Dialog;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Room
{
	/// <summary>
	/// ルーム操作をするUI
	/// アイテム表示やルーム移動を行える
	/// </summary>
	public class RoomUI : MonoBehaviour
	{
		/// <summary>
		/// 左側のルームへ行くボタン
		/// </summary>
		[SerializeField] private Button _leftButton;

		/// <summary>
		/// 右側のルームへ行くボタン
		/// </summary>
		[SerializeField] private Button _rightButton;

		/// <summary>
		/// 手前のルームへ行くボタン
		/// </summary>
		[SerializeField] private Button _backButton;

		/// <summary>
		/// ヒントを表示するボタン
		/// </summary>
		[SerializeField] private Button _hintButton;

		/// <summary>
		/// ヒントを表示するボタンの文字
		/// </summary>
		[SerializeField] private TextMeshProUGUI _hintButtonMessage;

		/// <summary>
		/// アイテムアイコンのオリジナル
		/// これを複製する
		/// </summary>
		[SerializeField] private ItemIconUI _originalItemIcon;

		/// <summary>
		/// アイテムウィンドウの親オブジェクト
		/// </summary>
		[SerializeField] private Transform _itemIconBase;

		/// <summary>
		/// 複製したアイテムアイコンを格納
		/// </summary>
		private List<ItemIconUI> _itemIconList = new List<ItemIconUI>();

		/// <summary>
		/// クリックしたアイテム
		/// </summary>
		private SaveData.ItemKind _selectItemKind = SaveData.ItemKind.NONE;

		/// <summary>
		/// アイテムアイコンのマックス数
		/// </summary>
		private const int MAX_ITEM_VIEW = 4;

		/// <summary>
		/// 外部からアクセスするのに使用
		/// </summary>
		public static RoomUI Instance;

		/// <summary>
		/// アイテム選択時のイベント
		/// </summary>
		public Action OnSelectedItem;

		/// </summary>
		public Action<SaveData.ItemKind> OnUnisonItem;

		private Save.SaveManager _saveManager;

		private ItemAssetsDataBase _itemAssetsDataBase;
		private Csv.HintMasterReader _hintMasterReader;
		private SaveData.LANGUAGE _lastLanguage = SaveData.LANGUAGE.JAPAN;
		private Action _pendingRewardSuccessAction;
		private bool _pendingRewardEarned;
		private bool _rewardEventRegistered;

		/// <summary>
		/// 初期化
		/// </summary>
		public void Init(Stage.StageManager stageManager, Save.SaveManager saveManager,
			Csv.HintMasterReader hintMasterReader, ItemAssetsDataBase itemAssetsDataBase)
		{
			Instance = this;
			_saveManager = saveManager;
			_itemAssetsDataBase = itemAssetsDataBase;
			_hintMasterReader = hintMasterReader;
			_lastLanguage = _saveManager.SaveDataInstance.GetLanguage();
			RegisterRewardEvents();
			//左の部屋へ移動
			_leftButton.onClick.AddListener(() =>
			{
				SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.MoveRoom);
				stageManager.MoveLeft();
			});
			//右の部屋へ移動
			_rightButton.onClick.AddListener(() =>
			{
				SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.MoveRoom);
				stageManager.MoveRight();
			});
			//手前の部屋へ移動
			_backButton.onClick.AddListener(() =>
			{
				SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.MoveRoomBack);
				stageManager.MoveBack();
			});

			//ヒントの表示
			_hintButton.onClick.AddListener(() =>
			{
				var hintList = hintMasterReader.HintMasterDataList;

				for (int i = 0; i < hintList.Count; i++)
				{
					var hint = hintList[i];
					if (_saveManager.SaveDataInstance.GetFlagNum(hint.Flag) == 0)
					{
						var dialogManager = UI.Dialog.DialogManager.GetInstance();
						OpenHintSelectionDialog(dialogManager, saveManager, hint, i);
						return;
					}
				}

				//対応メッセージがないならスルーしてサウンド鳴らす
				SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.Buzzer);
			});

			//入室時に左右移動の表示を更新
			stageManager.OnEnterRoom += (int roomID) =>
			{
				//左に移動可能なら表示
				_leftButton.gameObject.SetActive(stageManager.GetRoom(roomID)._leftRoom != null);
				//右に移動可能なら表示
				_rightButton.gameObject.SetActive(stageManager.GetRoom(roomID)._rightRoom != null);
				//手前に移動可能なら表示
				_backButton.gameObject.SetActive(stageManager.GetRoom(roomID)._backRoom != null);
			};

			//フラグが更新された
			stageManager.OnUpdateFlag += () =>
			{
				//ボタンのテキストを変える
				//ヒントか答えかの表示を更新する
				UpdateHintButtonText(hintMasterReader, saveManager);
			};

			//アイテムウィンドウを作っておく
			for (int i = 0; i < MAX_ITEM_VIEW; i++)
			{
				//アイコンUIを作成
				var itemIcon = Instantiate(_originalItemIcon).GetComponent<ItemIconUI>();
				//親オブジェクトを変更
				itemIcon.transform.SetParent(_itemIconBase);
				var rectTransform = itemIcon.GetComponent<RectTransform>();
				//特定の大きさ
				rectTransform.localScale = new Vector2(1.5f, 1.5f);
				//初期化処理
				itemIcon.Init(i);
				//ボタンを押したときのイベント設定
				itemIcon.OnPush += SelectedButton;

				itemIcon.SettingItemIcon(false);
				_itemIconList.Add(itemIcon);
			}

			//アイテム更新時にアイテムの表示を更新
			stageManager.OnUpdateItem += () => { UpdateItemView(); };
			UpdateItemView();
			UpdateHintButtonText(hintMasterReader, saveManager);
		}

		private void Update()
		{
			if (_saveManager == null || _saveManager.SaveDataInstance == null || _hintMasterReader == null)
			{
				return;
			}

			var language = _saveManager.SaveDataInstance.GetLanguage();
			if (_lastLanguage == language)
			{
				return;
			}

			_lastLanguage = language;
			UpdateHintButtonText(_hintMasterReader, _saveManager);
		}

		/// <summary>
		/// ヒントボタンの表示を更新する
		/// ヒントを表示できるときは「ヒント」
		/// 答えを表示できるときは「こたえ」
		/// 何も表示しないときは「---」
		/// </summary>
		/// <param name="hintMasterReader"></param>
		/// <param name="saveManager"></param>
		private void UpdateHintButtonText(Csv.HintMasterReader hintMasterReader, SaveManager saveManager)
		{
			//ボタンのテキストを変える
			//ヒントか答えかの表示を更新する
			var hintList = hintMasterReader.HintMasterDataList;
			for (int i = 0; i < hintList.Count; i++)
			{
				var hint = hintList[i];
				//クリアしてない謎があるかチェック
				if (_saveManager.SaveDataInstance.GetFlagNum(hint.Flag) == 0)
				{
					if (saveManager.SaveDataInstance.GetUsedHint(hint.Flag))
					{
						//一回見たらこたえを表示
						_hintButtonMessage.text = Localize("こたえ", "ANS\nWER");
					}
					else
					{
						//通常はヒントと表示
						_hintButtonMessage.text = Localize("ヒント", "Hint");
					}

					return;
				}
			}
			//表示の必要がないので非表示状態
			_hintButtonMessage.text = "---";
		}

		private void OpenHintSelectionDialog(DialogManager dialogManager, SaveManager saveManager, Csv.HintMasterData hint, int hintIndex)
		{
			var hasSeenHint = saveManager.SaveDataInstance.GetUsedHint(hint.Flag);
			var dialog = dialogManager.CreateYesNoDialog(GetHintPromptText(hasSeenHint));
			dialog.OffImage();

			if (hasSeenHint)
			{
				dialog.SetButtonTexts(
					Localize("答えを\n見る", "Show\nAnswer"),
					Localize("いいえ", "No"),
					Localize("もう一度\nヒント", "Hint Again"));
				dialog.OnYes += () =>
				{
					RequestRewardAndShow(dialogManager, saveManager, hint, hintIndex, true);
				};
				dialog.OnThird += () =>
				{
					ShowHintOrAnswer(dialogManager, saveManager, hint, hintIndex, false);
				};
			}
			else
			{
				dialog.SetButtonTexts(Localize("はい", "Yes"), Localize("いいえ", "No"));
				dialog.OnYes += () =>
				{
					RequestRewardAndShow(dialogManager, saveManager, hint, hintIndex, false);
				};
			}

			dialog.OnNo += () => { };
		}

		private void ShowHintOrAnswer(DialogManager dialogManager, SaveManager saveManager, Csv.HintMasterData hint, int hintIndex, bool isAnswer)
		{
			var message = hint.GetMessage((int)_saveManager.SaveDataInstance.GetLanguage(), isAnswer);
			var hintDialog = dialogManager.CreateHintDialog(message);
			SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.Decision);

			if (!saveManager.SaveDataInstance.GetUsedHint(hint.Flag))
			{
				saveManager.SaveDataInstance.SetUsedHint(hint.Flag);
			}

			UpdateHintButtonText(_hintMasterReader, saveManager);
			hintDialog.SetImage((isAnswer ? "Answer/" : "Hint/") + hintIndex);
		}

		private void RequestRewardAndShow(DialogManager dialogManager, SaveManager saveManager, Csv.HintMasterData hint, int hintIndex, bool isAnswer)
		{
			_pendingRewardEarned = false;
			_pendingRewardSuccessAction = () => { ShowHintOrAnswer(dialogManager, saveManager, hint, hintIndex, isAnswer); };
			AdmobLibrary.ShowReward();
		}

		private void RegisterRewardEvents()
		{
			if (_rewardEventRegistered)
			{
				return;
			}

			_rewardEventRegistered = true;
			AdmobLibrary.OnReward += HandleRewardEarned;
			AdmobLibrary.OnRewardClosed += HandleRewardClosed;
			AdmobLibrary.OnRewardFailedToShow += HandleRewardFailedToShow;
		}

		private void HandleRewardEarned(double amount)
		{
			_pendingRewardEarned = true;
		}

		private void HandleRewardClosed()
		{
			if (_pendingRewardEarned)
			{
				var action = _pendingRewardSuccessAction;
				_pendingRewardSuccessAction = null;
				_pendingRewardEarned = false;
				action?.Invoke();
				return;
			}

			_pendingRewardSuccessAction = null;
			_pendingRewardEarned = false;
		}

		private void HandleRewardFailedToShow()
		{
			_pendingRewardSuccessAction = null;
			_pendingRewardEarned = false;
			var dialogManager = DialogManager.GetInstance();
			if (dialogManager == null)
			{
				return;
			}

			var dialog = dialogManager.CreateDialog(Localize(
				"通信状況が悪いみたいです。通信環境の良いところで、もう一度トライしてみてください",
				"It looks like the connection is unstable. Please try again later in a place with a better connection."));
			dialog.OffImage();
		}

		private string GetHintPromptText(bool hasSeenHint)
		{
			if (hasSeenHint)
			{
				return Localize(
					"答えを見ますか？\nもう一度ヒントを見ることもできます。",
					"Would you like to see the answer?\nYou can also view the hint again.");
			}

			return Localize(
				"広告を見るとヒントがみれます。\n見ますか？",
				"Watch an ad to see the hint.\nWould you like to continue?");
		}

		private string Localize(string japanese, string english)
		{
			if (_saveManager != null && _saveManager.SaveDataInstance != null &&
				_saveManager.SaveDataInstance.GetLanguage() == SaveData.LANGUAGE.ENGLISH)
			{
				return english;
			}

			return japanese;
		}

		private void OnDestroy()
		{
			if (!_rewardEventRegistered)
			{
				return;
			}

			AdmobLibrary.OnReward -= HandleRewardEarned;
			AdmobLibrary.OnRewardClosed -= HandleRewardClosed;
			AdmobLibrary.OnRewardFailedToShow -= HandleRewardFailedToShow;
			_rewardEventRegistered = false;
		}

		/// <summary>
		/// アイテム表示の更新
		/// </summary>
		public void UpdateItemView()
		{
			//一旦アイテムすべて消す
			for (int i = 0; i < MAX_ITEM_VIEW; i++)
			{
				_itemIconList[i].SettingItemIcon(false);
				_itemIconList[i].SettingSprite(SaveData.ItemKind.NONE);
			}

			var items = _saveManager.SaveDataInstance.GetItemNum();
			for (int i = 0; i < items.Count; i++)
			{
				SaveData.ItemKind kind = items[i];
				//所持しているものを表示
				_itemIconList[i].SettingItemIcon(true);
				_itemIconList[i].SettingSprite(kind);
			}

			//ダイアログをすべて消す
			DialogManager.GetInstance().DialogClear();
			ResetSelectItem();
		}

		/// <summary>
		/// UIの入力可否を切り替える
		/// </summary>
		/// <param name="isEnabled"></param>
		public void SetInputEnabled(bool isEnabled)
		{
			if (_leftButton != null)
			{
				_leftButton.interactable = isEnabled;
			}
			if (_rightButton != null)
			{
				_rightButton.interactable = isEnabled;
			}
			if (_backButton != null)
			{
				_backButton.interactable = isEnabled;
			}
			if (_hintButton != null)
			{
				_hintButton.interactable = isEnabled;
			}
			for (int i = 0; i < _itemIconList.Count; i++)
			{
				_itemIconList[i].SetInteractable(isEnabled);
			}
		}

		/// <summary>
		/// アイテムセレクト表示を消す
		/// </summary>
		private void ResetSelectItem()
		{
			//一旦アイテムすべて消す
			for (int i = 0; i < MAX_ITEM_VIEW; i++)
			{
				_itemIconList[i].SetSelectActiveView(false);
			}

			//選択をなくす
			_selectItemKind = SaveData.ItemKind.NONE;
		}

		//ボックスをクリックすると選択状態
		private void SelectedButton(int index)
		{
			var oldSelectItem = _selectItemKind;
			//背景なくす
			ResetSelectItem();
			//アイテムがなければ選択しない
			var nowSelectItemUI = _itemIconList[index];
			if (nowSelectItemUI == null)
			{
				_selectItemKind = SaveData.ItemKind.NONE;
				return;
			}

			//クリックしたボックスの背景出す
			nowSelectItemUI.SetSelectActiveView(true);

			var selectItem = nowSelectItemUI.GetItem();
			//同じものをクリックしたなら拡大ダイアログを出す
			if (oldSelectItem != SaveData.ItemKind.NONE)
			{
				//合成素材となるアイテム
				var unionItem = _itemAssetsDataBase.GetUnionItem(oldSelectItem);
				if (oldSelectItem == selectItem)
				{
					if (selectItem == SaveData.ItemKind.KEY_4)
					{
						_selectItemKind = selectItem;
						OnSelectedItem?.Invoke();
						return;
					}
					//ダイアログをすべて消す
					DialogManager.GetInstance().DialogClear();
					//アイテム拡大ダイアログを出す
					DialogManager.GetInstance().CreateItemDialog(
						//アイテムマネージャからアイテム名取得
						_itemAssetsDataBase.GetName(selectItem),
						oldSelectItem,
						//閉じたらアイテム選択をなくす
						ResetSelectItem);
				}
				//選んだアイテムが合成素材なら合成処理
				else if (unionItem == selectItem)
				{
					//合成した
					OnUnisonItem(oldSelectItem);
					return;
				}
			}

			//選択アイテムとして取得
			_selectItemKind = selectItem;
			OnSelectedItem?.Invoke();
		}

		/// <summary>
		/// 該当アイテムを選択しているかどうか
		/// </summary>
		/// <param name="useItemKind"></param>
		/// <returns></returns>
		public bool IsUseItem(SaveData.ItemKind useItemKind)
		{
			return _selectItemKind == useItemKind;
		}
	}
}
