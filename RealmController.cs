using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Realms;
using Realms.Sync;
using Realms.Logging;
using logger = Realms.Logging.Logger;
using Realms.Sync.ErrorHandling;
using Realms.Sync.Exceptions;
using System.Linq;
using System.Threading.Tasks;
using GameDevWare.Serialization;
using UnityEngine.Rendering;

public class RealmController : MonoBehaviour
{
    public static RealmController instance;
    
    public Realm realm;
    private readonly string realmAppId = ""; //Deleted for security reason
    private readonly string apiKey = ""; //Deleted for security reason

    [SerializeField]
    private string playerName;
    public Inventory_SO playerInv;
    [SerializeField] private string partition;
    public static string PartitionName;

    [SerializeField] private bool isDebugMode = false;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        InitAsync();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (isDebugMode)
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                AddInventory(playerInv.invP); //Envoie l'inventaire d'un joueur à la base de données en ligne
            }

            if (Input.GetKeyDown(KeyCode.A)) //Ajoute un objet à l'inventaire d'un joueur en local
            {
                playerInv.invP.Player = playerName;
                playerInv.invP.AddItem(Compendium_SO.Instance.c.GetItemByName("Fer"), 6);
                playerInv.invP.InventoryJson = Json.SerializeToString(playerInv.invP.inventory);
                //Debug.Log(playerInv.invP.InventoryJson);
            }

            if (Input.GetKeyDown(KeyCode.C)) //Clear l'inventaire du joueur en local
            {
                playerInv.invP.inventory.Clear();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                GetServerInventory(playerName);
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                UpdateCompendium(Compendium_SO.Instance.c);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                GetServerCompendium();
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Compendium_SO.Instance.c.compendium.Clear();
            }
        }
        
