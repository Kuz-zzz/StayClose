namespace StayClose
{
    internal class Config
    {
        public bool killEveryone = false;
        public bool enableTeams = true;
        public int secondsUntilDeath = 3;
        public int distance = 20;

        public static Config DefaultConfig()
        {
            Config vConf = new Config
            {
                killEveryone = false,
                enableTeams = true,
                secondsUntilDeath = 3,
                distance = 20,
    };

            return vConf;
        }
    }
}
