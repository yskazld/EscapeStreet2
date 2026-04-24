using System.Collections;
using GoogleMobileAds.Api;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Title
{
	/// <summary>
	/// タイトルのUI制御　主にmainシーンへの遷移
	/// </summary>
		public class TitleUI : MonoBehaviour
		{
			private const float ContinueInterstitialWaitSeconds = 1.5f;
			private const float ContinueInterstitialFallbackSeconds = 1.5f;

		/// <summary>
		/// スタートボタン
		/// </summary>
		[SerializeField] private Button _gameStartButton;
		/// <summary>
		/// 続きから
		/// </summary>
		[SerializeField] private Button _continueButton;
		[SerializeField] private Button _languageChangeButton;
		[SerializeField] private TMP_Text _startButtonText;
		[SerializeField] private TMP_Text _continueButtonText;
		[SerializeField] private TMP_Text _linkButtonText;
		[SerializeField] private TMP_Text _languageChangeButtonText;
		[SerializeField] private TMP_Text _googleLinkPanelTitleText;
		[SerializeField] private TMP_Text _appleLinkPanelTitleText;
		[SerializeField] private GameObject _titleImage;
		[SerializeField] private GameObject _titleImageEnglish;
		[SerializeField] [Range(0f, 1f)] private float _continueButtonHiddenAlpha = 0.25f;
		private bool _isContinueTransitionRunning;
		private Coroutine _continueInterstitialFallbackCoroutine;
		private CanvasGroup _continueButtonCanvasGroup;
		private Save.SaveManager _saveManager;
		
		// Start is called before the first frame update
		void Start()
		{
			AdmobLibrary.FirstSetting();
			AdmobLibrary.RequestBanner(AdSize.Banner, AdPosition.Bottom, false);

			AssignReferencesIfNeeded();

			//セーブクラス初期化
			_saveManager = new Save.SaveManager();
			var save = _saveManager;
			//ロード
			save.LoadOrInitializeReturnInitialize();
			//ボタンを押したとき
			_gameStartButton.onClick.AddListener(() =>
			{
				AdmobLibrary.ClearResumeInterstitialPending();
				AdmobLibrary.DestroyBanner();
				//データ初期化
				save.InitializeSaveData();
				save.Save();
				//mainシーンに遷移
				SceneManager.LoadScene("main");
			});
			//ボタンを押したとき
			_continueButton.onClick.AddListener(() =>
			{
				if (_isContinueTransitionRunning)
				{
					return;
				}

				StartCoroutine(ContinueWithInterstitialIfNeeded());
			});
			_continueButtonCanvasGroup = _continueButton.GetComponent<CanvasGroup>();
			if (_continueButtonCanvasGroup == null)
			{
				_continueButtonCanvasGroup = _continueButton.gameObject.AddComponent<CanvasGroup>();
			}

			if (_languageChangeButton != null)
			{
				_languageChangeButton.onClick.AddListener(() =>
				{
					ToggleLanguage();
				});
			}

			// コンティニューは初回でも薄く残し、セーブデータがある時だけ操作可能にする
			UpdateContinueButtonState(!save.SaveDataInstance.IsFirst);
			ApplyLanguage();
		}

		private void OnDestroy()
		{
			AdmobLibrary.OnInterstitialClosed -= HandleContinueInterstitialFinished;
			AdmobLibrary.OnInterstitialFailedToShow -= HandleContinueInterstitialFinished;
			StopContinueInterstitialFallback();
			AdmobLibrary.DestroyBanner();
		}

		private IEnumerator ContinueWithInterstitialIfNeeded()
		{
			_isContinueTransitionRunning = true;
			SetButtonsInteractable(false);

			if (!AdmobLibrary.HasResumeInterstitialPending())
			{
				LoadMainScene();
				yield break;
			}

			var elapsed = 0f;
			while (!AdmobLibrary.IsInterstitialReady() && elapsed < ContinueInterstitialWaitSeconds)
			{
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}

			if (!AdmobLibrary.IsInterstitialReady())
			{
				LoadMainScene();
				yield break;
			}

			AdmobLibrary.OnInterstitialClosed += HandleContinueInterstitialFinished;
			AdmobLibrary.OnInterstitialFailedToShow += HandleContinueInterstitialFinished;
			AdmobLibrary.ClearResumeInterstitialPending();
			AdmobLibrary.DestroyBanner();
			AdmobLibrary.PlayInterstitial();
			_continueInterstitialFallbackCoroutine = StartCoroutine(ContinueInterstitialFallback());
		}

		private void HandleContinueInterstitialFinished()
		{
			AdmobLibrary.OnInterstitialClosed -= HandleContinueInterstitialFinished;
			AdmobLibrary.OnInterstitialFailedToShow -= HandleContinueInterstitialFinished;
			StopContinueInterstitialFallback();
			LoadMainScene();
		}

		private void LoadMainScene()
		{
			AdmobLibrary.DestroyBanner();
			SceneManager.LoadScene("main");
		}

		private void SetButtonsInteractable(bool isInteractable)
		{
			if (_gameStartButton != null)
			{
				_gameStartButton.interactable = isInteractable;
			}

			if (_continueButton != null)
			{
				_continueButton.interactable = isInteractable;
			}
		}

		private void UpdateContinueButtonState(bool hasSaveData)
		{
			if (_continueButton == null)
			{
				return;
			}

			_continueButton.interactable = hasSaveData;

			if (_continueButtonCanvasGroup == null)
			{
				return;
			}

			_continueButtonCanvasGroup.alpha = hasSaveData ? 1f : _continueButtonHiddenAlpha;
			_continueButtonCanvasGroup.blocksRaycasts = hasSaveData;
			_continueButtonCanvasGroup.interactable = hasSaveData;
		}

		private void ToggleLanguage()
		{
			if (_saveManager?.SaveDataInstance == null)
			{
				return;
			}

			var language = _saveManager.SaveDataInstance.GetLanguage();
			language = language == Save.SaveData.LANGUAGE.ENGLISH
				? Save.SaveData.LANGUAGE.JAPAN
				: Save.SaveData.LANGUAGE.ENGLISH;

			_saveManager.SaveDataInstance.SetLanguage(language);
			_saveManager.Save();
			ApplyLanguage();
		}

		private void ApplyLanguage()
		{
			if (_saveManager?.SaveDataInstance == null)
			{
				return;
			}

			var isEnglish = _saveManager.SaveDataInstance.GetLanguage() == Save.SaveData.LANGUAGE.ENGLISH;

			if (_startButtonText != null)
			{
				_startButtonText.text = isEnglish ? "Start" : "始めから";
			}

			if (_continueButtonText != null)
			{
				_continueButtonText.text = isEnglish ? "Continue" : "続きから";
			}

			if (_linkButtonText != null)
			{
				_linkButtonText.text = isEnglish ? "Link" : "リンク";
			}

			if (_languageChangeButtonText != null)
			{
				_languageChangeButtonText.text = isEnglish ? "Language" : "言語切替";
			}

			var linkPanelTitle = isEnglish ? "Developer's\nOther Game" : "開発者の他のゲーム";

			if (_googleLinkPanelTitleText != null)
			{
				_googleLinkPanelTitleText.text = linkPanelTitle;
			}

			if (_appleLinkPanelTitleText != null)
			{
				_appleLinkPanelTitleText.text = linkPanelTitle;
			}

			if (_titleImage != null)
			{
				_titleImage.SetActive(!isEnglish);
			}

			if (_titleImageEnglish != null)
			{
				_titleImageEnglish.SetActive(isEnglish);
			}
		}

		private void AssignReferencesIfNeeded()
		{
			if (_languageChangeButton == null)
			{
				_languageChangeButton = FindButton("Language Change");
			}

			if (_startButtonText == null)
			{
				_startButtonText = FindButtonLabel(_gameStartButton != null ? _gameStartButton.gameObject : FindGameObject("Start"));
			}

			if (_continueButtonText == null)
			{
				_continueButtonText = FindButtonLabel(_continueButton != null ? _continueButton.gameObject : FindGameObject("Continue"));
			}

			if (_linkButtonText == null)
			{
				_linkButtonText = FindButtonLabel(FindGameObject("OtherGame"));
			}

			if (_languageChangeButtonText == null)
			{
				_languageChangeButtonText = FindButtonLabel(_languageChangeButton != null ? _languageChangeButton.gameObject : FindGameObject("Language Change"));
			}

			if (_googleLinkPanelTitleText == null)
			{
				_googleLinkPanelTitleText = FindTextByParentNameAndCurrentText("LinkPanelforGoogle", "開発者の他のゲーム");
			}

			if (_appleLinkPanelTitleText == null)
			{
				_appleLinkPanelTitleText = FindTextByParentNameAndCurrentText("LinkPanelforApple", "開発者の他のゲーム");
			}

			if (_titleImage == null)
			{
				_titleImage = FindGameObject("TitleImage");
			}

			if (_titleImageEnglish == null)
			{
				_titleImageEnglish = FindGameObject("TitleImage_English");
			}
		}

		private Button FindButton(string objectName)
		{
			var target = FindGameObject(objectName);
			return target != null ? target.GetComponent<Button>() : null;
		}

		private GameObject FindGameObject(string objectName)
		{
			var transforms = Resources.FindObjectsOfTypeAll<Transform>();
			foreach (var current in transforms)
			{
				if (current == null || !current.gameObject.scene.IsValid())
				{
					continue;
				}

				if (current.name == objectName)
				{
					return current.gameObject;
				}
			}

			return null;
		}

		private TMP_Text FindButtonLabel(GameObject buttonObject)
		{
			if (buttonObject == null)
			{
				return null;
			}

			return buttonObject.GetComponentInChildren<TMP_Text>(true);
		}

		private TMP_Text FindTextByParentNameAndCurrentText(string parentObjectName, string currentText)
		{
			var parent = FindGameObject(parentObjectName);
			if (parent == null)
			{
				return null;
			}

			var texts = parent.GetComponentsInChildren<TMP_Text>(true);
			foreach (var text in texts)
			{
				if (text != null && text.text == currentText)
				{
					return text;
				}
			}

			return null;
		}

		private IEnumerator ContinueInterstitialFallback()
		{
			yield return new WaitForSecondsRealtime(ContinueInterstitialFallbackSeconds);
			HandleContinueInterstitialFinished();
		}

		private void StopContinueInterstitialFallback()
		{
			if (_continueInterstitialFallbackCoroutine == null)
			{
				return;
			}

			StopCoroutine(_continueInterstitialFallbackCoroutine);
			_continueInterstitialFallbackCoroutine = null;
		}
	}
}
