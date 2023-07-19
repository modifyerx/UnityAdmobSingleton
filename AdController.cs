using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using System;

public enum BannerSize
{
    Banner320x50,
    MediumRectangle300x250,
    IABBanner468x60,
    Leaderboard728x90,
    AnchoredAdaptive
}
[Serializable]
public struct MobileBanner
{
    public AdPosition BannerPosition;
    public BannerSize BannerSize;
    public string AdName;
    public string AdId;
    public BannerView BannerAd;

    public MobileBanner(MobileBanner mobileBanner, BannerView bannerAd)
    {
        BannerPosition = mobileBanner.BannerPosition;
        BannerSize = mobileBanner.BannerSize;
        AdName = mobileBanner.AdName;
        AdId = mobileBanner.AdId;
        BannerAd = bannerAd;
    }
    public void DeleteAd()
    {
        BannerAd.Destroy();
        BannerAd = null;
    }
}
[Serializable]
public struct MobileInterstitial
{
    public string AdName;
    public string AdId;
    public InterstitialAd Interstitial;

    public MobileInterstitial(MobileInterstitial mobileInterstitial, InterstitialAd interstitial)
    {
        AdName = mobileInterstitial.AdName;
        AdId = mobileInterstitial.AdId;
        Interstitial = interstitial;
    }
    public void DeleteAd()
    {
        Interstitial.Destroy();
        Interstitial = null;
    }
}
[Serializable]
public struct MobileRewarded
{
    public string AdName;
    public string AdId;
    public RewardedAd Rewarded;

    public MobileRewarded(MobileRewarded mobileInterstitial, RewardedAd rewarded)
    {
        AdName = mobileInterstitial.AdName;
        AdId = mobileInterstitial.AdId;
        Rewarded = rewarded;
    }
    public void DeleteAd()
    {
        Rewarded.Destroy();
        Rewarded = null;
    }
}
[Serializable]
public struct MobileRewardedInterstitial
{
    public string AdName;
    public string AdId;
    public RewardedInterstitialAd RewardedInterstitial;

    public MobileRewardedInterstitial(MobileRewardedInterstitial mobileRewardedInterstitial, RewardedInterstitialAd rewardedInterstitial)
    {
        AdName = mobileRewardedInterstitial.AdName;
        AdId = mobileRewardedInterstitial.AdId;
        RewardedInterstitial = rewardedInterstitial;
    }
    public void DeleteAd()
    {
        RewardedInterstitial.Destroy();
        RewardedInterstitial = null;
    }
}
public class AdController : MonoBehaviour
{
    private static AdController instance = null;

    [Header("Ads Lists")]
    public List<MobileBanner> MobileBannerList;
    public List<MobileInterstitial> MobileInterstitialList;
    public List<MobileRewarded> MobileRewardedList;
    public List<MobileRewardedInterstitial> MobileRewardedInterstitialList;

    [Header("Test Mode")]
    public bool TestMode = false;
    public string TestDeviceId = "";

    [Header("COPPA")]
    public bool SetTagForChildren = false;

    private float InterstitialTimeOutTime;
    private float RewardedTimeOutTime;
    private float RewardedInterstitalTimeOutTime;

    private float BannerAutoNewRequestTime = float.PositiveInfinity;
    private float InterstitialAutoNewRequestTime = float.PositiveInfinity;
    private float RewardedVideoAutoNewRequestTime = float.PositiveInfinity;
    private float RewardedInterstitialAutoNewRequestTime = float.PositiveInfinity;

    private bool AnyBanner = false;
    private bool AnyInterstitial = false;
    private bool AnyRewarded = false;
    private bool AnyRewardedInterstitial = false;

    public delegate void RewardedVideoReward(Reward reward);
    private RewardedVideoReward RewardDelegate;

    public delegate void RewardedInterstitialVideoReward(Reward reward);
    private RewardedInterstitialVideoReward RewardInterstitialDelegate;

    private void Awake()
    {
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);

