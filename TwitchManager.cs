using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net.Sockets;
using UnityEditor;

[CreateAssetMenu(fileName = "TwitchManager", menuName = "Scriptable Objects/Twitch")]
public class TwitchManager : ScriptableObject
{
    [HideInInspector] public TcpClient twitch;
    private StreamReader reader;
    private StreamWriter writer;
    
    private const string url = "irc.chat.twitch.tv";
    private const int port = 6667;

    [SerializeField] private string user;
    [SerializeField] private string oath;
    public string channel; 
    
    [HideInInspector] public string lastLine;
    private List<string> logs = new List<string>();
    private int logsIndex;
    private bool isClearLogs = false;

    private bool healBlockd;
    private bool pcBlocked;
    private bool guardBlocked;
    
    /// <summary>
    /// Will connect the player to twitch IRC - For the moment using my personal account but will later be a bot account
    /// </summary>
    public async void ConnectToTwitch()
    {
        twitch = new TcpClient();
        
        await twitch.ConnectAsync(url, port);
        
        reader = new StreamReader(twitch.GetStream());
        writer = new StreamWriter(twitch.GetStream()) {NewLine = "\r\n", AutoFlush = true};
        
        await writer.WriteLineAsync("PASS "+ oath);
        await writer.WriteLineAsync("NICK "+ user);

        ReadMessage();
    }

    /// <summary>
    /// Join the channel passed in parameters
    /// </summary>
    /// <param name="channelName">The exact channel name you want to join</param>
    public async void JoinChannel(string channelName)
    {
        await writer.WriteLineAsync("JOIN #" + channelName);
    }

    /// <summary>
    /// Leave the channel passed in parameters
    /// </summary>
    /// <param name="channelName">The exact channel name you want to leave</param>
    public async void LeaveChannel(string channelName)
    {
        await writer.WriteLineAsync("PART #" + channelName);
    }
    
    /// <summary>
    /// Will read the message in the chat in which channel we are connected
    /// </summary>
    private async void ReadMessage()
    {
        logs.Clear();
        logsIndex = 1;
        while (true)
        {
            if (isClearLogs) // CLear the logs
            {
                logs.Clear();
                logsIndex = 1;
                isClearLogs = false; //Everything linked to the logs are/is reset
                
                logs.Add(lastLine);
            }
            
            
            lastLine = await reader.ReadLineAsync();
            logs.Add(lastLine);
            if (lastLine.Contains("PRIVMSG"))
            {
                var t = GetMessageDetails(lastLine); //Temporary variable for message and user info

                if (t[1].Contains("!"))
                {
                    GetCommands(t[0],t[1]);
                }
                else //Debug ONLY
                {
                    Debug.Log(t[0] + " : " + t[1]);
                }
                
                
            }
            else
            {
                // Debug.Log(lastLine);
            }

            if (lastLine != null && lastLine.StartsWith("PING"))
            {
                lastLine.Replace("PING", "PONG");
                await writer.WriteLineAsync(lastLine);
            }
            #if UNITY_EDITOR
                if (!EditorApplication.isPlaying)   //EDITOR ONLY
                {
                    LeaveChannel(channel);
                    //TODO - Cancel await readLine
                    return;
                }   
            #endif

            
        }
    }

    /// <summary>
    /// Set isClearLogs to true
    /// </summary>
    public void ClearLogs()
    {
        isClearLogs = true;
    }
    
    /// <summary>
    /// Will check if there is a new message in the chat and return true and the message if there is a new one
    /// </summary>
    /// <param name="newMessage">The last message in the chat</param>
    /// <returns>If true then there is a new message in the chat</returns>
    public bool NewTwitchMessage(out string newMessage)
    {
        if (logs.Count < logsIndex)
        {
            newMessage = "";
            return false;
        }

        for (int i = logsIndex; i < logs.Count; i++)
        {
            if (logs[i -1].Contains("PRIVMSG"))
            {
                logsIndex = i + 1;
                newMessage = logs[i - 1];
                return true;
            }
        }

        logsIndex = logs.Count + 1;
        newMessage = "";
        return false;

    }
    
    
    /// <summary>
    /// Will send a message in the designated channel
    /// </summary>
    /// <param name="channelName">The exact name of the channel you want to write your message</param>
    /// <param name="messageToSend">The message you want to send to the channel's chat</param>
    public async void WriteToChannel(string channelName, string messageToSend)
    {
        await writer.WriteLineAsync($"PRIVMSG #{channelName} :{messageToSend}");
    }

