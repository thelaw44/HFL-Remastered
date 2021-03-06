﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoLLauncher.RiotObjects.Platform.Gameinvite.Contract
{
    public class InvitationGameMetaData : RiotGamesObject
    {
        public override string TypeName
        {
            get { return this.type; }
        }

        private string type = "com.riotgames.platform.gameinvite.contract.InvitationGameMetaData";

        public InvitationGameMetaData()
        {

        }

        public InvitationGameMetaData(Callback callback)
        {
            this.callback = callback;
        }

        public InvitationGameMetaData(TypedObject result)
        {
            base.SetFields(this, result);
        }

        public delegate void Callback(InvitationGameMetaData result);

        private Callback callback;

        public override void DoCallback(TypedObject result)
        {
            base.SetFields(this, result);
            callback(this);
        }

        [InternalName("inviteeStateAsString")]
        public String InviteeState { get; set; }

        [InternalName("summonerName")]
        public String SummonerName { get; set; }

        [InternalName("inviteeState")]
        public String inviteeState { get; set; }

        [InternalName("summonerId")]
        public Double SummonerId { get; set; }
    }
}