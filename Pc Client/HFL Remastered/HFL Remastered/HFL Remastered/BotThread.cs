﻿using LoLLauncher;
using LoLLauncher.RiotObjects.Platform.Catalog.Champion;
using LoLLauncher.RiotObjects.Platform.Clientfacade.Domain;
using LoLLauncher.RiotObjects.Platform.Game;
using LoLLauncher.RiotObjects.Platform.Gameinvite.Contract;
using LoLLauncher.RiotObjects.Platform.Game.Message;
using LoLLauncher.RiotObjects.Platform.Matchmaking;
using LoLLauncher.RiotObjects.Platform.Statistics;
using LoLLauncher.RiotObjects;
using LoLLauncher.RiotObjects.Leagues.Pojo;
using LoLLauncher.RiotObjects.Platform.Game.Practice;
using LoLLauncher.RiotObjects.Platform.Harassment;
using LoLLauncher.RiotObjects.Platform.Leagues.Client.Dto;
using LoLLauncher.RiotObjects.Platform.Login;
using LoLLauncher.RiotObjects.Platform.Reroll.Pojo;
using LoLLauncher.RiotObjects.Platform.Statistics.Team;
using LoLLauncher.RiotObjects.Platform.Summoner;
using LoLLauncher.RiotObjects.Platform.Summoner.Boost;
using LoLLauncher.RiotObjects.Platform.Summoner.Masterybook;
using LoLLauncher.RiotObjects.Platform.Summoner.Runes;
using LoLLauncher.RiotObjects.Platform.Summoner.Spellbook;
using LoLLauncher.RiotObjects.Team;
using LoLLauncher.RiotObjects.Team.Dto;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using LoLLauncher.RiotObjects.Platform.Game.Map;
using System.Net;
using System.Net.Http;
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using LoLLauncher.RiotObjects.Platform.Summoner.Icon;
using System.Windows.Interop;
using LoLLauncher.RiotObjects.Platform.Catalog.Icon;
using System.Timers;
using LoLLauncher.RiotObjects.Platform.Gameinvite.Contract;
using System.Windows.Documents;

namespace HFL_Remastered
{
    public class BotThread
    {
        // Callbacks
        public Process exeProcess;
        public ChampionDTO[] availableChampsArray;
        public LoginDataPacket loginPacket = new LoginDataPacket();
        public LoLConnection connection = new LoLConnection();

        // Summoner Init Values
        public string username;
        public string password;
        public Region region;
        public QueueTypes queue;
        public int desiredLevel;
        public Smurf smurf;
        public string regionVersion;

        //Summoner Group Values
        public bool groupMember { get; set; }
        public double smurfGroup {get;set;}
        public bool isHost { get; set; }

        // Summoner Dynamic Values
        public double currentLevel { get; set; }
        public double currentExp { get; set; }
        public double toNextLevel { get; set; }
        public string dynamicSummonerName { get; set; }
        public double summonerId { get; set; }
        public QueueTypes mustQueue { get; set; }

        // File Settings
        public string lolPath { get; set; }
        public string gamePath { get; set; }
        public string dllPath { get; set; }
        public double rpBalance { get; set; }

        // Imported Dlls
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr Handle, int Msg, int wParam, int lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        //Dynamic Variables
        public bool firstTimeInQueuePop = true;
        public bool firstTimeInLobby = true;
        public int m_leaverBustedPenalty { get; set; }
        public int DodgePenaltyRemainingTime { get; set; }
        public string m_accessToken { get; set; }
        public MatchMakerParams lastMatchParams { get; set; }
        public List<double> lobbyInviteQuery = new List<double>();
        public bool lobbyReady = false;
        public ProcessStartInfo lastStarter = null;

        public void init(string _username, string _password, int _desiredLevel, Region _region, QueueTypes _queue, Smurf _smurf, string _regionVersion)
        {
            username = _username;
            password = _password;
            desiredLevel = _desiredLevel;
            region = _region;
            queue = _queue;
            smurf = _smurf;
            regionVersion = _regionVersion;
            smurfGroup = smurfGroup;

            connection.OnConnect += new LoLConnection.OnConnectHandler(connection_OnConnect);
            connection.OnDisconnect += new LoLConnection.OnDisconnectHandler(connection_OnDisconnect);
            connection.OnError += new LoLConnection.OnErrorHandler(connection_OnError);
            connection.OnLogin += new LoLConnection.OnLoginHandler(connection_OnLogin);
            connection.OnLoginQueueUpdate += new LoLConnection.OnLoginQueueUpdateHandler(connection_OnLoginQueueUpdate);
            connection.OnMessageReceived += new LoLConnection.OnMessageReceivedHandler(connection_OnMessageReceived);
        }

        public async void start()
        {
            connection.Connect(username, password, region, regionVersion);
        }

