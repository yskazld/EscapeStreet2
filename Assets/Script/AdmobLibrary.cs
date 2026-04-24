using GoogleMobileAds.Api;
using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// adMobを使用するためのクラス
/// </summary>
public class AdmobLibrary
{
	private const string ResumeInterstitialPendingKey = "Admob.ResumeInterstitialPending";
	private static BannerView _bannerView;
	private static InterstitialAd _interstitialAd;
	private static RewardedAd _rewardedAd;
	private static bool _isInitialized;
	private static bool _isInitializing;
	private static bool _returnedToForegroundInCurrentProcess;
	private static bool _isLoadingInterstitial;
	private static bool _isLoadingReward;
	private static bool _isInterstitialReloadScheduled;
	private static bool _isRewardReloadScheduled;
	private static int _interstitialRetryCount;
	private static int _rewardRetryCount;

	public static Action<double> OnReward;
	public static Action OnRewardClosed;
	public static Action OnRewardFailedToShow;

	public static Action OnLoadedInterstitial;
	public static Action OnInterstitialClosed;
	public static Action OnInterstitialFailedToShow;

	/// <summary>
	/// ゲーム起動　初回に一度だけ呼ぶ
	/// </summary>
	public static void FirstSetting()
	{
		if (_isInitialized || _isInitializing)
		{
			return;
		}

		_isInitializing = true;
		//13歳以下を対象と「する」場合はtrue
		RequestConfiguration request = new RequestConfiguration
		{
			TagForChildDirectedTreatment = TagForChildDirectedTreatment.False
		};


		MobileAds.SetRequestConfiguration(request);
		MobileAds.RaiseAdEventsOnUnityMainThread = true;

		MobileAds.Initialize((InitializationStatus initStatus) =>
		{
			// This callback is called once the MobileAds SDK is initialized.
			_isInitializing = false;
			_isInitialized = true;
			RestoreGameTiming();
			InitInterstitial();
			LoadReward();

		});
	}


	/// <summary>
	/// バナー広告を生成
	/// </summary>
	/// <param name="size"></param>
	/// <param name="position"></param>
	public static void RequestBanner(AdSize size, AdPosition position, bool collapsible)
	{
		FirstSetting();
#if UNITY_ANDROID
        //自分のID
        //string adUnitId = "ca-app-pub-6073747809973329/9617256012"; // ← ご自身のユニットID
    
        //テストプレイ用ID
	    string adUnitId = "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IPHONE
        //自分のID
        //string adUnitId = "ca-app-pub-6073747809973329/6719629939"; 
        // ← テストID
        string adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
		string adUnitId = "unexpected_platform";

        
#endif

		if (_bannerView != null)
		{
			_bannerView.Destroy();
			_bannerView = null;
		}

		AdSize adaptiveSize =
					AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

		_bannerView = new BannerView(adUnitId, adaptiveSize, position);

		//セーフエリアを考慮
		// var area = Screen.safeArea;
		// _bannerView = new BannerView(adUnitId, size, Screen.width/4 ,Screen.height /10);

		// Create an empty ad request.

		var adRequest = new AdRequest();

		if (collapsible)
		{
			//折り畳みバナー設定
			adRequest.Extras.Add("collapsible", "bottom");
		}

		// Load the banner with the request.
		_bannerView.LoadAd(adRequest);
		Debug.Log($"ロード完了、アダプティブバナーサイズ: {_bannerView.GetHeightInPixels()} {_bannerView.GetWidthInPixels()}");
	}

	/// <summary>
	/// バナー広告削除
	/// </summary>
	public static void DestroyBanner()
	{
		if (_bannerView != null)
		{
			_bannerView.Destroy();
			_bannerView = null;
		}
	}

