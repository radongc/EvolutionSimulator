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

    // Usable trait values
    private float realSpeed;
    private float realSense;

    // Energy
    [SerializeField] private float energy = 100f;
    [SerializeField] private float maxEnergy = 200f;

    // Movement
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float movementEnergyCost;

    // Reproduction
    [SerializeField] private bool isPregnant = false;
    private float pregnancyTime = 10f;
    private float currentPregnancyTime = 0f;
    private float pregnancyEnergyCost = 100f;

    // Mutation constants
    private const float mutationChance = 0.15f;
    private const float mutationRange = 0.2f;

    // Game constants
    private const float predatorSize = 1.4f;

    // Game variables
    private bool isInitialized = false;
    [SerializeField] private bool isFleeing = false;

    // Start is called before the first frame update
    public void Initialize(float speed, float sense, float size)
    {
        this.speed = speed;
        this.sense = sense;
        this.size = size;

        // Correlate size trait with actual in-game size.
        transform.localScale = new Vector3(size, size, size);

        // Testing out increasing energy cost of large size for reproduction.
        maxEnergy = size * 200f;
        pregnancyEnergyCost = maxEnergy / 2;

        isInitialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isInitialized)
        {
            // TODO: Make these static formulas somehow.
            if (realSpeed == 0)
                realSpeed = speed * 2.5f;
            if (realSense == 0)
                realSense = sense * 5f;

            // Move towards target
            MoveTo(targetPosition);

            CheckVicinityFood();

            if (target && !isFleeing)
                targetPosition = target.position;
            else
            {
                // Search for food
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, realSense);

                Collider potentialFood = GetClosestFood(hitColliders);
                Collider potentialPredator = GetPotentialPredator(hitColliders);

                if (isFleeing)
                {
                    // Get direction and move 6 units away from there in that direction.
                    Vector3 direction = Vector3.Normalize(targetPosition - transform.position);
                    Vector3 offset = transform.position + direction * 6f;
                    targetPosition = transform.position + offset;
                }
                else if (Vector3.Distance(transform.position, targetPosition) < transform.localScale.x * 1.1f)
                    targetPosition = Environment.instance.GetRandomPosition();

                if (potentialFood)
                {
                    target = potentialFood.transform;
                }

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

            // Check for reproduction
            if (!isPregnant && energy >= maxEnergy)
            {
                isPregnant = true;
                energy -= pregnancyEnergyCost;
                currentPregnancyTime = 0f;
            }

            // Reproduce if pregnant
            if (isPregnant)
            {
                currentPregnancyTime += Time.deltaTime;

                if (currentPregnancyTime >= pregnancyTime)
                {
                    // Spawn offspring
                    Vector3 offspringPosition = transform.position + new Vector3(1f, 0f, 1f);
                    Creature offspring = Environment.instance.MakeCreature(offspringPosition);

                    float mutationSpeed = 0f;
                    float mutationSense = 0f;
                    float mutationSize = 0f;

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
                        mutationSize = Random.Range(-mutationRange * 1.25f, mutationRange * 1.25f);
                    }

                    float offspringSpeed = speed + (speed * mutationSpeed);
                    float offspringSense = sense + (sense * mutationSense);
                    float offspringSize = size + (size * mutationSize);

                    offspring.GetComponent<Creature>().Initialize(offspringSpeed, offspringSense, offspringSize);

                    // Reset pregnancy
                    isPregnant = false;
                    currentPregnancyTime = 0f;
                }
            }

            // Check for death
            if (energy <= 0f)
            {
                GetEatenOrDie();
            }
        }
    }

    void MoveTo(Vector3 position)
    {
        transform.position = Vector3.MoveTowards(transform.position, position, realSpeed * Time.deltaTime);

        // Reduce energy based on movement. Size costs 25% more energy while pregnant.
        //movementEnergyCost = (1.2f * (isPregnant ? speed * size : speed) + 1.1f * sense) * size;
        movementEnergyCost = ((size * 2.25f) * (speed * 1.5f)) + sense; 
        energy -= movementEnergyCost * Time.deltaTime;
    }

    private void CheckVicinityFood()
    {
        Collider[] foodCheck = Physics.OverlapSphere(transform.position, transform.localScale.x * 1.1f);

        bool consumed = false;

        foreach (Collider other in foodCheck)
        {
            if (other.transform == target)
            {
                if (other.CompareTag("Food"))
                {
                    EatFood(other.gameObject);
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
            targetPosition = Environment.instance.GetRandomPosition();
    }

    void EatFood(GameObject food)
    {
        energy += 50f;
        Environment.instance.food.Remove(food);
        Destroy(food);
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
            if (t.CompareTag("Food") || t.GetComponent<Creature>() && t.GetComponent<Creature>().size * predatorSize < size)
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
}