        //Event Handslers
        #region OnConnect
        public async void connection_OnConnect(object sender, object message)
        {
            Logger.Log newLog = new Logger.Log("info");
            newLog.Smurf = this.username;
            newLog.Text = "Smurf connected to Riot Server, ready to login.";
            Logger.Push(newLog);
        }
        #endregion
        #region OnDisconnect
        public async void connection_OnDisconnect(object sender, object message)
        {
            SmurfManager.stopSmurf(smurf);
        }
        #endregion
        #region OnError
        private void connection_OnError(object sender, LoLLauncher.Error error)
        {
            if (error.Message.Contains("Wrong client version for server"))
            {
                connection.Disconnect();
            }
            else if (error.Message.Contains("The given key was not present in the dictionary."))
            {
                // TODO Solve this error
            }
            else if (error.Message.Contains("Game was not found!"))
            {
                Logger.Log errorLog = new Logger.Log("warning");
                errorLog.Smurf = username;
                errorLog.Text = "Somebody broke the game queue.";
                Logger.Push(errorLog);
            }
            else
            {
                Logger.Log errorLog = new Logger.Log("warning");
                errorLog.Smurf = username;
                errorLog.Text = "Unhandled error message recieved from server:" + error.Message;
                Logger.Push(errorLog);
            }
        }
        #endregion
        #region OnLogin
        private void connection_OnLogin(object sender, string username, string ipAddress)
        {
            Logger.Log newLog = new Logger.Log("info");
            newLog.Smurf = this.username;
            newLog.Text = "Smurf logged in, ready to queue";
            Logger.Push(newLog);

            new Thread((ThreadStart)(async () =>
            {
                loginPacket = await connection.GetLoginDataPacketForUser();
                this.RegisterNotifications();
                if (loginPacket.AllSummonerData == null)
                {
                    Random rnd = new Random();
                    String summonerName = username;
                    if (summonerName.Length > 16)
                        summonerName = summonerName.Substring(0, 12) + new Random().Next(1000, 9999).ToString();
                    LoLLauncher.RiotObjects.Platform.Summoner.AllSummonerData sumData = await connection.CreateDefaultSummoner(summonerName);
                    loginPacket.AllSummonerData = sumData;

                    Logger.Log sNameLog = new Logger.Log("success");
                    sNameLog.Smurf = this.username;
                    sNameLog.Text = "Smurf summoner name updated as" + summonerName;
                    Logger.Push(sNameLog);
                }
                currentLevel = loginPacket.AllSummonerData.SummonerLevel.Level;
                currentExp = loginPacket.AllSummonerData.SummonerLevelAndPoints.ExpPoints;
                toNextLevel = loginPacket.AllSummonerData.SummonerLevel.ExpToNextLevel;
                dynamicSummonerName = loginPacket.AllSummonerData.Summoner.Name;
                summonerId = loginPacket.AllSummonerData.Summoner.SumId;
                rpBalance = loginPacket.RpBalance;
                updateSmurfInfo();

                if (currentLevel >= desiredLevel)
                {
                    Logger.Log sNameLog = new Logger.Log("success");
                    sNameLog.Smurf = this.username;
                    sNameLog.Text = "Smurfins is done, smurf is level: " + (int)currentLevel;
                    Logger.Push(sNameLog);
                    if (!smurf.groupMember) { 
                        connection.Disconnect();
                    }
                }

                if (rpBalance == 400.0 && App.Client.UserData.Settings.BuyBoost)
                {
                    Logger.Log bootBuyLog = new Logger.Log("info");
                    bootBuyLog.Smurf = this.username;
                    bootBuyLog.Text = "Buying xp boost.";
                    Logger.Push(bootBuyLog);
                    try
                    {
                        Task t = new Task(buyBoost);
                        t.Start();
                    }
                    catch (Exception exception)
                    {
                        Logger.Log boostFailLog = new Logger.Log("warning");
                        boostFailLog.Smurf = this.username;
                        boostFailLog.Text = "Failed to buy boost.";
                        Logger.Push(boostFailLog);
                    }
                }

                availableChampsArray = await connection.GetAvailableChampions();
                LoLLauncher.RiotObjects.Team.Dto.PlayerDTO player = await connection.CreatePlayer();

                if (this.loginPacket.ReconnectInfo != null && this.loginPacket.ReconnectInfo.Game != null)
                {
                    this.connection_OnMessageReceived(sender, (object)this.loginPacket.ReconnectInfo.PlayerCredentials); //Oyun varsa reconnect paketini yonlendir
                }
                else
                {
                    if (!smurf.groupMember)
                    {
                        this.joinQueue();
                    }
                    else
                    {
                        if (smurf.isHost) { 
                            this.createLobby();
                        }
                        else
                        {
                            Logger.Log inviteReqToHost = new Logger.Log("info");
                            inviteReqToHost.Smurf = username;
                            inviteReqToHost.Text = "Requesting host to invite me.";
                            Logger.Push(inviteReqToHost);
                            smurf.hostCallback.inviteMe(summonerId);
                        }
                    }
                    
                }
            })).Start();

        }
        #endregion
        #region OnLoginQueueUpdate
        private void connection_OnLoginQueueUpdate(object sender, int positionInLine)
        {
            if (positionInLine <= 0)
            {
                return;
            }
            else
            {
                Logger.Log newLog = new Logger.Log("info");
                newLog.Smurf = this.username;
                newLog.Text = "Position to login: " + positionInLine;
                Logger.Push(newLog);
            }
        }
        #endregion
        #region OnMessage
        public async void connection_OnMessageReceived(object sender, object message)
        {
            if (message is GameDTO) { handleGameDTO(message); return; }
            if (message is PlayerCredentialsDto) { handlePlayerCredentialsDto(message); return; }
            if (message is EndOfGameStats) { handleEndOfGameStats(message); return; }
            if (message is SearchingForMatchNotification) { handleSearchingForMatchNotification((SearchingForMatchNotification)message); return; }
            if (message is GameNotification) { handleGameNotification(message); return; }
            if (message is InvitationRequest) { handleInvitationRequest(message); return; }
            if (message is LobbyStatus) { handleLobbyStatus(message); return; }

            //Weird Handling of end game then close game
            if (message.ToString().Contains("EndOfGameStats")) { handleGameEnder(sender, message); return; }
        }

