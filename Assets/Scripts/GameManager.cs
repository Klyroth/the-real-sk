using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// GameManager — The real SK
// From tutorial videos 32 (weapon switching), 34 (singleton + score),
// 36 (score UI).
// Adaptations vs tutorial:
//  - `instance` is the tutorial's singleton field (lowercase); the
//    `Instance` property is added so ZombieAI/WaypointZombieAI can use
//    the conventional capitalised name.
//  - AddScore(int) wraps the score increment (tutorial does
//    `GameManager.instance.currentScore += 1` directly in the zombie
//    Dead case; both zombie scripts call AddScore(1) instead).
//  - SwitchWeapon guards the index against the weapons array length
//    (we have 2 weapons, not the tutorial's 4; keys 3/4 do nothing).
//  - Touch weapon switch (Phase 11): TouchControls sets
//    touchWeaponIndex (0-based); -1 means no request.
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static GameManager Instance { get { return instance; } }

    public GameObject[] weapons;
    public int highScore;
    public int currentScore;

    public Text highScoreText;
    public Text currentScoreText;

    [SerializeField] private int currentWeaponIndex = 0;

    // Android touch weapon switch (written by TouchControls; -1 = none)
    public static int touchWeaponIndex = -1;

    void Start()
    {
        instance = this;
        SwitchWeapon(currentWeaponIndex);
    }

    void Update()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
        }

        if (highScoreText != null)
        {
            highScoreText.text = highScore.ToString();
        }
        if (currentScoreText != null)
        {
            currentScoreText.text = currentScore.ToString();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchWeapon(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchWeapon(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchWeapon(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SwitchWeapon(3);
        }

        if (touchWeaponIndex >= 0)
        {
            SwitchWeapon(touchWeaponIndex);
            touchWeaponIndex = -1;
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
    }

    void SwitchWeapon(int newIndex)
    {
        if (weapons == null || newIndex < 0 || newIndex >= weapons.Length)
        {
            return;
        }

        weapons[currentWeaponIndex].SetActive(false);
        weapons[newIndex].SetActive(true);
        currentWeaponIndex = newIndex;
    }
}
