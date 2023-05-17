using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Food : MonoBehaviour
{
    public FoodType foodType;
    public FoodHardness foodHardness;

    private float expirationTimer = 0;
    private bool hasSetColor = false;

    private void Update()
    {
        if (!hasSetColor)
        {
            if (foodHardness == FoodHardness.Soft)
                GetComponent<Renderer>().material.color = Color.green;
            else
                GetComponent<Renderer>().material.color = Color.black;
        }

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

public enum FoodHardness
{
    Soft,
    Hard,
}