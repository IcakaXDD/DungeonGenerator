using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public Dictionary<string,int> inventory = new Dictionary<string,int>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventory.Add("knife",1);
        inventory.Add("health potion",1);
        inventory.Add("sword",1);

        AddToDictinoary("knife");

        AddToDictinoary("key",1, true);

        AddToDictinoary("key",1,true);

        //Debug.Log(inventory["knife"]);

        PrintDictionary();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddToDictinoary(string objectToAdd, int quantity = 1, bool isItSpecial = false)
    {     
        if (inventory.ContainsKey(objectToAdd))
        {
            if (isItSpecial)
            {
                Debug.Log("You have this already");
                return;
            }
            else
            {
                if (isItSpecial)
                {
                    Debug.Log("You got a special item");
                }
                inventory[objectToAdd] += quantity;
            }
            
        }
        else
        {
            inventory.Add(objectToAdd, 1);
        }
    }

    public void RemoveInventory(string itemToRemove,int quantity)
    {
        if(inventory.ContainsKey(itemToRemove))
        {
            inventory[itemToRemove] -= quantity;
        }
        if(inventory[itemToRemove] < 1)
        {
            inventory.Remove(itemToRemove);
        }
    }

    public void PrintDictionary()
    {
        foreach (var item in inventory)
        {
            Debug.Log(item.ToString());
        }
    }

    
}
