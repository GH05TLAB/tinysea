﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour {

	/*
	 *  Player's species
     *  Make sure that these are in the same order as the SwimmingHolder's prefabs!
	 */
	public List<CharacterManager> species; 
    //Highest level in the species list
    public int lowestLevel = 1;
    public int highestLevel = 3;
    public Temperature temperature;
	/*
	 *  Player's data
	 */

	public float days = 0;
	public float moneys = 0;
    public float dailyIncome = 100;
    public float netWorthIncomeRate = .2f;
    public float sellRate = .5f;
    public bool waitingForTemperature = false;

    public Text moneyText;

	/*
	 * Initialize with three species at each level
	 */
	void Awake(){
        species = new List<CharacterManager>(GetComponentsInChildren<CharacterManager>());
        foreach (CharacterManager c in species)
        {
            c.player = this;
        }
	}

    public void Update()
    {
        if (Input.GetButtonDown("Submit") && !waitingForTemperature)
        {
            waitingForTemperature = true;
            temperature.updateTemperature();
        }
        if (!temperature.animating && waitingForTemperature)
        {
            StepEcosystem(0.5f);
            waitingForTemperature = false;
            moneys += dailyIncome;
            foreach (CharacterManager c in species)
            {
                moneys += c.cost * c.speciesAmount * netWorthIncomeRate;
            }

        }
        /*if (Input.GetButtonDown("Fire1"))
        {
            if (Random.value < .05)
                CreatureAmountChanged(2, 1);
            else if (Random.value < .2)
                CreatureAmountChanged(1, 1);
            else
                CreatureAmountChanged(0, 1);
        }*/

        moneyText.text = Mathf.Floor(moneys).ToString();
    }

    public void Buy(int index)
    {
        if (moneys >= species[index].cost)
        {
            CreatureAmountChanged(index, 1);
            moneys -= species[index].cost;
            moneyText.text =  moneys.ToString();
        }
    }

    public void SellAll()
    {
        foreach (CharacterManager c in species)
        {
            moneys += Mathf.Round(c.speciesAmount) * c.cost * sellRate;
            c.speciesAmount = 0;
        }
    }

    public void CreatureAmountChanged(int index, float amount)
    {
        species[index].speciesAmount += amount;
    }

    //step the ecosystem forward by a given number or fraction of days
    public void StepEcosystem(float dayStep)
    {
        days += dayStep;
        foreach (CharacterManager c in species)
        {
            c.updatePerformance(temperature.temperature);
        }
        Predation(dayStep);
        foreach (CharacterManager c in species)
        {
            c.ReproduceOrDie(dayStep);
        }
    }

    //gets the total amount of all species for a given level
    private float getTotalAmountAtLevel(int level)
    {
        float amount = 0;
        foreach (CharacterManager c in species)
        {
            if (c.foodChainLevel == level)
            {
                amount += c.speciesAmount;
            }
        }
        return amount;
    }

    //returns a list of all the creatures for a given level
    private List<CharacterManager> getCharactersAtLevel(int level)
    {
        List<CharacterManager> list = new List<CharacterManager>();
        foreach (CharacterManager c in species)
        {
            if (c.foodChainLevel == level)
            {
                list.Add(c);
            }
        }
        return list;
    }

    //eats a certain amount of creatures sampled evenly within that level
    //negative things will happen if eatAmount is > the amount of creatures
    //^^^^^^^^ pun intended.
    private void eatAtLevel(int level, float eatAmount)
    {
        //make a list of all creatures in this level
        float totalAmount = getTotalAmountAtLevel(level);
        List<CharacterManager> eatenCreatures = getCharactersAtLevel(level);
        //eat them... EAT THEM!!! 
        if (totalAmount == 0)
            return;
        foreach (CharacterManager c in eatenCreatures)
        {
            float ratio = c.speciesAmount / totalAmount;
            c.speciesAmount = c.speciesAmount - ratio * eatAmount;
        }
    }

    // Food obtain
    private void Predation(float dayStep)
    {
        //step through each species level, skipping the first one (they don't eat anything)
        for (int i = lowestLevel + 1; i <= highestLevel; i++)
        {
            List<CharacterManager> leveliCharacters = new List<CharacterManager>();
            float leveliAmount = 0;
            float foodRequestAmount = 0;
            foreach (CharacterManager c in species)
            {
                if (c.foodChainLevel == i)
                {
                    leveliCharacters.Add(c);
                    leveliAmount += c.speciesAmount;
                    foodRequestAmount += c.speciesAmount * c.GetEatingRate() * dayStep;
                }
            }
            
            //if we don't have enough food to go around
            if (foodRequestAmount > getTotalAmountAtLevel(i - 1))
            {
                //set fed performance to the reduced amount. 
                float subOptimalFoodRate = getTotalAmountAtLevel(i-1) / foodRequestAmount;
                foreach (CharacterManager c in leveliCharacters)
                {
                    c.fedRate = subOptimalFoodRate;
                }
                eatAtLevel(i - 1, getTotalAmountAtLevel(i - 1));
            }
            else
            {
                foreach (CharacterManager c in leveliCharacters)
                {
                    c.fedRate = 1;
                }
                eatAtLevel(i - 1, foodRequestAmount);
            }
        }
    }

}
