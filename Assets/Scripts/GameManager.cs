using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Player playerRef;
    [Tooltip("The maximum number of enemies that can be alive at any point")]
    public int MaxEnemiesAllowed; //The remaining number of enemies to be spawned
    [Tooltip("The number of enemies remaining in this wave")]
    public int enemiesRemaining;//The remaining number of enemies that must be killed to complete the wave
    public float spawnRate = 3;
    [Tooltip("The number of enemies currently alive")]
    public int enemiesAlive = 0;//The Number of Enemies Currently Alive
    
    public float spawnTimer=0;
    public static GameManager instance;
    int score = 0;
    public Vector3 spawnOffset;
    public GameObject pauseCanvas;
    public bool paused;
    public GameObject respawnScreen;
    public Vector3 spawnPosition;
    [SerializeField]
    List<EnemySpawner> spawners;
    public TextMeshProUGUI waveInfoDisplay;
    public TextMeshProUGUI breakTimeText, ammoDisplayText;
    [System.Serializable]public class Wave
    {
        public int waveNum;//The Wave Number
        public int EnemiesPerWave;//The Enemies to be spawned in this wave
        public float nextWaveDelay = 25;
        public AnimationCurve enemiesPerWaveRamp;
    }
    public Wave[] waves;
    public int waveContainerIndex;
    public int currentWave;
    public bool waveInProgress;
    [Header("Weapon Switching")]
    public GameObject weaponWheel;
    public float weaponWheelTimeMutliplier;
    /// <summary>
    /// The fixed timestep 
    /// </summary>
    float weaponWheelTimestep;
    public float defaultFixedTimestep = 0.02f;
    public bool weaponWheelOpen;
    public Volume damageVolume;
    public void UseWeaponWheel(bool opening)
    {
        //If the player is dead, we don't want to allow the player to open the weapon wheel.
        //I don't think there's any real adverse effects of letting the player do this, because it hasn't been tried yet
        //But its stupid and pointless to let the player do this when the weapon wheel is hidden by the menus anyway :p

        if (!playerRef.IsAlive)
            return;

        if (opening)
        {
            weaponWheelTimestep = defaultFixedTimestep * weaponWheelTimeMutliplier;
            Time.timeScale = weaponWheelTimeMutliplier;
            Time.fixedDeltaTime = weaponWheelTimestep;
            weaponWheel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Time.timeScale = 1;
            Time.fixedDeltaTime = defaultFixedTimestep;
            weaponWheel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        weaponWheelOpen = opening;
    }
    public void PauseGame(bool newPause)
    {
        //pauses the game
        //I don't much like the timescale method but there's not much else I can think to do other than disabling most components and that might have a wonky effect
        //this is but a humble game so its okay :)
        paused = newPause;
        Time.timeScale = paused ? 0 : 1;
        pauseCanvas.SetActive(paused);
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }
    public void Respawn()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    private void Awake()
    {
        //Initialise singleton if one doesn't already exist, or destroy this object if it does
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        //Check if the main camera in the scene does or doesn't have a Cinemachine Brain,
        //Allows it to work with the player
        if (!Camera.main.GetComponent<CinemachineBrain>())
        {
            Camera.main.gameObject.AddComponent<CinemachineBrain>();
        }
        //Get the player 
        if (!playerRef)
            playerRef = FindFirstObjectByType<Player>();
        //Force the game to be unpaused
        PauseGame(false);
        //Disable this menu
        respawnScreen.SetActive(false);
        //Gather all the spawners
        spawners.AddRange(FindObjectsOfType<EnemySpawner>(true));
        //Start the first wave delay
        StartCoroutine(WaveDelay());
    }


    // Update is called once per frame
    private void FixedUpdate()
    {
        if (waveInProgress)
        {
            if (enemiesAlive < MaxEnemiesAllowed && enemiesRemaining > 0)
            {
                spawnTimer += Time.fixedDeltaTime;
                if (spawnTimer >= spawnRate)
                {
                    FindSpawner();
                }
            }
            else
            {
                spawnTimer = 0;
            }
        }
        if (playerRef.weaponManager.CurrentWeapon) 
        {
            (int max, int current) = playerRef.weaponManager.CurrentWeapon.Ammo;
            ammoDisplayText.text = $"{current}\n/{max}";
        }
    }
    public void FindSpawner()
    {
        if (playerRef == null || spawners.Count == 0)
            return;
        spawnTimer = 0;
        enemiesAlive++;
        enemiesRemaining--;
        int spawnerIndex = Random.Range(0, spawners.Count);
        spawners[spawnerIndex].Spawn();
        SetEnemyDisplay();
    }
    void SetEnemyDisplay()
    {
        waveInfoDisplay.text = WaveStringBuilder();
    }
    public void EnemyDeath()
    {
        score++;
        //The number of enemies left decrements when an enemy dies
        enemiesAlive--;
        SetEnemyDisplay();
        if (enemiesRemaining == 0 && enemiesAlive == 0)
        {
            EndWave();
        }
    }
    void EndWave()
    {
        StartCoroutine(WaveDelay());
    }
    IEnumerator WaveDelay()
    {
        waveInProgress = false;
        float time = waves[waveContainerIndex].nextWaveDelay;
        while (time >= 0)
        {
            breakTimeText.text = $"Break Time: {time:0}";
            time -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        WaveStart();
        yield break;
    }

    public void WaveStart()
    {
        currentWave++;
        breakTimeText.text = "";
        for (int i = 0; i < waves.Length; i++)
        {
            if(currentWave >= waves[i].waveNum)
            {
                waveContainerIndex = i;
            }
        }

        if(waveContainerIndex < waves.Length - 1)
        {
            float ilerp = Mathf.InverseLerp(waves[waveContainerIndex].waveNum, waves[waveContainerIndex +1].waveNum, currentWave);
            enemiesRemaining = Mathf.CeilToInt(Mathf.Lerp(waves[waveContainerIndex].EnemiesPerWave, waves[waveContainerIndex + 1].EnemiesPerWave, waves[waveContainerIndex].enemiesPerWaveRamp.Evaluate(ilerp)));
        }
        else
        {
            enemiesRemaining = waves[waveContainerIndex].EnemiesPerWave;
        }
        SetEnemyDisplay();
        waveInProgress = true;
    }

    public string WaveStringBuilder()
    {
        string printString = null;
        int currentwaveHundreds = Mathf.FloorToInt( currentWave / 100);
        int currentwave = currentWave % 100;
        printString = $"{currentwaveHundreds:00}:{currentwave:00}";
        return printString;
    }
}
