using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Food : MonoBehaviour
{
    public FoodType foodType;
    private float expirationTimer = 0;

    private void Update()
    {
        if (expirationTimer >= Environment.instance.foodExpirationTime)
        {
            Environment.instance.food.Remove(this);
            Destroy(gameObject);
        }

        expirationTimer += Time.deltaTime;
    }

    public float ConsumeEnergy()
    {
        float energy = 0f;

        if (foodType == FoodType.Small)
        {
            energy = 25f;
        }
        else if (foodType == FoodType.Regular)
        {
            energy = 50f;
        }
        else if (foodType == FoodType.Large)
        {
            energy = 75f;
        }

        Environment.instance.food.Remove(this);
        Destroy(gameObject);
        return energy;
    }
}

public enum FoodType
{
    Small,
    Regular,
    Large
}