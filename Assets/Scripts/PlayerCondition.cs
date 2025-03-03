using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCondition : MonoBehaviour
{
    public UICondition uiCondition;

    private Condition health
    {
        get { return uiCondition.health; }
    }
    private Condition hunger
    {
        get { return uiCondition.hunger; }
    }
    private Condition stamina
    {
        get { return uiCondition.stamina; }
    }

    public float noHungerHealthDecay;

    private void Update()
    {
        hunger.Subtract(hunger.passiveValue * Time.deltaTime);
        stamina.Add(stamina.passiveValue * Time.deltaTime);

        if (hunger.curValue <= 0f)
        {
            health.Subtract(noHungerHealthDecay * Time.deltaTime);
        }

        if (health.curValue <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        health.Add(amount);
    }

    public void Eat(float amount)
    {
        hunger.Add(amount);
    }
    
    public void Die()
    {
        Debug.Log("죽었다");
    }
}