        public async void handleGameDTO(object message)
        {
            GameDTO game = message as GameDTO;
            await connection.SetClientReceivedGameMessage(game.Id, "CHAMP_SELECT_CLIENT");
            switch (game.GameState)
            {
                case "TEAM_SELECT":
                    //In Custom Player Lobby or maybe invite lobby
                    break;
                case "CHAMP_SELECT":
                    if (this.firstTimeInLobby)
                    {
                        firstTimeInLobby = false;
                        // TODO Sr hero selection, change here!
                        if (currentLevel < 6)
                        {
                            Logger.Log limit6levelChamp = new Logger.Log("info");
                            limit6levelChamp.Smurf = username;
                            limit6levelChamp.Text = "Till level 6 we are not allowed to pick from free champions so I will select Ashe";
                            Logger.Push(limit6levelChamp);
                            await connection.SelectChampion(22);
                        }
                        else { 
                            if (queue != QueueTypes.ARAM)
                            {
                                int randomAdc = availableChampsArray.First(champ => (champ.Owned || champ.FreeToPlay) && (champ.ChampionId == 22 || champ.ChampionId == 51 || champ.ChampionId == 42 || champ.ChampionId == 119 || champ.ChampionId == 81 || champ.ChampionId == 104 || champ.ChampionId == 222 || champ.ChampionId == 429 || champ.ChampionId == 96 || champ.ChampionId == 236 || champ.ChampionId == 21 || champ.ChampionId == 133 || champ.ChampionId == 15 || champ.ChampionId == 18 || champ.ChampionId == 29 || champ.ChampionId == 110 || champ.ChampionId == 67)).ChampionId;
                                await connection.SelectChampion(randomAdc);
                            }
                            else
                            {
                                //Aram Logic
                            }
                        }
                        // Spell Selection
                        int spell1, spell2;
                        if (currentLevel >= 4)
                        {
                            if (currentLevel >= 8)
                            {
                                if (currentLevel >= 10)
                                {
                                    spell1 = Enums.spellToId("IGNITE");
                                    spell2 = Enums.spellToId("HEAL");
                                }
                                else
                                {
                                    spell1 = Enums.spellToId("BARRIER");
                                    spell2 = Enums.spellToId("HEAL");
                                }
                            }
                            else
                            {
                                spell1 = Enums.spellToId("BARRIER");
                                spell2 = Enums.spellToId("HEAL");
                            }
                        }
                        else
                        {
                            spell1 = Enums.spellToId("GHOST");
                            spell2 = Enums.spellToId("HEAL");
                        }
                        await connection.SelectSpells(spell1, spell2);
                        if (mustQueue != QueueTypes.ARAM)
                        {
                            await connection.ChampionSelectCompleted();
                        }
                    }
                    break;
                case "POST_CHAMP_SELECT":
                    Logger.Log newLogPOST_CHAMP_SELECT = new Logger.Log("info");
                    newLogPOST_CHAMP_SELECT.Smurf = username;
                    newLogPOST_CHAMP_SELECT.Text = "Waiting for game to start";
                    Logger.Push(newLogPOST_CHAMP_SELECT);
                    break;
                case "PRE_CHAMP_SELECT":
                    Logger.Log newLogPRE_CHAMP_SELECT = new Logger.Log("info");
                    newLogPRE_CHAMP_SELECT.Smurf = username;
                    newLogPRE_CHAMP_SELECT.Text = "Last seconds to set champion.";
                    Logger.Push(newLogPRE_CHAMP_SELECT);
                    break;
                case "GAME_START_CLIENT":
                    Logger.Log newLogGAME_START_CLIENT = new Logger.Log("info");
                    newLogGAME_START_CLIENT.Smurf = username;
                    newLogGAME_START_CLIENT.Text = "Game started";
                    Logger.Push(newLogGAME_START_CLIENT);
                    break;
                case "GameClientConnectedToServer":
                    Logger.Log newLogGameClientConnectedToServer = new Logger.Log("info");
                    newLogGameClientConnectedToServer.Smurf = username;
                    newLogGameClientConnectedToServer.Text = "Game client connected to server.";
                    Logger.Push(newLogGameClientConnectedToServer);
                    break;
                case "IN_QUEUE":
                    Logger.Log newLogIN_QUEUE = new Logger.Log("info");
                    newLogIN_QUEUE.Smurf = username;
                    newLogIN_QUEUE.Text = "Joined queue.";
                    Logger.Push(newLogIN_QUEUE);
                    break;
                case "TERMINATED":
                    Logger.Log newLogTERMINATED = new Logger.Log("info");
                    newLogTERMINATED.Smurf = username;
                    newLogTERMINATED.Text = "Re entering to queue.";
                    Logger.Push(newLogTERMINATED);
                    this.firstTimeInQueuePop = true;
                    break;
                case "JOINING_CHAMP_SELECT":
                    if (this.firstTimeInQueuePop && game.StatusOfParticipants.Contains("1"))
                    {
                        this.firstTimeInQueuePop = false;
                        this.firstTimeInLobby = true;
                        Logger.Log newLogJOINING_CHAMP_SELECT = new Logger.Log("info");
                        newLogJOINING_CHAMP_SELECT.Smurf = username;
                        newLogJOINING_CHAMP_SELECT.Text = "Accepting game";
                        Logger.Push(newLogJOINING_CHAMP_SELECT);
                        await this.connection.AcceptPoppedGame(true);
                    }
                    break;
                case "LEAVER_BUSTED":
                    Logger.Log newLogLEAVER_BUSTED = new Logger.Log("warning");
                    newLogLEAVER_BUSTED.Smurf = username;
                    newLogLEAVER_BUSTED.Text = "You are leaver busted.";
                    Logger.Push(newLogLEAVER_BUSTED);
                    break;
            }
        }

