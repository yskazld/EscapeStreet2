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

		/// <summary>
		/// 初期化
		/// </summary>
		public void Init(Stage.StageManager stageManager, Save.SaveManager saveManager,
			Csv.HintMasterReader hintMasterReader, ItemAssetsDataBase itemAssetsDataBase)
		{
			Instance = this;
			_saveManager = saveManager;
			_itemAssetsDataBase = itemAssetsDataBase;
			//左の部屋へ移動
			_leftButton.onClick.AddListener(() =>
			{
				SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.Select);
				stageManager.MoveLeft();
			});
			//右の部屋へ移動
			_rightButton.onClick.AddListener(() =>
			{
				SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.Select);
				stageManager.MoveRight();
			});
			//手前の部屋へ移動
			_backButton.onClick.AddListener(() =>
			{
				SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.Select);
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
						//trueなら答えを出す
						var isAnswer = saveManager.SaveDataInstance.GetUsedHint(hint.Flag);
						//答えかヒントで文言を変える
						var dialog = dialogManager.CreateYesNoDialog( isAnswer ? "こうこくをみるとこたえがみれます":"こうこくをみるとヒントがみれます");
						dialog.OffImage();
						//YESを押したときだけ進む
						dialog.OnYes += () =>
						{	
							//ここでリワード広告を出す
							
							//フラグがオンではないならそれを出す
							var message = hint.GetMessage((int)_saveManager.SaveDataInstance.GetLanguage(),
								//ヒントを一度見たかどうかのフラグを設定
								isAnswer);
							var hintDialog = dialogManager.CreateDialog(message);
							SoundManager.GetInstance().Play(SoundManager.SOUND_TYPE.Decision);
							//ヒントを見たフラグ
							saveManager.SaveDataInstance.SetUsedHint(hint.Flag);
							UpdateHintButtonText(hintMasterReader, saveManager);
							var link = "";
							if (isAnswer)
							{
								//答えの画像のリンク
								link = "Answer/" + link;
							}
							else
							{
								//ヒントの画像のリンク
								link = "Hint/" + link;
							}

							link += i.ToString();
							//ダイアログの画像を設定する
							hintDialog.SetImage(link);
						};
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
						_hintButtonMessage.text = "こたえ";
					}
					else
					{
						//通常はヒントと表示
						_hintButtonMessage.text = "ヒント";
					}

					return;
				}
			}
			//表示の必要がないので非表示状態
			_hintButtonMessage.text = "---";
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
