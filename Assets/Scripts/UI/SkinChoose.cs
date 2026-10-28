using UnityEngine;
using UnityEngine.UI;

public class SkinChoose : MonoBehaviour
{
    private int currentSkin = 1;
    [SerializeField] private int maxSkins = 4;

    [SerializeField] private Sprite skin1;
    [SerializeField] private Sprite skin2;
    [SerializeField] private Sprite skin3;
    [SerializeField] private Sprite skin4;

    [SerializeField] private Image skinShow;

    private void Awake()
    {
        currentSkin = 1;
        skinShow.sprite = skin1;
    }

    public void NextSkin()
    {
        currentSkin++;

        if (currentSkin > maxSkins)
        {
            currentSkin = 1;
        }

        switch (currentSkin)
        {
            case 1:
                skinShow.sprite = skin1;
                break;
            case 2:
                skinShow.sprite = skin2;
                break;
            case 3:
                skinShow.sprite = skin3;
                break;
            case 4:
                skinShow.sprite = skin4;
                break;
        }

        NetworkManager.Instance.ChangeLocalSkin(currentSkin);
    }

    public void PrevSkin()
    {
        currentSkin--;

        if (currentSkin < 1)
        {
            currentSkin = maxSkins;
        }

        switch (currentSkin)
        {
            case 1:
                skinShow.sprite = skin1;
                break;
            case 2:
                skinShow.sprite = skin2;
                break;
            case 3:
                skinShow.sprite = skin3;
                break;
            case 4:
                skinShow.sprite = skin4;
                break;
        }

        NetworkManager.Instance.ChangeLocalSkin(currentSkin);
    }
}
