using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// DeathScreen — The real SK
// From tutorial video 39. When the player dies, a black overlay and
// red "YOU DIED" text fade in over 5 seconds, then the game freezes.
// The tutorial's public field name `showDeadScreen` is kept (PlayerMovement
// sets it); the private typo `targetAplha` is corrected to `targetAlpha`.
public class DeathScreen : MonoBehaviour
{
    public Image targetImage;
    public Text targetText;
    public float duration = 5f;
    public bool showDeadScreen = false;
    private float targetAlpha = 1f;
    private float startAlpha;
    private float elapsedTime = 0f;

    void Start()
    {
        startAlpha = targetImage.color.a;
    }

    void Update()
    {
        if (showDeadScreen)
        {
            if (elapsedTime < duration)
            {
                float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);

                Color newColor = targetImage.color;
                newColor.a = newAlpha;
                targetImage.color = newColor;

                Color newTextAlpha = targetText.color;
                newTextAlpha.a = newAlpha;
                targetText.color = newTextAlpha;

                elapsedTime += Time.deltaTime;
            }
            else
            {
                Time.timeScale = 0;
            }
        }
    }
}