#endif
    }
    
    /// <summary>
    /// Initialisation du Realm et connection à ce dernier en async
    /// </summary>
    private async void InitAsync() //TODO - Appeller cette fonction lorsque que la DB est selectionnée
    {
        PartitionName = partition;
        var app = App.Create(realmAppId); //Initialisation de l'application avec l'ID de l'application MongoDb qui va servir à se connecter à la base de données
        User user = await Get_userAsync(app);
        PartitionSyncConfiguration config = GetConfig(user);
        realm = await Realm.GetInstanceAsync(config);
        Debug.Log("Realm ready !");
    }

    /// <summary>
    /// Récupération du l'utilisateur actuel et connection à l'application en utilisant la clé d'API
    /// </summary>
    /// <param name="app">L'application MongoDB</param>
    /// <returns>L'utilisateur de l'application</returns>
    private async Task<User> Get_userAsync(App app)
    {
        User user = app.CurrentUser;
        if (user == null)
        {
            user = await app.LogInAsync(Credentials.ApiKey(apiKey));
        }

        return user;
    }

    /// <summary>
    /// Définition de la configuration utilisée sur la base de donnée (ici syncrhonisation par partition)
    /// </summary>
    /// <param name="user">L'utilisateur précédemment récupéré</param>
    /// <returns>La configuration de la partition</returns>
    private PartitionSyncConfiguration GetConfig(User user)
    {
        PartitionSyncConfiguration config = new(PartitionName, user) //partitionName est le nom de la partition ici
        {
            ClientResetHandler = new DiscardLocalResetHandler()
            {
                ManualResetFallback = (ClientResetException clientResetException) => clientResetException.InitiateClientReset()
            }
        };

        return config;
    }

    /// <summary>
    /// Ajout ou Update de l'inventaire sur la base de donnée
    /// </summary>
    /// <param name="inv">L'inventaire à ajouter ou update</param>
    public void AddInventory(Inventory inv)
    {
        string playerName = inv.Player;
        SerializedDictionary<Item, int> inventory = inv.inventory;

        if (realm == null)
        {
            Debug.LogError("Realm not ready");
            return;
        }

        var temp = realm.All<Inventory>(); //Récupération de la totalité des inventaire pour vérifier s'il en existe.
        if (!temp.Any())
        {
            Debug.LogError("Error Null");
            return;
        }
        
        var currentInventory = temp.Where(inv => inv.Player == playerName); //On récupère ensuite uniquement l'inventaires lié à un joueur en particulier
        realm.Write(() =>
        {
            if (!currentInventory.Any()) //Si aucun inventaire n'est lié à ce joueur on en créé un avec les données de l'inventaire en local
            {
                realm.Add(new Inventory()
                {
                    Player = playerName,
                    inventory = inventory,
                    InventoryJson = Json.SerializeToString(inventory)
                });
                Debug.Log("Inventory added");
            }
            else //Simple update de l'inventaire si le joueur en possède déjà un
            {
                Debug.Log("Inventory updated");
                var t = currentInventory.First(); // On récupère le premier inventaire de la liste des inventaires liés à ce joueur (Normalement il n'y en a qu'un seul par partition)
                t.inventory = inventory;
                t.InventoryJson = Json.SerializeToString(t.inventory);
            }
        });
    }

    public void UpdateCompendium(Compendium compendium)
    {
        if (realm == null)
        {
            Debug.LogError("Realm not ready");
            return;
        }
        
        var temp = realm.All<Compendium>(); //Récupération de la totalité des compendium pour vérifier s'il en existe.
        if (!temp.Any())
        {
            Debug.LogError("Error Null");
            return;
        }

        var currentCompendium = temp.First();
        realm.Write(() =>
        {
            if (currentCompendium.compendium == null)
            {
                realm.Add(new Compendium()
                {
                    compendium = compendium.compendium,
                    CompendiumJson = Json.SerializeToString(compendium.compendium)
                });
                Debug.Log("Compendium added");
            }
            else
            {
                var t = currentCompendium;
                t.compendium = compendium.compendium;
                t.CompendiumJson = Json.SerializeToString(t.compendium);
                Debug.Log("Compendium Updated");
            }
        });


    }
    
    /// <summary>
    /// Récupère l'inventaire du joueur passé en paramètre de la base de donnée pour actualiser la version en local.
    /// </summary>
    /// <param name="playerName"></param>
    public void GetServerInventory(string playerName)
    {
        
        if (realm == null)
        {
            Debug.Log("Realm not ready");
            return;
        }

        var temp = realm.All<Inventory>(); //Récupération de la totalité des inventaire pour vérifier s'il en existe.
        if (!temp.Any())
        {
            Debug.Log("Error Null");
            return;
        }
        
        var currentInventory = temp.Where(inv => inv.Player == playerName); //On récupère ensuite uniquement l'inventaires lié à un joueur en particulier
        if (!currentInventory.Any())
        {
            Debug.Log("No Inventory to retrieve");
            return;
        }
        playerInv.invP = currentInventory.First();
        playerInv.invP.inventory = Json.Deserialize<SerializedDictionary<Item, int>>(currentInventory.First().InventoryJson);
        Debug.Log($"Inventory of {playerName} have been updated");
    }

    public void GetServerCompendium()
    {
        if (realm == null)
        {
            Debug.Log("Realm not ready");
            return;
        }
        
        var temp = realm.All<Compendium>(); //Récupération de la totalité des compendium pour vérifier s'il en existe.
        if (!temp.Any())
        {
            Debug.Log("Error Null");
            return;
        }

        var currentCompendium = temp.First();
        if (currentCompendium == null)
        {
            Debug.LogError("No Compendium to retreive");
            return;
        }

        Compendium_SO.Instance.c = currentCompendium;
        Compendium_SO.Instance.c.compendium = Json.Deserialize<List<Item>>(currentCompendium.CompendiumJson);
        Debug.Log("Compendium updated from database");
    }
    
    /// <summary>
    /// Force la déconnexion à la base de donnée
    /// </summary>
    public void Terminate()
    {
        realm?.Dispose();
    }
}
