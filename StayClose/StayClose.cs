using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;


namespace StayClose
{
    [ApiVersion(2, 1)]
    public class StayClose : TerrariaPlugin
    {
        public override string Author => "Kuz_";
        public override string Description => "Kills players if they get too far from each other";
        public override string Name => "StayClose";
        public override Version Version => new Version(1, 0, 0, 0);
        internal static string Filepath { get { return Path.Combine(TShock.SavePath, "stayclose.json"); } }

        private static Config config;
        
        public static Dictionary<string, System.Timers.Timer> Timers = new Dictionary<string, System.Timers.Timer>();

        public StayClose(Main game) : base(game) { }


        public override void Initialize()
        {
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
            GetDataHandlers.Teleport += OnTeleport;
            ReadConfig(Filepath, Config.DefaultConfig(), out config);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
                GetDataHandlers.Teleport -= OnTeleport;
            }
            base.Dispose(disposing);
        }


        private void OnPlayerUpdate(object? _, GetDataHandlers.PlayerUpdateEventArgs args)
        {
            OnUpdate();
        }

        private void OnTeleport(object? _, GetDataHandlers.TeleportEventArgs args)
        {
            OnUpdate();
        }

        public void OnUpdate()
        {
            if (TShock.Players.Length > 2)
            {
                TSPlayer[] players = TShock.Players;
                if (Timers.Keys.Count != 0)
                {
                    CheckLostPlayers();
                }

                // Go through all players and check if there's anybody close to them
                foreach (TSPlayer player in players)
                {
                    if (player == null || !player.RealPlayer || player.Dead)
                    {
                        continue;
                    }
                    Vector2 pos1 = player.LastNetPosition;
                    bool isFar = true;
                    foreach (TSPlayer player2 in players)
                    {
                        // Check if the player is real and alive
                        if (player2 == null || !player2.RealPlayer || player2.Dead || player == player2)
                        {
                            continue;
                        }
                        // If teams are enabled, we skip players that aren't on the same team
                        if (config.enableTeams)
                        {
                            if (player.Team != player2.Team)
                            {
                                continue;
                            }
                        }

                        Vector2 pos2 = player2.LastNetPosition;
                        // Measure distance
                        if (Math.Sqrt(Math.Pow(pos2.X - pos1.X, 2) + Math.Pow(pos2.Y - pos1.Y, 2)) <= config.distance * 16)
                        {
                            isFar = false;
                            break;
                        }
                    }
                    // Check if the death timer is already running
                    if (!Timers.ContainsKey(player.Name))
                    {
                        if (isFar)
                        {
                            if (config.secondsUntilDeath == 0)
                            {
                                if (config.killEveryone)
                                {
                                    foreach (TSPlayer playa in TShock.Players)
                                    {
                                        playa.KillPlayer();
                                    }
                                    TShock.Utils.Broadcast(player.Name + " went too far!", Color.Red);
                                    break;
                                }
                                else
                                {
                                    player.KillPlayer();
                                    TShock.Utils.Broadcast(player.Name + " went too far!", Color.Red);
                                }
                            }
                            else
                            {
                                player.SendMessage("You went too far from other players! You have "
                               + config.secondsUntilDeath + " seconds to get back!", Color.OrangeRed);
                                var timer = new System.Timers.Timer();
                                timer.Interval = config.secondsUntilDeath * 1000;
                                timer.Elapsed += (sender, args) => TimerElapsed(sender, args, player.Name);
                                timer.Enabled = true;
                                Timers.Add(player.Name, timer);
                            }
                        }
                    }

                }
            }
        }

        internal static void TimerElapsed(object? sender, ElapsedEventArgs args, string playerName)
        {
            
            if (config.killEveryone)
            {
                foreach (TSPlayer player in TShock.Players)
                {
                    if (config.enableTeams)
                    {
                        if (player.Team != TSPlayer.FindByNameOrID(playerName)[0].Team)
                        {
                            continue;
                        }
                    }
                    if (player != null && player.RealPlayer && !player.Dead)
                    {
                        player.KillPlayer();
                    }
                        

                }
                TShock.Utils.Broadcast(playerName + " went too far!", Color.Red);
            }
            else
            {
                TSPlayer.FindByNameOrID(playerName)[0].KillPlayer();
                TShock.Utils.Broadcast(playerName + " went too far!", Color.Red);
                
            }
            Timers[playerName].Dispose();
            Timers.Remove(playerName);
        }

        // Check if lost players returned to others in time
        private void CheckLostPlayers()
        {
            TSPlayer[] players = TShock.Players;
            foreach (string key in Timers.Keys)
            {
                TSPlayer player = TSPlayer.FindByNameOrID(key)[0];
                Vector2 pos1 = player.LastNetPosition;
                bool isFar = true;
                foreach (TSPlayer player2 in players)
                {
                    // Check if the player is real and alive
                    if (player2 == null || !player2.RealPlayer || player2.Dead || player == player2)
                    {
                        continue;
                    }
                    // If teams are enabled, we skip players that aren't on the same team
                    if (config.enableTeams)
                    {
                        if (player.Team != player2.Team)
                        {
                            continue;
                        }
                    }
                    Vector2 pos2 = player2.LastNetPosition;
                    // Measure distance
                    if (Math.Sqrt(Math.Pow(pos2.X - pos1.X, 2) + Math.Pow(pos2.Y - pos1.Y, 2)) <= config.distance*16)
                    {
                        isFar = false;
                        break;
                    }
                }
                // If the lost player have successfully returned to others, dispose of the timer remove the reference from Timers
                if (!isFar)
                {
                    Timers[key].Dispose();
                    Timers.Remove(key);
                    player.SendMessage("You are safe now", Color.LawnGreen);
                }
            }
        }

        private void ReadConfig<TConfig>(string path, TConfig defaultConfig, out TConfig config)
        {
            if (!File.Exists(path))
            {
                config = defaultConfig;
                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            else
            {
                try
                {
                    config = JsonConvert.DeserializeObject<TConfig>(File.ReadAllText(path));
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError("Config could not load: " + ex.Message);
                    TShock.Log.ConsoleError("Falling back to default configuration...");
                    TShock.Log.ConsoleError(ex.StackTrace);
                    config = defaultConfig;
                }
                
            }
        }
    }
}