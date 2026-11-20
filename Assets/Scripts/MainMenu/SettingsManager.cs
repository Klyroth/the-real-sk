using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// SettingsManager — The real SK (main menu + settings + map select)
// From tutorial videos 44 (difficulty), 45 (save/load difficulty),
// 46 (save/load mouse settings), 48 (map select + Continue).
// The menu UI itself is built with uGUI + TextMeshPro (the tutorial's
// SlimUI "3D Modern Menu UI" asset is NOT used in this build).
// Adaptations vs tutorial:
//  - SelectMap() saves the choice immediately AND calls
//    PlayerPrefs.Save() (the tutorial is unclear whether Save runs on
//    select; saving explicitly is the correct behaviour).
//  - selectedMap defaults to "Map1" so Continue always has a scene.
public class SettingsManager : MonoBehaviour
{
    public Button normalDifficultyButton;
    public Button hardDifficultyButton;
    public Button saveButton;
    public Button map1Button;
    public Button map2Button;
    public Button continueButton;
    public Slider smoothSpeedSlider;
    public Slider sensitivitySlider;

    [SerializeField] private float timeBetweenWaves;
    [SerializeField] private int zombiePerWave;
    [SerializeField] private float smoothSpeed;
    [SerializeField] private float sensitivity;
    [SerializeField] private string selectedMap = "Map1";

    void Start()
    {
        smoothSpeedSlider.minValue = 1;
        smoothSpeedSlider.maxValue = 10;
        sensitivitySlider.minValue = 1;
        sensitivitySlider.maxValue = 5;

        timeBetweenWaves = 10f;
        zombiePerWave = 4;
        smoothSpeed = 10f;
        sensitivity = 2f;

        LoadSettings();

        normalDifficultyButton.onClick.AddListener(SetNormalDifficulty);
        hardDifficultyButton.onClick.AddListener(SetHardDifficulty);
        map1Button.onClick.AddListener(() => SelectMap("Map1"));
        map2Button.onClick.AddListener(() => SelectMap("Map2"));
        saveButton.onClick.AddListener(SaveSettings);
        continueButton.onClick.AddListener(LoadSelectedMap);
    }

    private void SetNormalDifficulty()
    {
        timeBetweenWaves = 10f;
        zombiePerWave = 4;
    }

    private void SetHardDifficulty()
    {
        timeBetweenWaves = 7f;
        zombiePerWave = 8;
    }

    public void SaveSettings()
    {
        smoothSpeed = smoothSpeedSlider.value;
        sensitivity = sensitivitySlider.value;

        PlayerPrefs.SetFloat("TimeBetweenWaves", timeBetweenWaves);
        PlayerPrefs.SetInt("ZombiesPerWave", zombiePerWave);
        PlayerPrefs.SetFloat("SmoothSpeed", smoothSpeed);
        PlayerPrefs.SetFloat("Sensitivity", sensitivity);

        PlayerPrefs.Save();

        Debug.Log("Game Saved");
    }

    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey("TimeBetweenWaves"))
        {
            timeBetweenWaves = PlayerPrefs.GetFloat("TimeBetweenWaves");
        }

        if (PlayerPrefs.HasKey("ZombiesPerWave"))
        {
            zombiePerWave = PlayerPrefs.GetInt("ZombiesPerWave");
        }

        if (PlayerPrefs.HasKey("SmoothSpeed"))
        {
            smoothSpeed = PlayerPrefs.GetFloat("SmoothSpeed");
            smoothSpeedSlider.value = smoothSpeed;
        }

        if (PlayerPrefs.HasKey("Sensitivity"))
        {
            sensitivity = PlayerPrefs.GetFloat("Sensitivity");
            sensitivitySlider.value = sensitivity;
        }

        if (PlayerPrefs.HasKey("SelectedMap"))
        {
            selectedMap = PlayerPrefs.GetString("SelectedMap", selectedMap);
        }

        Debug.Log("Game Loaded");
    }

    private void SelectMap(string mapName)
    {
        selectedMap = mapName;
        PlayerPrefs.SetString("SelectedMap", selectedMap);
        PlayerPrefs.Save();
    }

    private void LoadSelectedMap()
    {
        SceneManager.LoadScene(selectedMap);
    }
}
