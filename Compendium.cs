using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Realms;
using MongoDB.Bson;
using UnityEngine.Events;

[Serializable]
public class Compendium : RealmObject
{
    [MapTo("_id"),PrimaryKey]
    public ObjectId? Id { get; set; }
    
    [MapTo("_partition")]
    public string Partition { get; set; }
    
    [MapTo("compendium")]
    public string CompendiumJson { get; set; }
    
    public List<Item> compendium = new List<Item>();

    public UnityEvent<Item> OnItemAdded;

    public Compendium()
    {
        Id = ObjectId.GenerateNewId();
        Partition = RealmController.PartitionName;
    }

    public void AddItem(Item item)
    {
        compendium ??= new List<Item>();
        
        if (compendium.Contains(item))
            return;
        
        compendium.Add(item);
        Debug.Log($"{item.name} successfully added to Compendium");
        OnItemAdded?.Invoke(item);
    }
    
    public Item GetItemByName(string name)
    {
        return compendium.First(i => String.Equals(i.name, name, StringComparison.CurrentCultureIgnoreCase));
    }
}