            for (int i = 0; i < MobileBannerList.Count; i++)
            {
                if (string.IsNullOrEmpty(MobileBannerList[i].AdId))
                {
                    Debug.Log("Empty BannerView Ad ID.");
                }
                AnyBanner = true;
            }
            for (int i = 0; i < MobileInterstitialList.Count; i++)
            {
                if (string.IsNullOrEmpty(MobileInterstitialList[i].AdId))
                {
                    Debug.Log("Empty Interstitial Ad ID.");
                }
                AnyInterstitial = true;
            }
            for (int i = 0; i < MobileRewardedList.Count; i++)
            {
                if (string.IsNullOrEmpty(MobileRewardedList[i].AdId))
                {
                    Debug.Log("Empty Rewarded Ad ID.");
                }
                AnyRewarded = true;
            }
            for (int i = 0; i < MobileRewardedInterstitialList.Count; i++)
            {
                if (string.IsNullOrEmpty(MobileRewardedInterstitialList[i].AdId))
                {
                    Debug.Log("Empty RewardedInterstitial Ad ID.");
                }
                AnyRewardedInterstitial = true;
            }

            RequestConfiguration adConfig = new RequestConfiguration();
            
            if (TestMode && !string.IsNullOrEmpty(TestDeviceId))
            {
                adConfig.TestDeviceIds.Add(TestDeviceId);
            }
            if (SetTagForChildren)
            {
                adConfig.TagForChildDirectedTreatment = TagForChildDirectedTreatment.True;
            }
            else
            {
                adConfig.TagForChildDirectedTreatment = TagForChildDirectedTreatment.False;
            }

            MobileAds.SetRequestConfiguration(adConfig);