    /// <summary>
    /// Get the message details like the username and the message itself (without any other information)
    /// </summary>
    /// <param name="twitchMessage"></param>
    /// <returns></returns>
    public string[] GetMessageDetails(string twitchMessage)
    {
        if (twitchMessage == null || !twitchMessage.Contains("PRIVMSG"))
            return new string[1] {""};

        int splitpoint = twitchMessage.IndexOf("!");
        string chatUser = twitchMessage.Substring(1, splitpoint - 1);

        splitpoint = twitchMessage.IndexOf(":",1);
        string userMessage = twitchMessage.Substring(splitpoint + 1);

        return new string[2] { chatUser, userMessage };
    }


    public void ResetBlock()
    {
        healBlockd = false;
        pcBlocked = false;
        guardBlocked = false;
    }
    
    public void BlockAlliesActions(int index)
    {
        switch (index)
        {
            case 0:
                healBlockd = true;
                break;
            case 1:
                pcBlocked = true;
                break;
            case 2:
                guardBlocked = true;
                break;
            default:
                break;
        }
    }
    
    
    /// <summary>
    /// Will call the right commands depending on which one the viewer used
    /// </summary>
    /// <param name="message">The message of the viewer who used the command</param>*
    public void GetCommands(string user, string message)
    {
        
        if (message.Contains(" ")) //If there is a message after the command, then the command became invalid
            return;

        int splitPoint = message.Length;

        string command = message.Substring(1,splitPoint-1);
        string commandList;
        switch (command)
        { 
            case "commandes": //Maybe adapt the list of commands depending on the team in which the viewer is
                if (TwitchDataManager.instance.UserAlreadyInTeam(user))
                {
                    if (TwitchDataManager.instance.alliesList.Contains(user))
                    {
                        commandList = "Liste des commandes disponibles pour les alliés: !soutien : En tant qu'alliés vous permet de soigner le joueur      !PC : En tant qu'alliés vous permet de faire récupérer des PC au joueur        !garde : En tant qu'alliés vous permet de protéger le joueur pendant un tour réduisant les dégâts subits";
                    }
                    else
                    {
                        commandList = "Liste des commandes disponibles pour les ennemis: !monstre + chiffre : En tant qu'ennemis permet de choisir le monstre que vous allez contrôler";

                        if (TwitchDataManager.instance.IsInMonster(user))
                        {
                            commandList =
                                "Liste des commandes disponibles pour les monstres: !monstre : Permet de savoir quel ennemi vous contrôlez       !attaque : En tant qu'ennemis vous permet de lancer une attaque sur le joueur avec le monstre que vous contrôlez        !soin : En tant qu'ennemis vous permet de soigner le monstre que vous controllez";
                        }
                    }
                }
                else
                {
                    commandList = "Liste des commandes disponibles : !allies : Permet de rejoindre les alliés du streamer si vous n'êtes dans aucune équipe     !ennemis : Permet de rejoindre les ennemis du streamer si vous n'êtes dans aucune équipe";
                }
                              // "!allies : Permet de rejoindre les alliés du streamer si vous n'êtes dans aucune équipe" +
                              // "!ennemis : Permet de rejoindre les ennemis du streamer si vous n'êtes dans aucune équipe" +
                              // "!monstre : Permet de savoir quel ennemi vous contrôlez" +
                              // "!monstre + chiffre : En tant qu'ennemis permet de choisir le monstre que vous allez contrôler" +
                              // "!soutien : En tant qu'alliés vous permet de soigner le joueur" +
                              // "!PC : En tant qu'alliés vous permet de faire récupérer des PC au joueur" +
                              // "!garde : En tant qu'alliés vous permet de protéger le joueur pendant un tour réduisant les dégâts subits" +
                              // "!attaque : En tant qu'ennemis vous permet de lancer une attaque sur le joueur avec le monstre que vous contrôlez" +
                              // "!soin : En tant qu'ennemis vous permet de soigner le monstre que vous controllez";
                WriteToChannel(channel,commandList);
                break;
            case "allies":
                
                if (!TwitchDataManager.instance.UserAlreadyInTeam(user) && VoteManager.instance.isTeamVote)
                    TwitchDataManager.instance.AddAlly(user);
                break;
            case "ennemis":
                
                if (!TwitchDataManager.instance.UserAlreadyInTeam(user)  && VoteManager.instance.isTeamVote)
                    TwitchDataManager.instance.AddEnemy(user);
                break;
            case "monstre":
                if (TwitchDataManager.instance.IsInMonster(user) && TwitchDataManager.instance.enemiesList.Contains(user))
                {
                    WriteToChannel(channel,"Vous controllez le monstre "+TwitchDataManager.instance.WhichMonster(user)); //TODO - Whisper this
                }
                break;
            case "1":
                if (!TwitchDataManager.instance.IsInMonster(user) && TwitchDataManager.instance.enemiesList.Contains(user) && VoteManager.instance.isMonsterVote)
                    TwitchDataManager.instance.AddToMonster(user,command);
                break;
            case "2":
                if (!TwitchDataManager.instance.IsInMonster(user) && TwitchDataManager.instance.enemiesList.Contains(user) && VoteManager.instance.isMonsterVote)
                    TwitchDataManager.instance.AddToMonster(user,command);
                break;
            case "3":
                if (!TwitchDataManager.instance.IsInMonster(user) && TwitchDataManager.instance.enemiesList.Contains(user) && VoteManager.instance.isMonsterVote)
                    TwitchDataManager.instance.AddToMonster(user,command);
                break;
            case "soutien":
                if (VoteManager.instance.isAlliesActionVote && TwitchDataManager.instance.alliesList.Contains(user))
                {
                    if (healBlockd)
                        break;
                    
                    TwitchDataManager.instance.IncreaseUserRank(user);
                    VoteManager.instance.option1Count++;
                }
                if (VoteManager.instance.isEnemyVote && TwitchDataManager.instance.enemiesList.Contains(user))
                {
                    TwitchDataManager.instance.IncreaseUserRank(user);
                    VoteManager.instance.option1Count++;
                }
                break;
            case "PC":
                if (VoteManager.instance.isAlliesActionVote && TwitchDataManager.instance.alliesList.Contains(user))
                {
                    if (pcBlocked)
                        break;
                    
                    TwitchDataManager.instance.IncreaseUserRank(user);
                    VoteManager.instance.option2Count++;
                }
                if (VoteManager.instance.isEnemyVote && TwitchDataManager.instance.enemiesList.Contains(user))
                {
                    TwitchDataManager.instance.IncreaseUserRank(user);
                    VoteManager.instance.option2Count++;
                }
                break;
            case "garde":
                if (VoteManager.instance.isAlliesActionVote && TwitchDataManager.instance.alliesList.Contains(user))
                {
                    if (guardBlocked)
                        break;
                    
                    TwitchDataManager.instance.IncreaseUserRank(user);
                    VoteManager.instance.option3Count++;
                    //TODO 
                }
                break;
            case "attaque":
                if (VoteManager.instance.isMonstersActionsVote && TwitchDataManager.instance.IsInMonster(user))
                {
                    TwitchDataManager.instance.IncreaseUserRank(user);
                    VoteManager.instance.AddVoteToMonsterAction(int.Parse(TwitchDataManager.instance.WhichMonster(user)), 1);
                }
                break;
            case "soin":
                if (VoteManager.instance.isMonstersActionsVote && TwitchDataManager.instance.IsInMonster(user))
                {
                    TwitchDataManager.instance.IncreaseUserRank(user);
                    VoteManager.instance.AddVoteToMonsterAction(int.Parse(TwitchDataManager.instance.WhichMonster(user)), 2);
                }
                break;
            case "go":
                if (VoteManager.instance.isUltimateVote && TwitchDataManager.instance.UserAlreadyInTeam(user))
                {
                    if (TwitchDataManager.instance.alliesList.Contains(user))
                        VoteManager.instance.option1Count++;
                    else
                        VoteManager.instance.option2Count++;
                    
                    TwitchDataManager.instance.IncreaseUserRank(user);
                }
                break;
            //TODO - Create a command for the ultimate of the boss
            default:
                Debug.Log("La commande n'existe pas, faites !commandes pour voir la liste des commandes");
                WriteToChannel(channel, "La commande n'existe pas faites !commandes pour voir la liste des commandes");
                break;
        }
    }
}
