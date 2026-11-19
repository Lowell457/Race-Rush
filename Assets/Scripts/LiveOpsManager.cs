using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class LiveOpsManager : MonoBehaviour
{
    public static LiveOpsManager instance;

    private float boostDuration;
    private float boostMult;
    private float dashCooldown;
    private bool turboWeek;
    private bool extremeDash;

    private float dashDuration;
    private float dashSpeed;
    private float pushForce;
    private float pushRadius;
    private float stunDuration;

    public float BoostDuration => boostDuration;
    public float BoostMult => boostMult;
    public float DashCooldown => dashCooldown;
    public bool TurboWeek => turboWeek;
    public bool ExtremeDash => extremeDash;

    public float DashDuration => dashDuration;
    public float DashSpeed => dashSpeed;
    public float PushForce => pushForce;
    public float PushRadius => pushRadius;
    public float StunDuration => stunDuration;

    private void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            await LoadConfigAsync();
        }
        catch (Exception e) { Debug.LogError(e); }
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            await RemoteConfigService.Instance.FetchConfigsAsync(
                new UserAttributes(), new AppAttributes());

            var config = RemoteConfigService.Instance.appConfig;

            boostDuration = config.GetFloat("boostDuration", 1f);
            boostMult = config.GetFloat("boostMult", 1f);
            dashCooldown = config.GetFloat("dashCooldown", 1f);
            extremeDash = config.GetBool("extremeDash", false);
            turboWeek = config.GetBool("turboWeek", false);

            dashDuration = config.GetFloat("dashDuration", 1f);
            dashSpeed = config.GetFloat("dashSpeed", 1f);
            pushForce = config.GetFloat("pushForce", 1f);
            pushRadius = config.GetFloat("pushRadius", 1f);
            stunDuration = config.GetFloat("stunDuration", 1f);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Remote Config no disponible: {e.Message}");
        }
    }

    public struct AppAttributes
    {
        public float TestValue;
    }

    public struct UserAttributes
    {
        
    }
}
