/*
 * Copyright 2014 The LolDevs team (https://github.com/loldevs)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package net.boreeas.riotapi.com.riotgames.platform.summoner;

import lombok.EqualsAndHashCode;
import lombok.Data;
import net.boreeas.riotapi.rtmp.serialization.Serialization;

import java.util.ArrayList;
import java.util.Date;
import java.util.List;

/**
 * Created on 7/19/2014.
 */
@Data
@EqualsAndHashCode(of = {"acctId"})
@Serialization(name = "com.riotgames.platform.summoner.Summoner")
public class Summoner {
    private String seasonOneTier;
    private String seasonTwoTier;
    private String previousSeasonHighestTier;
    private String internalName;
    private long acctId;
    private boolean helpFlag;
    private long sumId;
    private int profileIconId;
    private boolean displayEloQuestionaire;
    private Date lastGameDate;
    private boolean advancedTutorialFlag;
    private Date revisionDate;
    private long revisionId;
    private String name;
    private boolean nameChangeFlag;
    private boolean tutorialFlag;
    private List<Object> socialNetworkUserIds = new ArrayList<>();
    private int previousSeasonHighestTeamReward;
}
