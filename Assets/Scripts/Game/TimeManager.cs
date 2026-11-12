using Fusion;
using System.Collections;
using TMPro;
using UnityEngine;

public class TimeManager : NetworkBehaviour
{

    [Header("Start Countdown")]
    [SerializeField] private TextMeshProUGUI countDownText;
    [SerializeField] private Timer countDownTimer;
    [SerializeField] private float countDownTime;
    [SerializeField] private float maxSize;
    [SerializeField] private float minSize;
    [SerializeField] private GameObject panel;

    [Header("Time")]
    [SerializeField] private Timer levelTimer;
    [SerializeField] private float maxTime;
    [SerializeField] private TextMeshProUGUI timerText;
    private bool countTime = true;


    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        RPC_StartCountDown();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartCountDown()
    {
        StartCoroutine(CountDown());
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!countTime)
        {
            if (!levelTimer.IsRunning)
            {
                levelTimer.Start();
                timerText.gameObject.SetActive(true);
            }
                

            float realTime = maxTime - (float)levelTimer.ElapsedInSeconds;

            timerText.text = (((float)(int)(realTime * 10f)) / 10f).ToString();

            if (realTime <= 0f)
            {
                //EndRace
            }
        }
    }

    private IEnumerator CountDown()
    {
        countDownTimer.Start();
        panel.SetActive(true);
        timerText.gameObject.SetActive(false);

        while (countTime)
        {
            float realTime = countDownTime - (float)countDownTimer.ElapsedInSeconds;

            int seconds = (int)realTime;

            countDownText.text = (seconds + 1).ToString();

            countDownText.fontSize = Mathf.Lerp(minSize, maxSize, realTime - seconds);

            if (realTime < 0)
                countTime = false;

            switch (seconds)
            {
                default:
                    countDownText.color = Color.red; 
                    break;
                case 3:
                    countDownText.color = Color.red;
                    break;
                case 2:
                    countDownText.color = Color.red;
                    break;
                case 1:
                    countDownText.color = Color.yellow;
                    break;
                case 0:
                    countDownText.color = Color.green;
                    break;
            }

            yield return null;
        }
        panel.SetActive(false);

        countDownText.text = "GO";
        countDownText.color = Color.green;
        countDownText.fontSize = maxSize;
        NetworkManager.Instance.StartRace?.Invoke();
        yield return new WaitForSeconds(1f);
        countDownTimer.Stop();
        countDownText.gameObject.SetActive(false);

        yield return null;
    }
}
