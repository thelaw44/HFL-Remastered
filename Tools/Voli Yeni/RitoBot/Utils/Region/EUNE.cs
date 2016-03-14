﻿namespace RitoBot.Utils.Region
{
    using LoLLauncher;
    using System;
    using System.Net;

    public sealed class EUNE : BaseRegion
    {
        public override string ChatName
        {
            get
            {
                return "eun1";
            }
        }

        public override bool Garena
        {
            get
            {
                return false;
            }
        }

        public override string InternalName
        {
            get
            {
                return "EUN1";
            }
        }

        public override string Locale
        {
            get
            {
                return "en_GB";
            }
        }

        public override string Location
        {
            get
            {
                return null;
            }
        }

        public override Uri NewsAddress
        {
            get
            {
                return new Uri("http://eune.leagueoflegends.com/en/rss.xml");
            }
        }

        public override IPAddress[] PingAddresses
        {
            get
            {
                return new IPAddress[] { IPAddress.Parse("95.172.65.1") };
            }
        }

        public override Region PVPRegion
        {
            get
            {
                return Region.EUN;
            }
        }

        public override string RegionName
        {
            get
            {
                return "EUNE";
            }
        }

        public override string SpectatorIpAddress
        {
            get
            {
                return "95.172.65.26:8088";
            }
            set
            {
            }
        }

        public override Uri SpectatorLink
        {
            get
            {
                return new Uri("http://spectator.eu.lol.riotgames.com:8088/observer-mode/rest/");
            }
        }
    }
}

