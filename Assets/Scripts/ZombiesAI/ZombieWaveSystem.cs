using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ZombieWaveSystem — The real SK
// From tutorial videos 30 (setup), 31 (logic), 37 (wave UI).
// Quirk kept deliberately (tutorial's own): zombiesPerWave += 2 runs
// BEFORE the first wave spawns, so wave 1 spawns 6 zombies, not 4.
// LoadSetting() reads saved difficulty from PlayerPrefs; the tutorial
// calls it from Update() every frame (video 49) — here it is called
// once from Start() (same result, no per-frame cost).
public class ZombieWaveSystem : MonoBehaviour
{
    public GameObject[] zombiePrefabs;
    public Transform[] spawnPoints;
    public float timeBetweenWaves = 10f;
    [SerializeField] private float waveTimer = 0f;
    private int waveNumber = 1;
    public int zombiesPerWave = 4;

    [Header("UI")]
    public Text WaveNumber;
    public Text WaveTimer;

    void Start()
    {
        LoadSetting();
        if (WaveNumber != null)
        {
            WaveNumber.text = waveNumber.ToString();
        }
    }

    void Update()
    {
        if (waveNumber == 10)
        {
            return;
        }

        waveTimer += Time.deltaTime;

        int intValue = Mathf.RoundToInt(waveTimer);

        if (WaveTimer != null)
        {
            WaveTimer.text = intValue.ToString();
        }

        if (waveTimer >= timeBetweenWaves)
        {
            StartNewWave();
        }
    }

    void StartNewWave()
    {
        waveTimer = 0f;

        zombiesPerWave += 2;

        float minDistance = 4f;

        for (int i = 0; i < zombiesPerWave; i++)
        {
            int randomSpawnIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomSpawnIndex];

            GameObject randomZombiePrefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];

            Vector3 spawnPosition = spawnPoint.position + Random.insideUnitSphere * minDistance;
            spawnPosition.y = spawnPoint.position.y;

            Instantiate(randomZombiePrefab, spawnPosition, spawnPoint.rotation);
        }

        waveNumber++;

        if (WaveNumber != null)
        {
            WaveNumber.text = waveNumber.ToString();
        }
    }

    public void LoadSetting()
    {
        if (PlayerPrefs.HasKey("TimeBetweenWaves"))
        {
            timeBetweenWaves = PlayerPrefs.GetFloat("TimeBetweenWaves");
        }
        if (PlayerPrefs.HasKey("ZombiesPerWave"))
        {
            zombiesPerWave = PlayerPrefs.GetInt("ZombiesPerWave");
        }
    }
}
