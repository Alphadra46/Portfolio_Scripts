using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using CustomUtilities;

[Serializable]
public struct Possibility
{
    public List<TileInfos> hand;
    public bool isBlocked;
    public bool isClosedOnly;
}

[Serializable]
public class Pair : Sequence
{
    public TileInfos Item1;
    public TileInfos Item2;

    public Pair(TileInfos item1, TileInfos item2)
    {
        Item1 = item1;
        Item2 = item2;
        
        itemList.Add(Item1);
        itemList.Add(Item2);
    }
    
    public override string ToString()
    {
        return Item1 + " " + Item2;
    }
}

[Serializable]
public class ThreeOfAKind : Sequence
{
    public TileInfos Item1;
    public TileInfos Item2;
    public TileInfos Item3;

    public ThreeOfAKind(TileInfos item1, TileInfos item2, TileInfos item3)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        
        itemList.Add(Item1);
        itemList.Add(Item2);
        itemList.Add(Item3);
    }
    
    public override string ToString()
    {
        return Item1 + " " + Item2 + " " + Item3;
    }
}

[Serializable]
public class Straight : Sequence
{
    public TileInfos Item1;
    public TileInfos Item2;
    public TileInfos Item3;

    public Straight(TileInfos item1, TileInfos item2, TileInfos item3)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        
        itemList.Add(Item1);
        itemList.Add(Item2);
        itemList.Add(Item3);
    }
    
    public override string ToString()
    {
        return Item1 + " " + Item2 + " " + Item3;
    }
}

[Serializable]
public class FourOfAKind : Sequence
{
    public TileInfos Item1;
    public TileInfos Item2;
    public TileInfos Item3;
    public TileInfos Item4;

    public FourOfAKind(TileInfos item1, TileInfos item2, TileInfos item3, TileInfos item4)
    {
        Item1 = item1;
        Item2 = item2;
        Item3 = item3;
        Item4 = item4;
        
        itemList.Add(Item1);
        itemList.Add(Item2);
        itemList.Add(Item3);
        itemList.Add(Item4);
    }
    
    public override string ToString()
    {
        return Item1 + " " + Item2 + " " + Item3 + " " + Item4;
    }
}

[Serializable]
public class Sequence
{
    public List<TileInfos> itemList = new List<TileInfos>();
}

[ExecuteInEditMode]
public class YakuPossibilitiesCreator : MonoBehaviour
{
    public DataHolder data;
    
    public List<Possibility> specialHands = new List<Possibility>();
    public List<Pair> pairList = new List<Pair>();
    public List<ThreeOfAKind> brelanList = new List<ThreeOfAKind>();
    public List<Straight> straightList = new List<Straight>();
    public List<FourOfAKind> carreList = new List<FourOfAKind>();
    const int MaxTilesHand = 14;
    

    #region Debug

    [ContextMenu("Clear/Clear Pairs")]
    public void ClearPairs()
    {
        pairList.Clear();
        print("Pairs Cleared");
    }
    
    [ContextMenu("Clear/Clear Brelans")]
    public void ClearBrelans()
    {
        brelanList.Clear();
        print("Brelans Cleared");

    }
    
    [ContextMenu("Clear/Clear Straights")]
    public void ClearStraights()
    {
        straightList.Clear();
        print("Straights Cleared");

    }
    
    [ContextMenu("Clear/Clear All")]
    public void ClearAll()
    {
        ClearPairs();
        ClearBrelans();
        ClearStraights();
        print("All Cleared");
    }

    #endregion

    //Pair / Three of a kind / Straight / Four of a kind
    #region Base Small Hands 
    [ContextMenu("Create/Create Pairs")]
    public void CreatePairs()
    {
        pairList.Clear();
        foreach (var family in (Family[])Enum.GetValues(typeof(Family)))
        {
            int maxIndex = family is Family.Wind ? 4 : family is Family.Dragon ? 3 : 9;
            for (int i = 0; i < maxIndex; i++)
            {
                var t = new Pair(
                    new TileInfos() { family = family, value = i + 1 },
                    new TileInfos() { family = family, value = i + 1 });
                
                pairList.Add(t);
                print(t.ToString());
            }
        }
    }
    
