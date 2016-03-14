﻿namespace RitoBot.Utils.Region.Garena
{
    using LoLLauncher;
    using RitoBot.Utils.Region;
    using System;
    using System.Net;

    public sealed class MY : BaseRegion
    {
        public override string ChatName
        {
            get
            {
                return "chat.lol.garenanow.com";
            }
        }

        public override bool Garena
        {
            get
            {
                return true;
            }
        }

        public override string InternalName
        {
            get
            {
                return "MY";
            }
        }

        public override string Locale
        {
            get
            {
                return "en_MY";
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
                return new Uri("http://ll.leagueoflegends.com/landingpage/data/na/en_US.js");
            }
        }

        public override IPAddress[] PingAddresses
        {
            get
            {
                return new IPAddress[0];
            }
        }

        public override Region PVPRegion
        {
            get
            {
                return Region.MY;
            }
        }

        public override string RegionName
        {
            get
            {
                return "prod.lol.garenanow.com";
            }
        }

        public override string SpectatorIpAddress
        {
            get
            {
                return "";
            }
            set
            {
            }
        }

        public override Uri SpectatorLink
        {
            get
            {
                return new Uri("http://203.116.112.222:8088/observer-mode/rest/");
            }
        }
    }
}

