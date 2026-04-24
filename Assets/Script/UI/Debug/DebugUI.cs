using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Debug
{
	/// <summary>
	/// デバック用
	/// 現状は言語切り替えボタンのみを入れています。
	/// </summary>
		public class DebugUI : MonoBehaviour
		{
			[SerializeField] private Button _saveDeleteButton;
			[SerializeField] private Button _languageChangeButton;
			[SerializeField] private TextMeshProUGUI _languageText;

			[SerializeField] private Button _stageJumpButton;
			[SerializeField] private GameObject _stageJumpPanelRoot;
			[SerializeField] private TMP_InputField _stageJumpInputField;
			[SerializeField] private Button _stageJumpConfirmButton;
			[SerializeField] private Button _stageJumpCancelButton;

			private void Start()
			{
				if (_saveDeleteButton != null)
				{
					_saveDeleteButton.onClick.AddListener(() =>
					{
						var save = GameManager.GetInstance().SaveManagerInstance;
						save.InitializeSaveData();
						save.Save();
						// mainシーンに遷移
						SceneManager.LoadScene("Main");
					});
				}
				else
				{
					//Debug.LogWarning("[DebugUI] SaveDeleteButton が設定されていません。");
				}

				if (_languageChangeButton != null)
				{
					_languageChangeButton.onClick.AddListener(() =>
					{
						var save = GameManager.GetInstance().SaveManagerInstance;
						var nowLanguage = save.SaveDataInstance.GetLanguage();
						if (nowLanguage == Save.SaveData.LANGUAGE.JAPAN)
						{
							nowLanguage = Save.SaveData.LANGUAGE.ENGLISH;
						}
						else if (nowLanguage == Save.SaveData.LANGUAGE.ENGLISH)
						{
							nowLanguage = Save.SaveData.LANGUAGE.JAPAN;
						}
						save.SaveDataInstance.SetLanguage(nowLanguage);
						UpdateLanguageText();
					});
				}
				else
				{
					//Debug.LogWarning("[DebugUI] LanguageChangeButton が設定されていません。");
				}

				UpdateLanguageText();
				SetupStageJumpUI();
			}

			/// <summary>
			/// テキスト更新
			/// </summary>
			private void UpdateLanguageText()
			{
				if (_languageText == null)
				{
					//Debug.LogWarning("[DebugUI] LanguageText が設定されていません。");
					return;
				}

				var save = GameManager.GetInstance()?.SaveManagerInstance;
				if (save == null)
				{
					//Debug.LogWarning("[DebugUI] SaveManager が利用できません。");
					return;
				}
				var nowLanguage = save.SaveDataInstance.GetLanguage();
				_languageText.text = nowLanguage == Save.SaveData.LANGUAGE.ENGLISH
					? "ENG\nLISH"
					: nowLanguage.ToString();
			}

			private void SetupStageJumpUI()
			{
				if (_stageJumpPanelRoot != null)
				{
					_stageJumpPanelRoot.SetActive(false);
				}

				if (_stageJumpButton != null)
				{
					_stageJumpButton.onClick.AddListener(() =>
					{
						ShowStageJumpPanel();
					});
				}
				else
				{
					//Debug.LogWarning("[DebugUI] StageJumpButton が設定されていません。");
				}

				if (_stageJumpConfirmButton != null)
				{
					_stageJumpConfirmButton.onClick.AddListener(() =>
					{
						JumpToRoom();
					});
				}
				else
				{
					//Debug.LogWarning("[DebugUI] StageJumpConfirmButton が設定されていません。");
				}

				if (_stageJumpCancelButton != null)
				{
					_stageJumpCancelButton.onClick.AddListener(HideStageJumpPanel);
				}
			}

			private void ShowStageJumpPanel()
			{
				if (_stageJumpPanelRoot != null)
				{
					_stageJumpPanelRoot.SetActive(true);
				}

				if (_stageJumpInputField != null)
				{
					_stageJumpInputField.text = string.Empty;
					_stageJumpInputField.ActivateInputField();
				}
				else
				{
					//Debug.LogWarning("[DebugUI] StageJumpInputField が設定されていません。");
				}
			}

			private void HideStageJumpPanel()
			{
				if (_stageJumpPanelRoot != null)
				{
					_stageJumpPanelRoot.SetActive(false);
				}
			}

			private void JumpToRoom()
			{
				if (_stageJumpInputField == null)
				{
					//Debug.LogWarning("[DebugUI] StageJumpInputField が設定されていません。");
					return;
				}

				if (!int.TryParse(_stageJumpInputField.text, out var roomId))
				{
					//Debug.LogWarning($"[DebugUI] Room ID '{_stageJumpInputField.text}' を数値に変換できません。");
					return;
				}

				var gameManager = GameManager.GetInstance();
				if (gameManager == null || gameManager.StageManagerInstance == null)
				{
					//Debug.LogWarning("[DebugUI] GameManager または StageManager が利用できません。");
					return;
				}

				if (gameManager.StageManagerInstance.GetRoom(roomId) == null)
				{
					//Debug.LogWarning($"[DebugUI] Room ID {roomId} は存在しません。");
					return;
				}

				gameManager.StageManagerInstance.EnterRoom(roomId);
				HideStageJumpPanel();
			}
		}
	}