	/// <summary>
	/// インタースティシャル読み込み
	/// </summary>
	private static void InitInterstitial()
	{
		if (_isLoadingInterstitial)
		{
			return;
		}

#if UNITY_ANDROID
        //自分のID
        //string adUnitId = "ca-app-pub-6073747809973329/1658874944";

        //テストプレイ用のID
		string adUnitId = "ca-app-pub-3940256099942544/3419835294";
#elif UNITY_IPHONE
        //自分のID 
        //string adUnitId = "ca-app-pub-6073747809973329/7923016492";

        //テストプレイ用のID
        string adUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
		string adUnitId = "unexpected_platform";
#endif
		// Initialize an InterstitialAd.

		var adRequest = new AdRequest();
		if (_interstitialAd != null)
		{
			_interstitialAd.Destroy();
			_interstitialAd = null;
		}

		_isLoadingInterstitial = true;
		Debug.Log("InitInterstitial");
		// send the request to load the ad.
		InterstitialAd.Load(adUnitId, adRequest,
			(InterstitialAd ad, LoadAdError error) =>
			{
				_isLoadingInterstitial = false;
				// if error is not null, the load request failed.
				if (error != null || ad == null)
				{
					Debug.LogError("interstitial ad failed to load an ad " +
					               "with error : " + error);
					ScheduleInterstitialReload();
					return;
				}

				_interstitialRetryCount = 0;
				Debug.Log("Interstitial ad loaded with response : "
				          + ad.GetResponseInfo());

				// Raised when the ad is estimated to have earned money.
				ad.OnAdPaid += (AdValue adValue) =>
				{
					Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
						adValue.Value,
						adValue.CurrencyCode));
				};
				// Raised when an impression is recorded for an ad.
				ad.OnAdImpressionRecorded += () => { Debug.Log("Interstitial ad recorded an impression."); };
				// Raised when a click is recorded for an ad.
				ad.OnAdClicked += () => { Debug.Log("Interstitial ad was clicked."); };
				// Raised when an ad opened full screen content.
				ad.OnAdFullScreenContentOpened += () => { Debug.Log("Interstitial ad full screen content opened."); };
				// Raised when the ad closed full screen content.
				ad.OnAdFullScreenContentClosed += () =>
				{
					Debug.Log("Interstitial ad full screen content closed.");
					RestoreGameTiming();
					_interstitialAd = null;
					InitInterstitial();
					OnInterstitialClosed?.Invoke();
				};
				// Raised when the ad failed to open full screen content.
				ad.OnAdFullScreenContentFailed += (AdError error) =>
				{
					Debug.LogError("Interstitial ad failed to open full screen content " +
					               "with error : " + error);
					RestoreGameTiming();
					_interstitialAd = null;
					ScheduleInterstitialReload();
					OnInterstitialFailedToShow?.Invoke();
				};
				_interstitialAd = ad;
				OnLoadedInterstitial?.Invoke();
			});
	}

	/// <summary>
	/// インタースティシャルを出す
	/// </summary>
	public static void PlayInterstitial()
	{
		Debug.Log("PlayInterstitial");
		FirstSetting();
		if (_interstitialAd != null && _interstitialAd.CanShowAd())
		{
			Debug.Log("Showing interstitial ad.");
			_interstitialAd.Show();
		}
		else
		{
			Debug.LogError("Interstitial ad is not ready yet.");
			InitInterstitial();
			OnInterstitialFailedToShow?.Invoke();
		}
	}

	/// <summary>
	/// インタースティシャル削除
	/// </summary>
	public static void DestroyInterstitial()
	{
		if (_interstitialAd != null)
		{
			Debug.Log("DestroyInterstitial");
			_interstitialAd.Destroy();
			_interstitialAd = null;
		}
	}

	public static bool IsInterstitialReady()
	{
		return _interstitialAd != null && _interstitialAd.CanShowAd();
	}

	/// <summary>
	/// リワード広告
	/// </summary>
	public static void LoadReward()
	{
		FirstSetting();
		if (_isLoadingReward)
		{
			return;
		}

		string adUnitId;
#if UNITY_ANDROID
        //自分のID
        //adUnitId = "ca-app-pub-6073747809973329/5406548260";

        //テストプレイ用のID
		adUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
        //自分のID
        //adUnitId = "ca-app-pub-6073747809973329/2869343822";

        //テストプレイ用のID
        adUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
		adUnitId = "unexpected_platform";
#endif
		var adRequest = new AdRequest();
		if (_rewardedAd != null)
		{
			_rewardedAd.Destroy();
		}
		_rewardedAd = null;
		_isLoadingReward = true;
		RewardedAd.Load(adUnitId, adRequest,
			(RewardedAd ad, LoadAdError error) =>
			{
				_isLoadingReward = false;
				// if error is not null, the load request failed.
				if (error != null || ad == null)
				{
					Debug.LogError("rewarded ad failed to load an ad " +
					               "with error : " + error);
					ScheduleRewardReload();
					return;
				}

				_rewardRetryCount = 0;
				Debug.Log("Rewarded ad loaded with response : "
				          + ad.GetResponseInfo());
				// Raised when the ad is estimated to have earned money.
				ad.OnAdPaid += (AdValue adValue) =>
				{
					Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
						adValue.Value,
						adValue.CurrencyCode));
				};
				// Raised when an impression is recorded for an ad.
				ad.OnAdImpressionRecorded += () => { Debug.Log("Rewarded ad recorded an impression."); };
				// Raised when a click is recorded for an ad.
				ad.OnAdClicked += () => { Debug.Log("Rewarded ad was clicked."); };
				// Raised when an ad opened full screen content.
				ad.OnAdFullScreenContentOpened += () => { Debug.Log("Rewarded ad full screen content opened."); };
				// Raised when the ad closed full screen content.
				ad.OnAdFullScreenContentClosed += () =>
				{
					Debug.Log("Rewarded ad full screen content closed.");
					RestoreGameTiming();
					OnRewardClosed?.Invoke();
					_rewardedAd = null;
					LoadReward();
				};
				// Raised when the ad failed to open full screen content.
				ad.OnAdFullScreenContentFailed += (AdError error) =>
				{
					Debug.LogError("Rewarded ad failed to open full screen content " +
					               "with error : " + error);
					RestoreGameTiming();
					OnRewardFailedToShow?.Invoke();
					_rewardedAd = null;
					ScheduleRewardReload();
				};
				_rewardedAd = ad;
			});
	}

	/// <summary>
	/// リワード広告を作成
	/// </summary>
	public static void ShowReward()
	{
		FirstSetting();
		if (_rewardedAd != null && _rewardedAd.CanShowAd())
		{
			_rewardedAd.Show((Reward reward) =>
			{
				Debug.Log($"Reward earned. Type: {reward.Type}, Amount: {reward.Amount}");
				RestoreGameTiming();
				OnReward?.Invoke(reward.Amount);
			});
		}
		else
		{
			Debug.LogError("Rewarded ad is not ready yet.");
			LoadReward();
			OnRewardFailedToShow?.Invoke();
		}
	}

	/// <summary>
	/// リワード削除
	/// </summary>
	public static void DestroyReward()
	{
		if (_rewardedAd != null)
		{
			_rewardedAd.Destroy();
		}
	}

	/// <summary>
	/// リワード
	/// </summary>
	/// <returns></returns>
	public static bool IsActiveReward()
	{
		return _rewardedAd != null && _rewardedAd.CanShowAd();
	}

	public static void MarkResumeInterstitialPending()
	{
		PlayerPrefs.SetInt(ResumeInterstitialPendingKey, 1);
		PlayerPrefs.Save();
	}

	public static bool ConsumeResumeInterstitialPending()
	{
		if (PlayerPrefs.GetInt(ResumeInterstitialPendingKey, 0) == 0)
		{
			return false;
		}

		if (_returnedToForegroundInCurrentProcess)
		{
			return false;
		}

		PlayerPrefs.DeleteKey(ResumeInterstitialPendingKey);
		PlayerPrefs.Save();
		return true;
	}

	public static bool HasResumeInterstitialPending()
	{
		if (PlayerPrefs.GetInt(ResumeInterstitialPendingKey, 0) == 0)
		{
			return false;
		}

		return !_returnedToForegroundInCurrentProcess;
	}

	public static void ClearResumeInterstitialPending()
	{
		if (!PlayerPrefs.HasKey(ResumeInterstitialPendingKey))
		{
			return;
		}

		PlayerPrefs.DeleteKey(ResumeInterstitialPendingKey);
		PlayerPrefs.Save();
	}

	public static void NotifyReturnedToForegroundInCurrentProcess()
	{
		_returnedToForegroundInCurrentProcess = true;
		RestoreGameTiming();
	}

	private static async void ScheduleInterstitialReload()
	{
		if (_isInterstitialReloadScheduled)
		{
			return;
		}

		_isInterstitialReloadScheduled = true;
		_interstitialRetryCount = Mathf.Min(_interstitialRetryCount + 1, 6);
		var delayMs = Mathf.Min(1000 * (1 << (_interstitialRetryCount - 1)), 30000);
		await Task.Delay(delayMs);
		_isInterstitialReloadScheduled = false;
		InitInterstitial();
	}

	private static async void ScheduleRewardReload()
	{
		if (_isRewardReloadScheduled)
		{
			return;
		}

		_isRewardReloadScheduled = true;
		_rewardRetryCount = Mathf.Min(_rewardRetryCount + 1, 6);
		var delayMs = Mathf.Min(1000 * (1 << (_rewardRetryCount - 1)), 30000);
		await Task.Delay(delayMs);
		_isRewardReloadScheduled = false;
		LoadReward();
	}

	public static void RestoreGameTiming()
	{
		if (Time.timeScale != 1f)
		{
			Time.timeScale = 1f;
		}
		AudioListener.pause = false;
	}
}
