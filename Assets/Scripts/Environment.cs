using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Environment : MonoBehaviour
{
    public static Environment instance;

    public GameObject creaturePrefab;

    public GameObject smallFoodPrefab;
    public GameObject foodPrefab;
    public GameObject largeFoodPrefab;

    // Initialization values
    [Header("Initialization Values")]
    public float startingSpeed = 1f;
    public float startingSense = 1f;
    public float startingSize = 1f;

    [Header("Game Constants")]
    public int startingCreatureNum = 5;
    public Vector2 foodPerDayRange = new Vector2(25, 50);
    public Vector2 dailyTempRange = new Vector2(50f, 80f);
    public float dayDuration = 10f;
    public float foodExpirationTime = 10f;

    // Game variables
    public DateTime gameStartTimestamp;
    
    [Header("Game Variables")]
    public int realFoodPerDay = 0;
    public int maxFoodPerDay = 0;
    public int currentDay = 0;
    public float realTemperature = 70f;
    [SerializeField] private float dayTimer = 0f;
    public float timeScale = 1f;

    [Header("Statistics")]
    [SerializeField] private float averageSpeed = 1f;
    [SerializeField] private float averageSense = 1f;
    [SerializeField] private float averageSize = 1f;

    // Containers
    [Header("Containers")]
    public List<Creature> creatures = new List<Creature>();
    public List<Food> food = new List<Food>();

    public List<int> daysOverTime = new List<int>();
    public List<int> creatureNumOverTime = new List<int>();

    private void Awake()
    {
        if (instance)
            Debug.LogWarning("Environment instance already exists, problem with singleton!");
        else
            instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        PopulateCreatures();
        PopulateFood();

        gameStartTimestamp = DateTime.Now;
    }

    // Update is called once per frame
    void Update()
    {
        dayTimer += Time.deltaTime;

        if (dayTimer >= dayDuration)
        {
            currentDay++;

            dayTimer = 0f;
            PopulateFood();
            CalculateStatistics();
        }

        if (timeScale != Time.timeScale)
            Time.timeScale = timeScale;
    }

    void PopulateCreatures()
    {
        for (int i = 0; i < startingCreatureNum; i++)
        {
            Creature creatureInstance = MakeCreature(GetRandomPosition());
            creatureInstance.Initialize(startingSpeed, startingSense, startingSize);
        }
    }

    void PopulateFood()
    {
        // Clamp total food in environment to 50% higher than the highest possible food per day.
        maxFoodPerDay = (int)(foodPerDayRange.y + (foodPerDayRange.y * 0.5f));
        realFoodPerDay = (int)Mathf.Min(UnityEngine.Random.Range(foodPerDayRange.x, foodPerDayRange.y), maxFoodPerDay - food.Count);

        for (int i = 0; i < realFoodPerDay; i++)
        {
            MakeFood(GetRandomPosition());
        }
    }

    void CalculateStatistics()
    {
        int creatureCount = creatures.Count;

        float totalSpeed = 0f;
        float totalSense = 0f;
        float totalSize = 0f;

        foreach (Creature creature in creatures)
        {
            totalSpeed += creature.GetSpeed();
            totalSense += creature.GetSense();
            totalSize += creature.GetSize();
        }

        averageSpeed = totalSpeed / creatureCount;
        averageSense = totalSense / creatureCount;
        averageSize = totalSize / creatureCount;

        daysOverTime.Add(currentDay);
        creatureNumOverTime.Add(creatureCount);

        // Record graph info every 10 days.
        if (currentDay % 10 == 0)
        {
            string daysFileName = $"GRAPH_days_{gameStartTimestamp.ToShortDateString()}-{gameStartTimestamp.ToLongTimeString()}.txt";
            string creaturesFileName = $"GRAPH_creatures_{gameStartTimestamp.ToShortDateString()}-{gameStartTimestamp.ToLongTimeString()}.txt";
            
            daysFileName = daysFileName.Replace('/', '_').Replace(':', '_');
            creaturesFileName = creaturesFileName.Replace('/', '_').Replace(':', '_');

            string graphDaysPath = $"Assets/Resources/{daysFileName}";
            string graphCreaturesPath = $"Assets/Resources/{creaturesFileName}";

            if (!File.Exists(graphDaysPath))
            {
                var fs = new FileStream(graphDaysPath, FileMode.Create);
                fs.Dispose();
            }
            if (!File.Exists(graphCreaturesPath))
            {
                var fs = new FileStream(graphCreaturesPath, FileMode.Create);
                fs.Dispose();
            }
            
            foreach (int i in daysOverTime)
            {
                StreamWriter daysWriter = new StreamWriter(graphDaysPath, true);
                daysWriter.Write($"{i}, ");
                daysWriter.Close();
            }

            foreach (int i in creatureNumOverTime)
            {
                StreamWriter creaturesWriter = new StreamWriter(graphCreaturesPath, true);
                creaturesWriter.Write($"{i}, ");
                creaturesWriter.Close();
            }

            daysOverTime.Clear();
            creatureNumOverTime.Clear();
        }
    }

    public Creature MakeCreature(Vector3 position)
    {
        Creature creatureInstance = Instantiate(creaturePrefab, position, Quaternion.identity).GetComponent<Creature>();
        creatures.Add(creatureInstance);
        
        return creatureInstance;
    }

    void MakeFood(Vector3 position)
    {
        GameObject prefabToUse;
        int randomChance = UnityEngine.Random.Range(0, 3);

        if (randomChance == 0)
        {
            prefabToUse = smallFoodPrefab;
        }
        else if (randomChance == 1)
        {
            prefabToUse = foodPrefab;
        }
        else
        {
            prefabToUse = largeFoodPrefab;
        }

        Food foodInstance = Instantiate(prefabToUse, position, Quaternion.identity).GetComponent<Food>();
        food.Add(foodInstance);
    }

    public Vector2 GetMapBounds() => new Vector2(transform.localScale.x, transform.localScale.z);

    public Vector3 GetRandomPosition() => new Vector3(
                                                       UnityEngine.Random.Range(
                                                           -(GetMapBounds().x / 2f),
                                                           GetMapBounds().x / 2f),
                                                       0.5f,
                                                       UnityEngine.Random.Range(
                                                           -(GetMapBounds().y / 2f),
                                                           GetMapBounds().y / 2f)
                                                      );
}