    [ContextMenu("Create/Create Brelan")]
    public void CreateBrelan()
    {
        brelanList.Clear();
        foreach (var family in (Family[])Enum.GetValues(typeof(Family)))
        {
            int maxIndex = family is Family.Wind ? 4 : family is Family.Dragon ? 3 : 9;
            for (int i = 0; i < maxIndex; i++)
            {
                var t = new ThreeOfAKind(
                    new TileInfos() { family = family, value = i + 1 },
                    new TileInfos() { family = family, value = i + 1 },
                    new TileInfos() { family = family, value = i + 1 });
                brelanList.Add(t);
                print(t.ToString());
            }
        }
    }
    
    [ContextMenu("Create/Create Straight")]
    public void CreateStraight()
    {
        straightList.Clear();
        foreach (var family in (Family[])Enum.GetValues(typeof(Family)))
        {
            if (family is Family.Dragon or Family.Wind)
                continue;
            
            int maxIndex = 7; //Seven because 7 + 2 = 9 and 8 + 2 = 10 but there isn't a 10th tile
            for (int i = 0; i < maxIndex; i++)
            {
                var t = new Straight(
                    new TileInfos() { family = family, value = i + 1 },
                    new TileInfos() { family = family, value = i + 2 },
                    new TileInfos() { family = family, value = i + 3 });
                straightList.Add(t);
                print(t.ToString());
            }
        }
    }
    
    [ContextMenu("Create/Create Carre")]
    public void CreateCarre()
    {
        carreList.Clear();
        foreach (var family in (Family[])Enum.GetValues(typeof(Family)))
        {
            int maxIndex = family is Family.Wind ? 4 : family is Family.Dragon ? 3 : 9;
            for (int i = 0; i < maxIndex; i++)
            {
                var t = new FourOfAKind(
                    new TileInfos() { family = family, value = i + 1 },
                    new TileInfos() { family = family, value = i + 1 },
                    new TileInfos() { family = family, value = i + 1 },
                    new TileInfos() { family = family, value = i + 1 });
                carreList.Add(t);
                print(t.ToString());
            }
        }
    }
    
    #endregion
    
    [ContextMenu("Create/Create Possibilities")]
    public void CreateSpecialHands()
    {
        OneHanClosedOnly();
        OneHan();
        TwoHan();
        ThreeHan();
        SixHan();
        Yakuman();
    }

    [ContextMenu("Create/Create Data")]
    public void CreateData()
    {
        data.baseSequences.Clear();
        CreatePairs();
        CreateStraight();
        CreateBrelan();
        CreateCarre();
        
        foreach (var family in (Family[])Enum.GetValues(typeof(Family)))
        {
            int maxIndex = family is Family.Wind ? 4 : family is Family.Dragon ? 3 : 9;
            for (int i = 0; i < maxIndex; i++)
            {
                var tInfos = new TileInfos() { family = family, value = i + 1};
                data.baseSequences.Add(tInfos,new Sequences()
                {
                    pair = pairList.First(p => p.Item1 == tInfos),
                    brelan = brelanList.First(b => b.Item1 == tInfos),
                    straights = straightList.Where(s => s.Item1 == tInfos || s.Item2 == tInfos || s.Item3 == tInfos).ToList(),
                    carre = carreList.First(c => c.Item1 == tInfos)
                });
            }
        }
    }

    private void OneHanClosedOnly()
    {
        
    }

    private void OneHan()
    {
        //Tanyao - All simples (A hand composed of only inside (numbers 2-8) tiles)
        
        //Yakuhai - Value tiles (A hand with at least one group of dragon tiles, seat wind, or round wind tiles. This hand can be valued at 1 han for each group)
    }

    private void TwoHan()
    {
        
    }

    private void ThreeHan()
    {
        
    }

    private void SixHan()
    {
        
    }

    private void Yakuman()
    {
        
    }
    
}
