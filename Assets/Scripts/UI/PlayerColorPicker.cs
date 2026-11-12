using UnityEngine;
using UnityEngine.UI;

public class PlayerColorPicker : MonoBehaviour
{

    [SerializeField] private Color[] color;
    private int header = 0;

    private Image colorDisplay;

    private void Awake()
    {
        colorDisplay = GetComponent<Image>();
        colorDisplay.color = color[header];
    }

    public void NextColor()
    {
        if (header == color.Length -1) 
        {
            header = 0;
        }
        else header++;
        colorDisplay.color = color[header];
    }

    public void PrevColor()
    {
        if (header == 0)
        {
            header = color.Length;
        }
        else header--;
        colorDisplay.color = color[header];
    }
}
