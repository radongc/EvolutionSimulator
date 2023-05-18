using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Food : MonoBehaviour
{
    public FoodHardness foodHardness;
    public float foodScale;

    private float expirationTimer = 0;
    private bool hasSetColor = false;

    public void Initialize(float scale, FoodHardness hardness)
    {
        foodScale = scale;
        foodHardness = hardness;

        transform.localScale = new Vector3(foodScale, foodScale, foodScale);
    }

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
        float energy = (foodScale / 2f) * 100f;

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