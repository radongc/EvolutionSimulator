using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Creature class.
// NOTES:
// Separation of concerns should've been implemented. Movement related code should go in one class, trait related in another, and eating in another.
// (Backbone of class generated from ChatGPT)

// Future ideas:
// Introduce trait that:
// * Affects gestation period
// * Affects energy needed to be pregnant

public class Creature : MonoBehaviour
{
    // Traits
    [SerializeField] private float speed;
    [SerializeField] private float sense;
    [SerializeField] private float size;
    [SerializeField] private MouthSize mouthSize;

    // Usable trait values
    private float realSpeed;
    private float realSense;

    // Energy
    [SerializeField] private float energy = 100f;
    [SerializeField] private float maxEnergy = 200f;
    [SerializeField] private float movementEnergyCost;
    private float pregnancyEnergyCost = 100f;

    // Movement
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetPosition;

    // Reproduction
    [SerializeField] private bool isPregnant = false;
    private float pregnancyTime = 10f;
    private float currentPregnancyTime = 0f;

    // Mutation constants
    private const float mutationChance = 0.25f;
    private const float mutationRange = 0.2f;

    // Game constants
    private const float predatorSize = 1.65f;

    // Game variables
    private Color materialColor;
    private bool isInitialized = false;
    [SerializeField] private bool isFleeing = false;

    public void Initialize(float speed, float sense, float size, MouthSize mouthSize)
    {
        this.speed = speed;
        this.sense = sense;
        this.size = size;
        this.mouthSize = mouthSize;

        // Correlate size trait with actual in-game size.
        SetRealSize();

        // Testing out increasing energy cost of large size for reproduction.
        // This creates biases toward being too small, and occasionally too big. It is how it works in nature, but I'm not sure how to fix the bias.
        //SetMaxEnergy();

        materialColor = GetComponent<Renderer>().material.color;
        targetPosition = GetRandomPositionNearMe();

        isInitialized = true;
    }

    private void SetRealSize() 
    {
        transform.localScale = new Vector3(size, size, size);
    }

    private void SetMaxEnergy()
    {
        maxEnergy = size * 200f;
        energy = maxEnergy / 2;
        pregnancyEnergyCost = maxEnergy / 2;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitialized) { return; }

