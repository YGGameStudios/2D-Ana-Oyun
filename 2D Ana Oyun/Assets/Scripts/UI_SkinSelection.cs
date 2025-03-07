using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SkinSelection : MonoBehaviour
{
    [SerializeField] private int currentIndex;
    [SerializeField] private int maxIndex;
    [SerializeField] private Animator skinDisplay;

    public void NextSkin()
    {
        currentIndex++;
        if (currentIndex > maxIndex)
            currentIndex = 0;

        UpdateSkinDisplay();
    }

    public void PreviousSkin()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = maxIndex;

        UpdateSkinDisplay();
    }

    private void UpdateSkinDisplay()
    {
        for (int i = 0; i <= maxIndex; i++)
        {
            skinDisplay.SetLayerWeight(i, 0f);
        }

        skinDisplay.SetLayerWeight(currentIndex, 1f);
    }

    public void SelectSkin() => SkinManager.instance.SetSkinId(currentIndex);
}