        private void handlePlayerCredentialsDto(object message)
        {
            string str = Enumerable.Last<string>((IEnumerable<string>)Enumerable.OrderBy<string, DateTime>(Directory.EnumerateDirectories((Properties.Settings.Default.lolPath ?? "").Split(new string[] { "lol.launcher.exe" }, StringSplitOptions.None)[0] + "RADS\\solutions\\lol_game_client_sln\\releases\\"), (Func<string, DateTime>)(f => new DirectoryInfo(f).CreationTime))) + "\\deploy\\";
            LoLLauncher.RiotObjects.Platform.Game.PlayerCredentialsDto credentials = message as PlayerCredentialsDto;
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.WorkingDirectory = str;
            if (App.Client.UserData.Settings.DisableGpu)
            {
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }
            startInfo.FileName = "League of Legends.exe";
            startInfo.Arguments = string.Concat(new object[] { "\"8394\" \"LoLLauncher.exe\" \"\" \"", credentials.ServerIp, " ", credentials.ServerPort, " ", credentials.EncryptionKey, " ", credentials.SummonerId, "\"" });
            Logger.Log gameStarterLog = new Logger.Log("info");
            gameStarterLog.Smurf = username;
            gameStarterLog.Text = "Starting game";
            Logger.Push(gameStarterLog);
            lastStarter = startInfo;
            startProcessor(startInfo);
        }

        private void handleEndOfGameStats(object message)
        {
            updateSmurfInfo();
            if (!smurf.groupMember)
            {
                this.joinQueue();
            }
            else
            {
                if (smurf.isHost)
                {
                    this.createLobby();
                }
                else
                {
                    Logger.Log inviteReqToHost = new Logger.Log("info");
                    inviteReqToHost.Smurf = username;
                    inviteReqToHost.Text = "Requesting host to invite me.";
                    Logger.Push(inviteReqToHost);
                    smurf.hostCallback.inviteMe(summonerId);
                }
            }
        }

        private async void handleSearchingForMatchNotification(SearchingForMatchNotification message)
        {

        }

        private void handleGameNotification(object message)
        {
            //throw new NotImplementedException();
        }

        private async void handleInvitationRequest(object message)
        {
            if (smurf.groupMember) {
                Logger.Log parttInviteLog = new Logger.Log("info");
                parttInviteLog.Smurf = username;
                parttInviteLog.Text = "Recieved party from group host, accepting.";
                Logger.Push(parttInviteLog);
                InvitationRequest req = message as InvitationRequest;
                await connection.AcceptInviteForMatchmakingGame(req.InvitationId);
            }
            else
            {
                Logger.Log nonPartyLog = new Logger.Log("warning");
                nonPartyLog.Smurf = username;
                nonPartyLog.Text = "Recieved party request from a friend, not responding...";
                Logger.Push(nonPartyLog);
            }
        }

        private async void handleLobbyStatus(object message)
        {
            LobbyStatus lobby = message as LobbyStatus;
            Logger.Log parttInviteLog = new Logger.Log("info");
            parttInviteLog.Smurf = username;
            int totalMembers = (lobby.Invitees.FindAll(member => member.InviteeState == "ACCEPTED").Count + 1);
            parttInviteLog.Text = "Lobby just updated, accepted members:" + totalMembers;
            Logger.Push(parttInviteLog);

            if (totalMembers == smurf.totalGroupLength)
            {
                Logger.Log readyQueue = new Logger.Log("warning");
                readyQueue.Smurf = username;
                readyQueue.Text = "We have enough members to join queue, joining queue";
                Logger.Push(readyQueue);
                lobbyReady = false;
                lobbyInviteQuery.Clear();

                //Starting
                MatchMakerParams matchParams = new MatchMakerParams();
                if (queue == QueueTypes.INTRO_BOT)
                {
                    matchParams.BotDifficulty = "INTRO";
                }
                else if (queue == QueueTypes.BEGINNER_BOT)
                {
                    matchParams.BotDifficulty = "EASY";
                }
                else if (queue == QueueTypes.MEDIUM_BOT)
                {
                    matchParams.BotDifficulty = "MEDIUM";
                }
                matchParams.QueueIds = new Int32[1] { (int)queue };
                await connection.attachTeamToQueue(matchParams);
            }
        }