        UpdateMovement();
        SensePredators();
        CheckVicinityFood();
        CheckReproduction();
        CheckPregnancyState();
        CheckDeath();
    }

    private void UpdateMovement()
    {
        if (realSpeed == 0)
            realSpeed = speed * 2.5f;
        if (realSense == 0)
            realSense = sense * 5f;

        // Move towards target
        MoveTo(targetPosition);

        if (target && !isFleeing)
            targetPosition = target.position;
        else
        {
            if (isFleeing)
            {
                // Get direction and move 6 units away from there in that direction.
                Vector3 direction = Vector3.Normalize(targetPosition - transform.position);
                Vector3 offset = transform.position + direction * 6f;
                targetPosition = transform.position + offset;

                if (GetComponent<Renderer>().material.color != Color.red)
                    GetComponent<Renderer>().material.color = Color.red;
            }
            else if (Vector3.Distance(transform.position, targetPosition) < transform.localScale.x - (transform.localScale.x * 0.25f))
            {
                targetPosition = GetRandomPositionNearMe();

                if (GetComponent<Renderer>().material.color != materialColor)
                    GetComponent<Renderer>().material.color = materialColor;
            }

            SenseFood();
        }
    }

    private void CheckVicinityFood()
    {
        Collider[] foodCheck = Physics.OverlapSphere(transform.position, transform.localScale.x * 1.1f);

        bool consumed = false;

        foreach (Collider other in foodCheck)
        {
            if (other.transform == target)
            {
                if (other.GetComponent<Food>())
                {
                    EatFood(other.GetComponent<Food>());
                    consumed = true;
                }
                else if (other.GetComponent<Creature>()) // We don't need to check size as, if this creature is our target, that has already been done. (Maybe faulty thinking? Who knows!)
                {
                    EatCreature(other.GetComponent<Creature>());
                    consumed = true;
                }
            }
        }

        if (consumed)
            targetPosition = GetRandomPositionNearMe();
    }

    private void CheckReproduction()
    {
        if (!isPregnant && energy >= maxEnergy)
        {
            isPregnant = true;
            energy -= pregnancyEnergyCost;
            currentPregnancyTime = 0f;
        }
    }

    private void CheckPregnancyState()
    {
        // Reproduce if pregnant
        if (isPregnant)
        {
            currentPregnancyTime += Time.deltaTime;

            if (currentPregnancyTime >= pregnancyTime)
            {
                Reproduce();
            }
        }
    }

    private void CheckDeath()
    {
        if (energy <= 0f)
        {
            GetEatenOrDie();
        }
    }

    private void SenseFood()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, realSense);
        Collider potentialFood = GetClosestFood(hitColliders);

        if (potentialFood)
        {
            target = potentialFood.transform;
        }
    }

    private void SensePredators()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, realSense);
        Collider potentialPredator = GetPotentialPredator(hitColliders);

        if (potentialPredator)
        {
            isFleeing = true;
            Debug.Log("Fleeing!");
        }
        else
        {
            isFleeing = false;
        }
    }

    private void Reproduce()
    {
        // Spawn offspring
        Vector3 offspringPosition = transform.position + new Vector3(1f, 0f, 1f);
        Creature offspring = Environment.instance.MakeCreature(offspringPosition);

        float mutationSpeed = 0f;
        float mutationSense = 0f;
        float mutationSize = 0f;
        MouthSize mutationMouthSize = mouthSize;

        // Mutate offspring
        if (Random.Range(0f, 1f) <= mutationChance)
        {
            mutationSpeed = Random.Range(-mutationRange, mutationRange);
        }

        if (Random.Range(0f, 1f) <= mutationChance)
        {
            mutationSense = Random.Range(-mutationRange, mutationRange);
        }

        if (Random.Range(0f, 1f) <= mutationChance)
        {
            mutationSize = Random.Range(-mutationRange, mutationRange);
        }

        if (Random.Range(0f, 1f) <= mutationChance)
        {
            if (mouthSize == MouthSize.Small)
                mutationMouthSize = MouthSize.Large;
            else if (mouthSize == MouthSize.Large)
                mutationMouthSize = MouthSize.Small;
        }

        float offspringSpeed = speed + (speed * mutationSpeed);
        float offspringSense = sense + (sense * mutationSense);
        float offspringSize = size + (size * mutationSize);

        offspring.GetComponent<Creature>().Initialize(offspringSpeed, offspringSense, offspringSize, mutationMouthSize);

        // Reset pregnancy
        isPregnant = false;
        currentPregnancyTime = 0f;
    }

    void MoveTo(Vector3 position)
    {
        transform.position = Vector3.MoveTowards(transform.position, position, realSpeed * Time.deltaTime);

        // Reduce energy based on movement. Size costs 25% more energy while pregnant.
        // Trying out different energy formulas..
        //movementEnergyCost = (1.2f * (isPregnant ? speed * size : speed) + 1.1f * sense) * size;
        //movementEnergyCost = ((size * 2.25f) * (speed * 1.5f)) + sense;
        //movementEnergyCost = ((speed * 3.5f) + (sense * 2f)) * size;
        movementEnergyCost = ((Mathf.Pow(size, 3) * Mathf.Pow(speed, 2)) + sense) * 2;
        energy -= movementEnergyCost * Time.deltaTime;
    }

    Vector3 GetRandomPositionNearMe()
    {
        Vector3 pos = transform.position;

        pos.x = pos.x + Random.Range(-realSense, realSense);
        pos.z = pos.z + Random.Range(-realSense, realSense);

        if (pos.x > Environment.instance.GetMapBounds().x / 2f)
            pos.x = (Environment.instance.GetMapBounds().x / 2f) - 1;

        if (pos.z > Environment.instance.GetMapBounds().y / 2f)
            pos.z = (Environment.instance.GetMapBounds().y / 2f) - 1;

        if (pos.x < -(Environment.instance.GetMapBounds().x / 2f))
            pos.x = -(Environment.instance.GetMapBounds().x / 2f) + 1;

        if (pos.z < -(Environment.instance.GetMapBounds().y / 2f))
            pos.z = -(Environment.instance.GetMapBounds().y / 2f) + 1;

        return pos;
    }

    private bool IsMouthCorrectSize(Food food)
    {
        if (mouthSize == MouthSize.Large)
            return true;
        else if (mouthSize == MouthSize.Small && food.foodHardness == FoodHardness.Soft)
            return true;
        else
            return false;
    }

    private bool CanEat(Food food)
    {
        if (!IsMouthCorrectSize(food))
            return false;

        if (food.foodType == FoodType.Small)
        {
            if (size >= 0.55f)
                return true;
            else
                return false;
        }
        else if (food.foodType == FoodType.Regular)
        {
            if (size >= 0.85f)
                return true;
            else
                return false;
        }
        else
        {
            if (size >= 1.15f)
                return true;
            else
                return false;
        }
    }

    void EatFood(Food food)
    {
        energy += food.ConsumeEnergy();
    }

    void EatCreature(Creature creature)
    {
        energy += (creature.size * 100f);
        creature.GetEatenOrDie();
        Debug.Log("Creature ate a creatuer!");
    }

    // Taken from old natsel project. Retrieves the closest food object from nearby, instead of the first one in the list of colliders.
    Collider GetClosestFood(Collider[] objects)
    {
        Collider tMin = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;
        foreach (Collider t in objects)
        {
            if (t.GetComponent<Food>() && CanEat(t.GetComponent<Food>()) || t.GetComponent<Creature>() && t.GetComponent<Creature>().size * predatorSize < size)
            {
                float dist = Vector3.Distance(t.transform.position, currentPos);
                if (dist < minDist)
                {
                    tMin = t;
                    minDist = dist;
                }
            }
        }
        return tMin;
    }

    Collider GetPotentialPredator(Collider[] objects)
    {
        foreach (Collider t in objects)
        {
            if (t.GetComponent<Creature>() && size * predatorSize < t.GetComponent<Creature>().size)
            {
                return t;
            }
        }

        return null;
    }

    public void GetEatenOrDie()
    {
        Environment.instance.creatures.Remove(this);
        Destroy(gameObject);
    }

    public float GetSpeed() => speed;
    public float GetSense() => sense;
    public float GetSize() => size;
    public MouthSize GetMouthSize() => mouthSize;
}

public enum MouthSize
{
    Small,
    Large
}