            MobileAds.Initialize(initStatus => 
            {
                if (initStatus == null)
                {
                    Debug.LogError("Google Mobile Ads initialization failed.");
                    return;
                }

                // If you use mediation, you can check the status of each adapter.
                var adapterStatusMap = initStatus.getAdapterStatusMap();
                if (adapterStatusMap != null)
                {
                    foreach (var item in adapterStatusMap)
                    {
                        Debug.Log(string.Format("Adapter {0} is {1}",
                            item.Key,
                            item.Value.InitializationState));
                    }
                }

                Debug.Log("Google Mobile Ads initialization complete.");
                GetAllBannerAds();
                GetAllInterstitialAds();
                GetAllRewardedAds();
                GetAllRewardedInterstitialAds();
            });
        }
        else if (this != instance)
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        float time = Time.realtimeSinceStartup;

        if (time >= BannerAutoNewRequestTime)
        {
            BannerAutoNewRequestTime = float.PositiveInfinity;
            GetAllBannerAds();
        }
        if (time >= InterstitialAutoNewRequestTime)
        {
            InterstitialAutoNewRequestTime = float.PositiveInfinity;
            GetAllInterstitialAds();
        }
        if (time >= RewardedVideoAutoNewRequestTime)
        {
            RewardedVideoAutoNewRequestTime = float.PositiveInfinity;
            GetAllRewardedAds();
        }
        if (time >= RewardedInterstitialAutoNewRequestTime)
        {
            RewardedInterstitialAutoNewRequestTime = float.PositiveInfinity;
            GetAllRewardedInterstitialAds();
        }
    }

    private AdRequest CreateAdRequest()
    {
        return new AdRequest();
    }

    // Banner Section
    public static void GetAllBannerAds()
    {
        if (!instance.TestMode && !instance.AnyBanner)
        {
            return;
        }

        for (int i = 0; i < instance.MobileBannerList.Count; i++)
        {
            if (instance.MobileBannerList[i].BannerAd != null)
            {
                instance.MobileBannerList[i].BannerAd.Destroy();
            }
        }

        for (int i = 0; i < instance.MobileBannerList.Count; i++)
        {
            if (string.IsNullOrEmpty(instance.MobileBannerList[i].AdId))
            {
                Debug.Log("Empty BannerView Ad ID.");
            }

            AdSize BannerViewSize;
            if (instance.MobileBannerList[i].BannerSize == BannerSize.Banner320x50)
            {
                BannerViewSize = AdSize.Banner;
            }
            else if (instance.MobileBannerList[i].BannerSize == BannerSize.MediumRectangle300x250)
            {
                BannerViewSize = AdSize.MediumRectangle;
            }
            else if (instance.MobileBannerList[i].BannerSize == BannerSize.IABBanner468x60)
            {
                BannerViewSize = AdSize.IABBanner;
            }
            else if (instance.MobileBannerList[i].BannerSize == BannerSize.Leaderboard728x90)
            {
                BannerViewSize = AdSize.Leaderboard;
            }
            else
            {
                BannerViewSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            }

            BannerView adObject;
            if (instance.TestMode)
            {
#if UNITY_ANDROID
                adObject = new BannerView("ca-app-pub-3940256099942544/6300978111", BannerViewSize, instance.MobileBannerList[i].BannerPosition);
#elif UNITY_IPHONE
                adObject = new BannerView("ca-app-pub-3940256099942544/2934735716", BannerViewSize, instance.MobileBannerList[i].BannerPosition);
#endif
            }
            else
            {
                adObject = new BannerView(instance.MobileBannerList[i].AdId, BannerViewSize, instance.MobileBannerList[i].BannerPosition);
            }

            adObject.OnBannerAdLoadFailed += instance.BannerLoadFailed;
            adObject.OnBannerAdLoaded += () =>
            {
                Debug.Log("Banner loaded and hided");
                adObject.Hide();
            };
            adObject.LoadAd(new AdRequest());
            instance.MobileBannerList[i] = new MobileBanner(instance.MobileBannerList[i], adObject);
        }
    }
    public static void GetBannerAd(string bannerName)
    {
        if (instance == null)
        {
            return;
        }

        int bannerIndex = instance.MobileBannerList.FindIndex(x => x.AdName == bannerName);

        if (string.IsNullOrEmpty(instance.MobileBannerList[bannerIndex].AdId))
        {
            Debug.Log("Empty BannerView Ad ID.");
        }

        AdSize BannerViewSize;
        if (instance.MobileBannerList[bannerIndex].BannerSize == BannerSize.Banner320x50)
        {
            BannerViewSize = AdSize.Banner;
        }
        else if (instance.MobileBannerList[bannerIndex].BannerSize == BannerSize.MediumRectangle300x250)
        {
            BannerViewSize = AdSize.MediumRectangle;
        }
        else if (instance.MobileBannerList[bannerIndex].BannerSize == BannerSize.IABBanner468x60)
        {
            BannerViewSize = AdSize.IABBanner;
        }
        else if (instance.MobileBannerList[bannerIndex].BannerSize == BannerSize.Leaderboard728x90)
        {
            BannerViewSize = AdSize.Leaderboard;
        }
        else
        {
            BannerViewSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        }

        BannerView adObject;
        if (instance.TestMode)
        {
#if UNITY_ANDROID
            adObject = new BannerView("ca-app-pub-3940256099942544/6300978111", BannerViewSize, instance.MobileBannerList[bannerIndex].BannerPosition);
#else
            adObject = new BannerView("ca-app-pub-3940256099942544/2934735716", BannerViewSize, instance.MobileBannerList[bannerIndex].BannerPosition);
#endif
        }
        else
        {
            adObject = new BannerView(instance.MobileBannerList[bannerIndex].AdId, BannerViewSize, instance.MobileBannerList[bannerIndex].BannerPosition);
        }
        adObject.OnBannerAdLoadFailed += instance.BannerLoadFailed;
        adObject.LoadAd(instance.CreateAdRequest());
        adObject.Hide();
        instance.MobileBannerList[bannerIndex] = new MobileBanner(instance.MobileBannerList[bannerIndex], adObject);
    }
    private void BannerLoadFailed(LoadAdError args)
    {
        Debug.LogError(args.ToString());
        string responseId = args.GetResponseInfo().GetResponseId();
        for (int i = 0; i < instance.MobileBannerList.Count; i++)
        {
            if (responseId == instance.MobileBannerList[i].BannerAd.GetResponseInfo().GetResponseId())
            {
                BannerAutoNewRequestTime = Time.realtimeSinceStartup + 30f;

                if (instance.MobileBannerList[i].BannerAd != null)
                {
                    instance.MobileBannerList[i].DeleteAd();
                }
            }
        }
    }
    public static void ShowBanner(string bannerName)
    {
        if (instance == null)
        {
            return;
        }

        int bannerIndex = instance.MobileBannerList.FindIndex(x => x.AdName == bannerName);
        if (instance.MobileBannerList[bannerIndex].BannerAd == null)
        {
            GetBannerAd(bannerName);
            if (instance.MobileBannerList[bannerIndex].BannerAd == null)
            {
                return;
            }
        }

        instance.MobileBannerList[bannerIndex].BannerAd.Show();
    }
    public static void HideBanner(string bannerName)
    {
        if (instance == null)
        {
            return;
        }

        int bannerIndex = instance.MobileBannerList.FindIndex(x => x.AdName == bannerName);
        if (instance.MobileBannerList[bannerIndex].BannerAd == null)
        {
            return;
        }

        instance.MobileBannerList[bannerIndex].BannerAd.Hide();
    }

    // Interstitial Section
    public static void GetAllInterstitialAds()
    {
        if (!instance.TestMode && !instance.AnyInterstitial)
        {
            return;
        }

        for (int i = 0; i < instance.MobileInterstitialList.Count; i++)
        {
            if (instance.MobileInterstitialList[i].Interstitial != null)
            {
                instance.MobileInterstitialList[i].Interstitial.Destroy();
            }
        }

        for (int i = 0; i < instance.MobileInterstitialList.Count; i++) 
        {
            if (string.IsNullOrEmpty(instance.MobileInterstitialList[i].AdId))
            {
                Debug.Log("Empty Interstitial Ad ID.");
            }

            string interstitialAdId = instance.MobileInterstitialList[i].AdId;
            int interstitialAdIndex = i;
            if (instance.TestMode)
            {
#if UNITY_ANDROID
                interstitialAdId = "ca-app-pub-3940256099942544/1033173712";
#else
                interstitialAdId = "ca-app-pub-3940256099942544/4411468910";
#endif
            }
            InterstitialAd.Load(interstitialAdId, instance.CreateAdRequest(), (InterstitialAd adObject, LoadAdError error) =>
            {
                if (error != null)
                {
                    Debug.LogError("Interstitial ad failed to load an ad with error : " + error);
                    instance.InterstitialLoadFailed(i);
                    return;
                }
                if (adObject == null)
                {
                    Debug.LogError("Unexpected error: Interstitial load event fired with null ad and null error.");
                    instance.InterstitialLoadFailed(i);
                    return;
                }

                instance.MobileInterstitialList[interstitialAdIndex] = new MobileInterstitial(instance.MobileInterstitialList[interstitialAdIndex], adObject);
                adObject.OnAdFullScreenContentClosed += () =>
                {
                    instance.InterstitialAdDelegate(adObject.GetResponseInfo());
                };
            });
        }
    }
    public static void GetInterstitialAd(string interstitialName)
    {
        if (instance == null)
        {
            return;
        }

        int interstitialIndex = instance.MobileInterstitialList.FindIndex(x => x.AdName == interstitialName);

        if (string.IsNullOrEmpty(instance.MobileInterstitialList[interstitialIndex].AdId))
        {
            Debug.Log("Empty Interstitial Ad ID.");
        }

        string interstitialAdId = instance.MobileInterstitialList[interstitialIndex].AdId;
        if (instance.TestMode)
        {
#if UNITY_ANDROID
            interstitialAdId = "ca-app-pub-3940256099942544/1033173712";
#else
            interstitialAdId = "ca-app-pub-3940256099942544/4411468910";
#endif
        }
        InterstitialAd.Load(interstitialAdId, instance.CreateAdRequest(), (InterstitialAd adObject, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("Interstitial ad failed to load an ad with error : " + error);
                instance.InterstitialLoadFailed(interstitialIndex);
                return;
            }
            if (adObject == null)
            {
                Debug.LogError("Unexpected error: Interstitial load event fired with null ad and null error.");
                instance.InterstitialLoadFailed(interstitialIndex);
                return;
            }

            Debug.Log("Interstitial ad loaded with response : " + adObject.GetResponseInfo());
            instance.MobileInterstitialList[interstitialIndex] = new MobileInterstitial(instance.MobileInterstitialList[interstitialIndex], adObject);
            adObject.OnAdFullScreenContentClosed += () =>
            {
                instance.InterstitialAdDelegate(adObject.GetResponseInfo());
            };
        });
        instance.InterstitialTimeOutTime = Time.realtimeSinceStartup + 30f;
    }
    private void InterstitialLoadFailed(int failedAdIndex)
    {
        InterstitialAutoNewRequestTime = Time.realtimeSinceStartup + 30f;
        if (instance.MobileInterstitialList[failedAdIndex].Interstitial != null)
        {
            instance.MobileInterstitialList[failedAdIndex].DeleteAd();
        }
    }
    public static bool InterstitialIsReady(string interstitialName)
    {
        if (instance == null)
        {
            return false;
        }
        if (instance.MobileInterstitialList.Find(x => x.AdName == interstitialName).Interstitial == null)
        {
            return false;
        }
        return instance.MobileInterstitialList.Find(x => x.AdName == interstitialName).Interstitial.CanShowAd();
    }
    public static void ShowInterstitialAd(string interstitialName)
    {
        if (instance == null)
        {
            return;
        }

        int interstitialIndex = instance.MobileInterstitialList.FindIndex(x => x.AdName == interstitialName);
        if (instance.MobileInterstitialList[interstitialIndex].Interstitial == null)
        {
            GetInterstitialAd(interstitialName);
            if (instance.MobileInterstitialList[interstitialIndex].Interstitial == null)
            {
                return;
            }
        }
        if (instance.MobileInterstitialList[interstitialIndex].Interstitial.CanShowAd())
        {
            instance.MobileInterstitialList[interstitialIndex].Interstitial.Show();
        }
        else
        {
            if (instance.InterstitialTimeOutTime <= Time.realtimeSinceStartup)
            {
                GetInterstitialAd(interstitialName);
            }
        }
    }
    private void InterstitialAdDelegate(ResponseInfo info)
    {
        string responseId = info.GetResponseId();
        for (int i = 0; i < MobileInterstitialList.Count; i++)
        {
            if (responseId == MobileInterstitialList[i].Interstitial.GetResponseInfo().GetResponseId())
            {
                InterstitialAutoNewRequestTime = Time.realtimeSinceStartup + 30f;

                if (MobileInterstitialList[i].Interstitial != null)
                {
                    MobileInterstitialList[i].DeleteAd();
                    GetInterstitialAd(MobileInterstitialList[i].AdName);
                }
            }
        }
    }

    // Rewarded Section
    public static void GetAllRewardedAds()
    {
        if (!instance.TestMode && !instance.AnyRewarded)
        {
            return;
        }

        for (int i = 0; i < instance.MobileRewardedList.Count; i++)
        {
            if (instance.MobileRewardedList[i].Rewarded != null)
            {
                instance.MobileRewardedList[i].Rewarded.Destroy();
            }
        }

        for (int i = 0; i < instance.MobileRewardedList.Count; i++)
        {
            if (string.IsNullOrEmpty(instance.MobileRewardedList[i].AdId))
            {
                Debug.Log("Empty Rewarded Ad ID.");
            }

            string rewardedAdId = instance.MobileRewardedList[i].AdId;
            int rewardedAdIndex = i;
            if (instance.TestMode)
            {
#if UNITY_ANDROID
                rewardedAdId = "ca-app-pub-3940256099942544/5224354917";
#else
                rewardedAdId = "ca-app-pub-3940256099942544/1712485313";
#endif
            }
            RewardedAd.Load(rewardedAdId, instance.CreateAdRequest(), (RewardedAd adObject, LoadAdError error) =>
            {
                if (error != null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                    instance.RewardedLoadFailed(i);
                    return;
                }
                if (adObject == null)
                {
                    Debug.LogError("Unexpected error: Rewarded load event fired with null ad and null error.");
                    instance.RewardedLoadFailed(i);
                    return;
                }

                Debug.Log("Rewarded ad loaded with response : " + adObject.GetResponseInfo());
                instance.MobileRewardedList[rewardedAdIndex] = new MobileRewarded(instance.MobileRewardedList[rewardedAdIndex], adObject);
                adObject.OnAdFullScreenContentClosed += () =>
                {
                    instance.RewardedAdDelegate(adObject.GetResponseInfo());
                };
            });
        }
    }
    public static void GetRewardedAd(string rewardedName)
    {
        if (instance == null)
        {
            return;
        }

        int rewardedIndex = instance.MobileRewardedList.FindIndex(x => x.AdName == rewardedName);

        if (string.IsNullOrEmpty(instance.MobileRewardedList[rewardedIndex].AdId))
        {
            Debug.Log("Empty Rewarded Ad ID.");
        }

        string rewardedAdId = instance.MobileRewardedList[rewardedIndex].AdId;
        if (instance.TestMode)
        {
#if UNITY_ANDROID
            rewardedAdId = "ca-app-pub-3940256099942544/5224354917";
#else
            rewardedAdId = "ca-app-pub-3940256099942544/1712485313";
#endif
        }
        RewardedAd.Load(rewardedAdId, instance.CreateAdRequest(), (RewardedAd adObject, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                instance.RewardedLoadFailed(rewardedIndex);
                return;
            }
            if (adObject == null)
            {
                Debug.LogError("Unexpected error: Rewarded load event fired with null ad and null error.");
                instance.RewardedLoadFailed(rewardedIndex);
                return;
            }

            Debug.Log("Rewarded ad loaded with response : " + adObject.GetResponseInfo());
            instance.MobileRewardedList[rewardedIndex] = new MobileRewarded(instance.MobileRewardedList[rewardedIndex], adObject);
            adObject.OnAdFullScreenContentClosed += () =>
            {
                instance.RewardedAdDelegate(adObject.GetResponseInfo());
            };
        });
        instance.RewardedTimeOutTime = Time.realtimeSinceStartup + 30f;
    }
    private void RewardedLoadFailed(int failedAdIndex)
    {
        RewardedTimeOutTime = Time.realtimeSinceStartup + 30f;
        if (instance.MobileRewardedList[failedAdIndex].Rewarded != null)
        {
            instance.MobileRewardedList[failedAdIndex].DeleteAd();
        }
    }
    public static bool RewardedIsReady(string rewardedName)
    {
        if (instance == null)
        {
            return false;
        }
        if (instance.MobileRewardedList.Find(x => x.AdName == rewardedName).Rewarded == null)
        {
            return false;
        }
        return instance.MobileRewardedList.Find(x => x.AdName == rewardedName).Rewarded.CanShowAd();
    }
    public static void ShowRewardedAd(string rewardedName, RewardedVideoReward rewardMethod)
    {
        if (instance == null)
        {
            return;
        }

        int rewardedIndex = instance.MobileRewardedList.FindIndex(x => x.AdName == rewardedName);
        instance.RewardDelegate = rewardMethod;
        if (instance.MobileRewardedList[rewardedIndex].Rewarded == null)
        {
            GetRewardedAd(rewardedName);
            if (instance.MobileRewardedList[rewardedIndex].Rewarded == null)
            {
                return;
            }
        }
        if (instance.MobileRewardedList[rewardedIndex].Rewarded.CanShowAd())
        {
            instance.MobileRewardedList[rewardedIndex].Rewarded.Show((Reward reward) =>
            {
                instance.RewardDelegate(reward);
                instance.RewardDelegate = null;
            });
        }
        else
        {
            if (instance.RewardedTimeOutTime <= Time.realtimeSinceStartup)
            {
                GetRewardedAd(rewardedName);
            }
        }
    }
    private void RewardedAdDelegate(ResponseInfo info)
    {
        string responseId = info.GetResponseId();
        for (int i = 0; i < MobileRewardedList.Count; i++)
        {
            if (responseId == MobileRewardedList[i].Rewarded.GetResponseInfo().GetResponseId())
            {
                RewardedVideoAutoNewRequestTime = Time.realtimeSinceStartup + 30f;

                if (MobileRewardedList[i].Rewarded != null)
                {
                    MobileRewardedList[i].DeleteAd();
                    GetRewardedAd(MobileRewardedList[i].AdName);
                }
            }
        }
    }

    // Rewarded Interstitial Section
    public static void GetAllRewardedInterstitialAds()
    {
        if (!instance.TestMode && !instance.AnyRewardedInterstitial)
        {
            return;
        }

        for (int i = 0; i < instance.MobileRewardedInterstitialList.Count; i++)
        {
            if (instance.MobileRewardedInterstitialList[i].RewardedInterstitial != null)
            {
                instance.MobileRewardedInterstitialList[i].RewardedInterstitial.Destroy();
            }
        }

        for (int i = 0; i < instance.MobileRewardedInterstitialList.Count; i++)
        {
            if (string.IsNullOrEmpty(instance.MobileRewardedInterstitialList[i].AdId))
            {
                Debug.Log("Empty RewardedInterstitial Ad ID.");
            }

            string rewardedInterstitialAdId = instance.MobileRewardedInterstitialList[i].AdId;
            int rewardedInterstitialAdIndex = i;
            if (instance.TestMode)
            {
#if UNITY_ANDROID
                rewardedInterstitialAdId = "ca-app-pub-3940256099942544/5354046379";
#else
                rewardedInterstitialAdId = "ca-app-pub-3940256099942544/6978759866";
#endif
            }
            RewardedInterstitialAd.Load(rewardedInterstitialAdId, instance.CreateAdRequest(), (RewardedInterstitialAd adObject, LoadAdError error) =>
            {
                if (error != null)
                {
                    Debug.LogError("RewardedInterstitial ad failed to load an ad with error : " + error);
                    instance.RewardedInterstitialLoadFailed(i);
                    return;
                }
                if (adObject == null)
                {
                    Debug.LogError("Unexpected error: RewardedInterstitial load event fired with null ad and null error.");
                    instance.RewardedInterstitialLoadFailed(i);
                    return;
                }

                Debug.Log("RewardedInterstitial ad loaded with response : " + adObject.GetResponseInfo());
                instance.MobileRewardedInterstitialList[rewardedInterstitialAdIndex] = new MobileRewardedInterstitial(instance.MobileRewardedInterstitialList[rewardedInterstitialAdIndex], adObject);
                adObject.OnAdFullScreenContentClosed += () =>
                {
                    instance.RewardedInterstitialDelegate(adObject.GetResponseInfo());
                };
            });
        }
    }
    public static void GetRewardedInterstitialAd(string rewardedInterstitialName)
    {
        if (instance == null)
        {
            return;
        }

        int rewardedInterstitialIndex = instance.MobileRewardedInterstitialList.FindIndex(x => x.AdName == rewardedInterstitialName);

        if (string.IsNullOrEmpty(instance.MobileRewardedInterstitialList[rewardedInterstitialIndex].AdId))
        {
            Debug.Log("Empty RewardedInterstitial Ad ID.");
        }

        string rewardedInterstitialAdId = instance.MobileRewardedInterstitialList[rewardedInterstitialIndex].AdId;
        if (instance.TestMode)
        {
#if UNITY_ANDROID
            rewardedInterstitialAdId = "ca-app-pub-3940256099942544/5224354917";
#else
            rewardedInterstitialAdId = "ca-app-pub-3940256099942544/1712485313";
#endif
        }
        RewardedInterstitialAd.Load(rewardedInterstitialAdId, instance.CreateAdRequest(), (RewardedInterstitialAd adObject, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("RewardedInterstitial ad failed to load an ad with error : " + error);
                instance.RewardedInterstitialLoadFailed(rewardedInterstitialIndex);
                return;
            }
            if (adObject == null)
            {
                Debug.LogError("Unexpected error: RewardedInterstitial load event fired with null ad and null error.");
                instance.RewardedInterstitialLoadFailed(rewardedInterstitialIndex);
                return;
            }

            Debug.Log("RewardedInterstitial ad loaded with response : " + adObject.GetResponseInfo());
            instance.MobileRewardedInterstitialList[rewardedInterstitialIndex] = new MobileRewardedInterstitial(instance.MobileRewardedInterstitialList[rewardedInterstitialIndex], adObject);
            adObject.OnAdFullScreenContentClosed += () =>
            {
                instance.RewardedInterstitialDelegate(adObject.GetResponseInfo());
            };
        });
        instance.RewardedInterstitalTimeOutTime = Time.realtimeSinceStartup + 30f;
    }
    private void RewardedInterstitialLoadFailed(int failedAdIndex)
    {
        RewardedInterstitalTimeOutTime = Time.realtimeSinceStartup + 30f;
        if (instance.MobileRewardedInterstitialList[failedAdIndex].RewardedInterstitial != null)
        {
            instance.MobileRewardedInterstitialList[failedAdIndex].DeleteAd();
        }
    }
    public static bool RewardedInterstitialIsReady(string rewardedInterstitialName)
    {
        if (instance == null)
        {
            return false;
        }
        if (instance.MobileRewardedInterstitialList.Find(x => x.AdName == rewardedInterstitialName).RewardedInterstitial == null)
        {
            return false;
        }
        return instance.MobileRewardedInterstitialList.Find(x => x.AdName == rewardedInterstitialName).RewardedInterstitial.CanShowAd();
    }
    public static void ShowRewardedInterstitialAd(string rewardedInterstitialName, RewardedInterstitialVideoReward rewardMethod)
    {
        if (instance == null)
        {
            return;
        }

        int rewardedIndex = instance.MobileRewardedInterstitialList.FindIndex(x => x.AdName == rewardedInterstitialName);
        instance.RewardInterstitialDelegate = rewardMethod;
        if (instance.MobileRewardedInterstitialList[rewardedIndex].RewardedInterstitial == null)
        {
            GetRewardedInterstitialAd(rewardedInterstitialName);
            if (instance.MobileRewardedInterstitialList[rewardedIndex].RewardedInterstitial == null)
            {
                return;
            }
        }
        if (instance.MobileRewardedInterstitialList[rewardedIndex].RewardedInterstitial.CanShowAd())
        {
            instance.MobileRewardedInterstitialList[rewardedIndex].RewardedInterstitial.Show((Reward reward) =>
            {
                instance.RewardInterstitialDelegate(reward);
                instance.RewardInterstitialDelegate = null;
            });
        }
        else
        {
            if (instance.RewardedInterstitalTimeOutTime <= Time.realtimeSinceStartup)
            {
                GetRewardedInterstitialAd(rewardedInterstitialName);
            }
        }
    }
    private void RewardedInterstitialDelegate(ResponseInfo info)
    {
        string responseId = info.GetResponseId();
        for (int i = 0; i < MobileRewardedInterstitialList.Count; i++)
        {
            if (responseId == MobileRewardedInterstitialList[i].RewardedInterstitial.GetResponseInfo().GetResponseId())
            {
                RewardedInterstitalTimeOutTime = Time.realtimeSinceStartup + 30f;

                if (MobileRewardedInterstitialList[i].RewardedInterstitial != null)
                {
                    MobileRewardedInterstitialList[i].DeleteAd();
                    GetRewardedInterstitialAd(MobileRewardedInterstitialList[i].AdName);
                }
            }
        }
    }
}
