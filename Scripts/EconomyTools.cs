using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EconomyTools : MonoBehaviour {

    public float startingCash;
    public float refundPercentage = .5f; // Percentage to refund when object is deleted
    public GameObject uiCanvas;
    public ConstructionTools constructionTools;
    public LandscapingTools landscapingTools;

    float funds;

	// Use this for initialization
	void Start ()
    {
        funds = startingCash;
	}
	
	// Update is called once per frame
	void Update ()
    {
	}

    public float CalculateCost(GameObject item, int numberOfItems = 1)
    {
        return numberOfItems * item.GetComponent<BuildableObject>().objectPrice;
    }
    public float CalculateCost(GameObject item, List<Tile> tilesToBuild) // Used for paths and fences
    {    
        int numberOfitems = 0;
        foreach(Tile t in tilesToBuild)
        {
            if(t != null)
            {
                numberOfitems++;
            }
        }
        return numberOfitems * item.GetComponent<BuildableObject>().objectPrice;
    }
    public float CalculateCost(float pricePerItem, int numberOfItems = 1)
    {
        return numberOfItems * pricePerItem;
    }
    /// <summary>
    /// Check if the purchase can be made, and, if so, make the purchase. Otherwise, throw an error message based on the type of transaction.
    /// </summary>
    /// <param name="price"></param>
    /// <returns></returns>
    public bool AttemptPurchase(float price)
    {
        if (funds >= price)
        {
            Purchase(price);
            return true;
        }
        else
        {
            // Make this a function for use any time player doesn't have enough money
            GameObject error = Instantiate(Resources.Load<GameObject>("UI/ErrorMessagePanel"), Input.mousePosition, new Quaternion(), uiCanvas.transform);
            error.GetComponentInChildren<Text>().text = "That would cost $" + price + "!";
            // Throw Not enough cash message here "Building that would cost $" + price +"!"
            return false;
        }
    }

    public void Purchase(float price)
    {
        funds -= price;
        // Do some UI stuff here
    }

    public void AddFunds(float fundsToAdd)
    {
        funds += fundsToAdd;
    }

    public float CurrentFunds
    {
        get { return funds; }
        set { funds = value; }
    }

}
