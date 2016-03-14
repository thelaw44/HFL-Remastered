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

package net.boreeas.riotapi.com.riotgames.platform.login;

import lombok.Data;
import lombok.Setter;
import lombok.ToString;
import net.boreeas.riotapi.rtmp.serialization.Serialization;

/**
 * Created by malte on 7/15/2014.
 */
@Data
@Setter
@ToString
@Serialization(name = "com.riotgames.platform.login.AuthenticationCredentials")
public class AuthenticationCredentials {
    private String locale;
    private String macAddress;
    private Object oldPassword;
    private String domain;
    private String clientVersion;
    private String operatingSystem;
    private String authToken;
    private Object partnerCredentials;
    private String password;
    private String username;
    private Object securityAnswer;
    //private String ipAddress;
}