        private async void handleGameEnder(object sender, object message)
        {
            EndOfGameStats eog = new EndOfGameStats();
            connection_OnMessageReceived(sender, eog);
            exeProcess.Exited -= exeProcess_Exited;
            exeProcess.Kill();
            Thread.Sleep(500);
            if (exeProcess.Responding)
            {
                Process.Start("taskkill /F /IM \"League of Legends.exe\"");
            }
            loginPacket = await this.connection.GetLoginDataPacketForUser();
            var oldLevel = currentLevel;
            currentLevel = loginPacket.AllSummonerData.SummonerLevel.Level;
            if (oldLevel != currentLevel)
            {
                levelUp();
            }
        }
        #endregion

        //Methods
        public void levelUp()
        {
            updateSmurfInfo();
            rpBalance = loginPacket.RpBalance;
            Logger.Log levelupLog = new Logger.Log("success");
            levelupLog.Smurf = username;
            levelupLog.Text = "Level up, smurf is now level:" + currentLevel;
            Logger.Push(levelupLog);
            if (currentLevel >= desiredLevel && !smurf.groupMember)
            {
                connection.Disconnect();
            }
            else
            {
                this.joinQueue();
            }
            if (rpBalance == 400.0 && App.Client.UserData.Settings.BuyBoost)
            {
                Logger.Log bootBuyLog = new Logger.Log("info");
                bootBuyLog.Smurf = this.username;
                bootBuyLog.Text = "Buying xp boost.";
                Logger.Push(bootBuyLog);
                try
                {
                    Task t = new Task(buyBoost);
                    t.Start();
                }
                catch (Exception exception)
                {
                    Logger.Log boostFailLog = new Logger.Log("warning");
                    boostFailLog.Smurf = this.username;
                    boostFailLog.Text = "Failed to buy boost.";
                    Logger.Push(boostFailLog);
                }
            }
        }
        async void buyBoost()
        {
            try
            {
                if (region.ToString() == "EUW")
                {
                    string url = await connection.GetStoreUrl();
                    HttpClient httpClient = new HttpClient();
                    Console.Out.WriteLine(url);
                    await httpClient.GetStringAsync(url);
                    string storeURL = "https://store." + region.ToString().ToLower() + "1.lol.riotgames.com/store/tabs/view/boosts/1";
                    await httpClient.GetStringAsync(storeURL);
                    string purchaseURL = "https://store." + region.ToString().ToLower() + "1.lol.riotgames.com/store/purchase/item";
                    List<KeyValuePair<string, string>> storeItemList = new List<KeyValuePair<string, string>>();
                    storeItemList.Add(new KeyValuePair<string, string>("item_id", "boosts_2"));
                    storeItemList.Add(new KeyValuePair<string, string>("currency_type", "rp"));
                    storeItemList.Add(new KeyValuePair<string, string>("quantity", "1"));
                    storeItemList.Add(new KeyValuePair<string, string>("rp", "260"));
                    storeItemList.Add(new KeyValuePair<string, string>("ip", "null"));
                    storeItemList.Add(new KeyValuePair<string, string>("duration_type", "PURCHASED"));
                    storeItemList.Add(new KeyValuePair<string, string>("duration", "3"));
                    HttpContent httpContent = new FormUrlEncodedContent(storeItemList);
                    await httpClient.PostAsync(purchaseURL, httpContent);
                    Logger.Log boostBought = new Logger.Log("success");
                    boostBought.Smurf = this.username;
                    boostBought.Text = "Bought 3 days xp boost.";
                    Logger.Push(boostBought);
                    httpClient.Dispose();
                }
                else if (region.ToString() == "EUNE")
                {
                    string url = await connection.GetStoreUrl();
                    HttpClient httpClient = new HttpClient();
                    Console.Out.WriteLine(url);
                    await httpClient.GetStringAsync(url);
                    string storeURL = "https://store." + region.ToString().Substring(0, 3).ToLower() + "1.lol.riotgames.com/store/tabs/view/boosts/1";
                    await httpClient.GetStringAsync(storeURL);
                    string purchaseURL = "https://store." + region.ToString().Substring(0, 3).ToLower() + "1.lol.riotgames.com/store/purchase/item";
                    List<KeyValuePair<string, string>> storeItemList = new List<KeyValuePair<string, string>>();
                    storeItemList.Add(new KeyValuePair<string, string>("item_id", "boosts_2"));
                    storeItemList.Add(new KeyValuePair<string, string>("currency_type", "rp"));
                    storeItemList.Add(new KeyValuePair<string, string>("quantity", "1"));
                    storeItemList.Add(new KeyValuePair<string, string>("rp", "260"));
                    storeItemList.Add(new KeyValuePair<string, string>("ip", "null"));
                    storeItemList.Add(new KeyValuePair<string, string>("duration_type", "PURCHASED"));
                    storeItemList.Add(new KeyValuePair<string, string>("duration", "3"));
                    HttpContent httpContent = new FormUrlEncodedContent(storeItemList);
                    await httpClient.PostAsync(purchaseURL, httpContent);
                    Logger.Log boostBought = new Logger.Log("success");
                    boostBought.Smurf = this.username;
                    boostBought.Text = "Bought 3 days xp boost.";
                    Logger.Push(boostBought);
                    httpClient.Dispose();
                }
                else if (region.ToString() == "NA")
                {
                    string url = await connection.GetStoreUrl();
                    HttpClient httpClient = new HttpClient();
                    Console.Out.WriteLine(url);
                    await httpClient.GetStringAsync(url);
                    string storeURL = "https://store." + region.ToString().ToLower() + "2.lol.riotgames.com/store/tabs/view/boosts/1";
                    await httpClient.GetStringAsync(storeURL);
                    string purchaseURL = "https://store." + region.ToString().ToLower() + "2.lol.riotgames.com/store/purchase/item";
                    List<KeyValuePair<string, string>> storeItemList = new List<KeyValuePair<string, string>>();
                    storeItemList.Add(new KeyValuePair<string, string>("item_id", "boosts_2"));
                    storeItemList.Add(new KeyValuePair<string, string>("currency_type", "rp"));
                    storeItemList.Add(new KeyValuePair<string, string>("quantity", "1"));
                    storeItemList.Add(new KeyValuePair<string, string>("rp", "260"));
                    storeItemList.Add(new KeyValuePair<string, string>("ip", "null"));
                    storeItemList.Add(new KeyValuePair<string, string>("duration_type", "PURCHASED"));
                    storeItemList.Add(new KeyValuePair<string, string>("duration", "3"));
                    HttpContent httpContent = new FormUrlEncodedContent(storeItemList);
                    await httpClient.PostAsync(purchaseURL, httpContent);
                    Logger.Log boostBought = new Logger.Log("success");
                    boostBought.Smurf = this.username;
                    boostBought.Text = "Bought 3 days xp boost.";
                    Logger.Push(boostBought);
                    httpClient.Dispose();
                }
                else
                {
                    string url = await connection.GetStoreUrl();
                    HttpClient httpClient = new HttpClient();
                    Console.Out.WriteLine(url);
                    await httpClient.GetStringAsync(url);
                    string storeURL = "https://store." + region.ToString().ToLower() + ".lol.riotgames.com/store/tabs/view/boosts/1";
                    await httpClient.GetStringAsync(storeURL);
                    string purchaseURL = "https://store." + region.ToString().ToLower() + ".lol.riotgames.com/store/purchase/item";
                    List<KeyValuePair<string, string>> storeItemList = new List<KeyValuePair<string, string>>();
                    storeItemList.Add(new KeyValuePair<string, string>("item_id", "boosts_2"));
                    storeItemList.Add(new KeyValuePair<string, string>("currency_type", "rp"));
                    storeItemList.Add(new KeyValuePair<string, string>("quantity", "1"));
                    storeItemList.Add(new KeyValuePair<string, string>("rp", "260"));
                    storeItemList.Add(new KeyValuePair<string, string>("ip", "null"));
                    storeItemList.Add(new KeyValuePair<string, string>("duration_type", "PURCHASED"));
                    storeItemList.Add(new KeyValuePair<string, string>("duration", "3"));
                    HttpContent httpContent = new FormUrlEncodedContent(storeItemList);
                    await httpClient.PostAsync(purchaseURL, httpContent);
                    Logger.Log boostBought = new Logger.Log("success");
                    boostBought.Smurf = this.username;
                    boostBought.Text = "Bought 3 days xp boost.";
                    Logger.Push(boostBought);
                    httpClient.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }
        public void startProcessor(ProcessStartInfo startInfo)
        {
            var processStarter = new Thread((ThreadStart)(() =>
            {
                exeProcess = Process.Start(startInfo);
                exeProcess.Exited += exeProcess_Exited;
                while (exeProcess.MainWindowHandle == IntPtr.Zero) ;
                exeProcess.PriorityClass = ProcessPriorityClass.Idle;
                exeProcess.EnableRaisingEvents = true;
                if (App.Client.UserData.Settings.DisableGpu)
                {
                    /* App.gameContainer.Dispatcher.Invoke(new Action(() =>
                     {
                         App.gameContainer.addWindow(exeProcess, Accountname);    //Implement This
                     }), DispatcherPriority.ContextIdle);*/
                }
                Thread.Sleep(3000);
                if (App.Client.UserData.Settings.ManualInjection)
                {
                    BasicInject.Inject(exeProcess, Properties.Settings.Default.bolPath.Split(new string[] { "lol.launcher.exe" }, StringSplitOptions.None)[0] + "tangerine.dll");
                }
            }));
            processStarter.Start();
        }
        private async void joinQueue()
        {
            MatchMakerParams matchParams = new MatchMakerParams();
            if (queue == QueueTypes.INTRO_BOT)
            {
                matchParams.BotDifficulty = "INTRO";
            }
            else if (queue == QueueTypes.BEGINNER_BOT)
            {
                matchParams.BotDifficulty = "EASY";
            }
            else if (queue == QueueTypes.MEDIUM_BOT)
            {
                matchParams.BotDifficulty = "MEDIUM";
            }
            mustQueue = queue;
            if (currentLevel < 3 && queue == QueueTypes.NORMAL_5x5)
            {
                Logger.Log normalWarnLog = new Logger.Log("warning");
                normalWarnLog.Smurf = username;
                normalWarnLog.Text = "Need to be Level 3 before NORMAL_5x5 queue, joining Co-Op vs AI (Beginner) queue until 3.";
                Logger.Push(normalWarnLog);
                mustQueue = QueueTypes.BEGINNER_BOT;
            }
            if (currentLevel < 6 && queue == QueueTypes.ARAM)
            {
                Logger.Log normalWarnLog = new Logger.Log("warning");
                normalWarnLog.Smurf = username;
                normalWarnLog.Text = "Need to be Level 6 before ARAM queue, joining Co-Op vs AI (Beginner) queue until 6.";
                Logger.Push(normalWarnLog);
                mustQueue = QueueTypes.BEGINNER_BOT;
            }
            if (currentLevel < 7 && queue == QueueTypes.NORMAL_3x3)
            {
                Logger.Log normalWarnLog = new Logger.Log("warning");
                normalWarnLog.Smurf = username;
                normalWarnLog.Text = "Need to be Level 7 before NORMAL_3x3 queue, joining Co-Op vs AI (Beginner) queue until 6.";
                Logger.Push(normalWarnLog);
                mustQueue = QueueTypes.BEGINNER_BOT;
            }

            matchParams.QueueIds = new Int32[1] { (int)mustQueue };
            lastMatchParams = matchParams;
            SearchingForMatchNotification message = await connection.AttachToQueue(matchParams);
            if (message.PlayerJoinFailures == null)
            {
                Logger.Log inQueueLog = new Logger.Log("info");
                inQueueLog.Smurf = username;
                inQueueLog.Text = "In Queue: " + mustQueue.ToString();
                Logger.Push(inQueueLog);
            }
            else
            {
                dynamic failure = message.PlayerJoinFailures[0];
                bool taintedWarning = false;
                foreach (QueueDodger current in message.PlayerJoinFailures)
                {

                    if (current.ReasonFailed == "LEAVER_BUSTED")
                    {
                        m_accessToken = current.AccessToken;
                        if (current.LeaverPenaltyMillisRemaining > this.m_leaverBustedPenalty)
                        {
                            this.m_leaverBustedPenalty = current.LeaverPenaltyMillisRemaining;
                        }
                    }

                    if (current.ReasonFailed == "QUEUE_DODGER")
                    {
                        m_accessToken = current.AccessToken;
                        if (current.DodgePenaltyRemainingTime > this.m_leaverBustedPenalty)
                        {
                            this.m_leaverBustedPenalty = current.DodgePenaltyRemainingTime;
                        }
                    }

                    if (current.ReasonFailed == "LEAVER_BUSTER_TAINTED_WARNING")
                    {
                        Logger.Log accountAcceptTos = new Logger.Log("danger");
                        accountAcceptTos.Smurf = username;
                        accountAcceptTos.Text = "Login to your account using your real client and accept the popup you will see";
                        Logger.Push(accountAcceptTos);
                        taintedWarning = true;
                        connection.Disconnect();
                        break;
                    }
                }
                if (!string.IsNullOrEmpty(this.m_accessToken))
                {
                    double minutes = ((float)(this.m_leaverBustedPenalty / 0x3e8)) / 60f;
                    Logger.Log leaveTimeLeft = new Logger.Log("warning");
                    leaveTimeLeft.Smurf = username;
                    leaveTimeLeft.Text = "Waiting out leaver buster: " + minutes + " minutes!";
                    Logger.Push(leaveTimeLeft);
                    Thread.Sleep(TimeSpan.FromMilliseconds((double)this.m_leaverBustedPenalty));
                    try { 
                        message = await connection.AttachToLowPriorityQueue(matchParams, this.m_accessToken);
                        if (message.PlayerJoinFailures == null)
                        {
                            Logger.Log lowerScc = new Logger.Log("info");
                            lowerScc.Smurf = username;
                            lowerScc.Text = "Succesfully joined lower priority queue!";
                            Logger.Push(lowerScc);
                        }
                        else
                        {
                            Logger.Log lowerFail = new Logger.Log("danger");
                            lowerFail.Smurf = username;
                            lowerFail.Text = "There was an error in joining lower priority queue.Disconnecting...";
                            Logger.Push(lowerFail);
                            this.connection.Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                else
                {
                    if (!taintedWarning)
                    { 
                        double minutes = ((float)(this.m_leaverBustedPenalty / 0x3e8)) / 60f;
                        if (minutes <= 0)
                        {
                            try { 
                                exeProcess.Exited -= exeProcess_Exited;
                                exeProcess.Kill();
                            }
                            catch(Exception ex)
                            {
                                
                            }
                            if (lastStarter != null)
                            {
                                startProcessor(lastStarter);
                            }
                        }
                        else
                        {
                            Logger.Log leaveTimeLeft = new Logger.Log("warning");
                            leaveTimeLeft.Smurf = username;
                            leaveTimeLeft.Text = "Waiting out queue buster: " + minutes + " minutes!";
                            Logger.Push(leaveTimeLeft);
                            Thread.Sleep(TimeSpan.FromMilliseconds((double)this.m_leaverBustedPenalty));
                            try { 
                                this.joinQueue();
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }
                }

            }
        }
        public async void lobbyInviteUpdate()
        {
            try {
                List<double> ls = new List<double>();
                if (lobbyReady){
                    foreach(double sumId in lobbyInviteQuery){
                        await connection.Invite(sumId);
                        ls.Add(sumId);
                    }
                    foreach (double sumId in ls)
                    {
                        lobbyInviteQuery.Remove(sumId);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        private async void RegisterNotifications()
        {
            object obj1 = await this.connection.Subscribe("bc", this.connection.AccountID());
            object obj2 = await this.connection.Subscribe("cn", this.connection.AccountID());
            object obj3 = await this.connection.Subscribe("gn", this.connection.AccountID());
        }
        async void CreatePracticeGame()
        {
            LoLLauncher.RiotObjects.Platform.Game.PracticeGameConfig cfg = new LoLLauncher.RiotObjects.Platform.Game.PracticeGameConfig();
            cfg.GameName = "thelawkings" + new Random().Next().ToString();
            LoLLauncher.RiotObjects.Platform.Game.Map.GameMap map = new LoLLauncher.RiotObjects.Platform.Game.Map.GameMap();
            map.Description = "DUBLEBERTAN";
            map.DisplayName = "DUBLEBERTAN";
            map.TotalPlayers = 2;
            map.Name = "dummy";
            map.MapId = (int)GameMode.TwistedTreeline;
            map.MinCustomPlayers = 1;
            cfg.GameMap = map;
            cfg.MaxNumPlayers = 6;
            cfg.GamePassword = "bertan";
            cfg.GameTypeConfig = 1;
            cfg.AllowSpectators = "NONE";
            cfg.GameMode = StringEnum.GetStringValue(GameMode.TwistedTreeline);
            Thread.Sleep(TimeSpan.FromMilliseconds((double)1000 * 20));
            GameDTO game = await connection.CreatePracticeGame(cfg);
            if (game.Id == 0)
            {
                Logger.Log customGameLog = new Logger.Log("warning");
                customGameLog.Text = "Game failed to create";
                customGameLog.Smurf = username;
                Logger.Push(customGameLog);
            }
            else
            {
                Logger.Log customGameLog = new Logger.Log("info");
                customGameLog.Text = "Game (" + game.Id + ") created.";
                customGameLog.Smurf = username;
                Logger.Push(customGameLog);
            }

            
        }
        void exeProcess_Exited(object sender, EventArgs e)
        {
            if (this.loginPacket.ReconnectInfo != null && this.loginPacket.ReconnectInfo.Game != null)
            {
                this.connection_OnMessageReceived(sender, (object)this.loginPacket.ReconnectInfo.PlayerCredentials);
            }
            else
            {
                this.connection_OnMessageReceived(sender, (object)new EndOfGameStats());
            }
        }
        private async void createLobby()
        {
            Logger.Log lobbyCreateLog = new Logger.Log("info");
            lobbyCreateLog.Smurf = username;
            lobbyCreateLog.Text = "Creating lobby for the team";
            Logger.Push(lobbyCreateLog);
            LobbyStatus newLobby;
            if (queue == QueueTypes.INTRO_BOT)
            {
                double lobbyQueue = (double)queue;
                newLobby = await connection.createArrangedBotTeamLobby(lobbyQueue, "INTRO");
            }
            else if (queue == QueueTypes.BEGINNER_BOT)
            {
                double lobbyQueue = (double)queue;
                newLobby = await connection.createArrangedBotTeamLobby(lobbyQueue, "EASY");
            }
            else if (queue == QueueTypes.MEDIUM_BOT)
            {
                double lobbyQueue = (double)queue;
                newLobby = await connection.createArrangedBotTeamLobby(lobbyQueue, "MEDIUM");
            }
            else
            {
                double lobbyQueue = (double)queue;
                newLobby = await connection.CreateArrangedTeamLobby(lobbyQueue);
            }
            if (newLobby != null)
            {
                lobbyReady = true;
                Logger.Log lobbyCreateLogSucc = new Logger.Log("info");
                lobbyCreateLogSucc.Smurf = username;
                lobbyCreateLogSucc.Text = "Lobby created successfully";
                Logger.Push(lobbyCreateLogSucc);
                await connection.Invite(11422666.0);
                lobbyInviteUpdate();
            }
        }
        private async void updateSmurfInfo()
        {
            loginPacket = await connection.GetLoginDataPacketForUser();
            smurf.currentrp = loginPacket.RpBalance;
            smurf.currentLevel = loginPacket.AllSummonerData.SummonerLevel.Level;
            smurf.currentip = loginPacket.IpBalance;
            smurf.updateSelfOnRemote();
        }
    }
}
