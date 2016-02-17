var express = require('express');
var router = express.Router();
var mongoose = require('mongoose');
var jwt = require('jsonwebtoken');
var ipn = require('paypal-ipn');
var querystring = require('querystring');
var request = require('request');
var net = require('net');
var Table = require('cli-table2');


mongoose.connect("mongodb://127.0.0.1:27017/hflRest");
var db = mongoose.connection;
var fs = require("fs");
var Schema = mongoose.Schema;
var TOKEN_KEY = "Oh!my_ohMibod.?;";
db.on('error', console.error.bind(console, 'connection error:'));
db.once('open', function(callback) {
    console.log("connection to db open")
        //db.db.dropDatabase(); //Db refresher
});
var User = require("../models/user.js");

router.get('/ts', function(req, res, next) {
    res.json({
        ts: Date.now()
    });
});

router.post('/ipn', function(req, res, next) {
    ipn.verify(req.body, {
        'allow_sandbox': true
    }, function callback(err, mes) {
        if (err) {
            console.error(err);
        } else {
            if (req.body.payment_status == 'Completed') {
                console.log(req.body)
            }
        }
    });
});

router.get('/ping', verifyTokenDetectUser, function(req, res, next) {
    res.end("pong");
});

router.post("/remotelogin", function(req, res, next) {
    loginForumBridge(req.body.username, req.body.password, function(login) {
        var resultResponse = {
            forumData: {},
            userData: {},
            token: ""
        };
        if (login.uid) {
            resultResponse.forumData = login;
            User.findOne({
                uid: login.uid
            }, {
                logs: 0
            }, function(err, user) {
                if (err) {
                    res.end("err");
                } else {
                    if (user) {
                        if (req.body.hwid) {
                            if (user.type === 0) {
                                user.testHwid(req.body.hwid, function(r) {
                                    if (r) {
                                        resultResponse.userData = user;
                                        resultResponse.date = Date.now();
                                        resultResponse.token = jwt.sign({
                                            joindate: login.joindate,
                                            uid: login.uid,
                                            removeTime: Date.now() + 1000 * 60 * 30
                                        }, TOKEN_KEY);
                                        res.json(resultResponse);
                                    } else {
                                        res.json({
                                            err: "You have to wait some more to reset your hwid."
                                        });
                                    }
                                });
                            } else {
                                user.testHwid(req.body.hwid, function(r) {
                                    if (r) {
                                        resultResponse.userData = user;
                                        resultResponse.date = Date.now();
                                        resultResponse.token = jwt.sign({
                                            joindate: login.joindate,
                                            uid: login.uid,
                                            removeTime: Date.now() + 1000 * 60 * 30
                                        }, TOKEN_KEY);
                                        res.json(resultResponse);
                                    } else {
                                        res.json({
                                            err: "You have to wait some more to reset your hwid."
                                        });
                                    }
                                });
                            }
                        } else {
                            resultResponse.userData = user;
                            resultResponse.date = Date.now();
                            resultResponse.token = jwt.sign({
                                joindate: login.joindate,
                                uid: login.uid,
                                removeTime: Date.now() + 1000 * 60 * 30
                            }, TOKEN_KEY);
                            res.json(resultResponse);
                        }
                    } else {
                        var newUser = new User({
                            uid: login.uid,
                        });
                        newUser.save(function(err, newUserAppended) {
                            resultResponse.userData = newUserAppended;
                            resultResponse.date = Date.now();
                            resultResponse.token = jwt.sign({
                                joindate: login.joindate,
                                uid: login.uid,
                                removeTime: Date.now() + 1000 * 60 * 30
                            }, TOKEN_KEY);

                            if (req.body.hwid) {
                                User.findOne({
                                    hwid: req.body.hwid
                                }, function(err, hwidDup) {
                                    if (err) {
                                        //console.log(err);
                                    } else {
                                        if (user) {
                                            res.json({
                                                err: "This hwid is belongs to another account."
                                            });
                                        } else {
                                            newUserAppended.testHwid(req.body.hwid, function(r) {
                                                if (r) {
                                                    res.json(resultResponse);
                                                } else {
                                                    res.json({
                                                        err: "You have to wait some more to reset your hwid."
                                                    });
                                                }
                                            });
                                        }
                                    }
                                });
                            } else {
                                res.json(resultResponse);
                            }
                        });
                    }
                }
            })
        } else {
            res.json(login)
        }
    });
});


router.get("/getLogs", verifyTokenDetectUser, function(req, res, next) {
    res.json(req.user);
});

router.get("/getSmurfs", verifyTokenDetectUser, function(req, res, next) {
    res.json(req.user);
});

router.post("/updateSettings", verifyTokenDetectUser, function(req, res, next) {
    req.user.settings = req.body.settings;
    req.user.save();
    res.end();
});

router.post("/updateSmurfs", verifyTokenDetectUser, function(req, res, next) {
    req.user.smurfs = req.body.userData.smurfs;
    req.user.groups = req.body.userData.groups;
    req.user.markModified('smurfs');
    req.user.markModified('groups');
    req.user.save(function(err, updated) {
        //console.log(updated);
        res.json(updated);
    });
});


function loginForumBridge(username, password, callback) {

    var headers = {
        'Content-Type': 'application/x-www-form-urlencoded'
    }

    var options = {
        url: 'http://127.0.0.1:4444/api/ns/login',
        method: 'POST',
        headers: headers,
        form: {
            'username': username,
            'password': password
        }
    }


    request(options, function(error, response, body) {
        if (!error && response.statusCode == 200) {
            callback(JSON.parse(body))
        }
    })
}

function verifyTokenDetectUser(req, res, next) {
    jwt.verify(req.headers.authorization, TOKEN_KEY, function(err, decoded) {
        if (err) {
            res.json({
                type: "dead",
                message: "Token error"
            });
            return false;
        }
        if (!decoded) {
            res.json({
                type: "dead",
                message: "Token decoding error"
            });
            return false;
        }
        if (decoded.removeTime < Date.now()) {
            res.json({
                type: "dead",
                message: "Token expire error"
            });
            return false;
        } else {
            User.findOne({
                uid: decoded.uid
            }, function(err, user) {
                if (err || !user) {
                    res.json({
                        type: "dead",
                        message: "Token user does not exist"
                    });
                    return false;
                } else {
                    req.user = user;
                    decoded.removeTime = Date.now() + 1000 * 60 * 20;
                    var newToken = jwt.sign(decoded, TOKEN_KEY);
                    res.header("Authorization", newToken);
                    next();
                }
            });
        }
    });
}

function verifyTokenDetectUserSocket(token, cb) {
    jwt.verify(token, TOKEN_KEY, function(err, decoded) {
        if (err) {
            cb(false);
        }
        if (!decoded) {
            cb(false);
        }
        if (decoded.removeTime < Date.now()) {
            cb(false);
        } else {
            User.findOne({
                uid: decoded.uid
            }, function(err, user) {
                if (err || !user) {
                    cb(false);
                } else {
                    cb(user);
                }
            });
        }
    });
}



function SocketMap() {
    this.groupList = [];

    this.addSocket = function(ws, type, user) {
        if (!this.groupList[user.uid]) {
            this.groupList[user.uid] = new Group(user);
        } else {
            this.groupList[user.uid].user = user;
        }
        if (type == "controller") {
            this.groupList[user.uid].addController(ws);
        } else if (type == "remote") {
            this.groupList[user.uid].addRemote(ws);
        }
    }

    this.removeSocket = function(ws, type, user) {
        if (this.groupList[user.uid]) {
            if (type == "controller") {
                this.groupList[user.uid].removeController(ws);
            } else if (type == "remote") {
                this.groupList[user.uid].removeRemote(ws);
            }
            if (this.groupList[user.uid].getRemoteCount() + this.groupList[user.uid].getControllerCount() === 0) {
                delete this.groupList[user.uid];
            }
        }
    }

    this.cmdLog = function(ws, data) {
        if (this.groupList[ws.user.uid]) {
            this.groupList[ws.user.uid].cmdOutput(data);
        }
    }

    this.cmdWrite = function(ws, data) {
        if (this.groupList[ws.user.uid]) {
            this.groupList[ws.user.uid].cmdWrite(data);
        }
    }

    this.updateSettings = function(ws, settings) {
        if (this.groupList[ws.user.uid]) {
            this.groupList[ws.user.uid].updateSettings(settings);
        }
    }

    this.pushLog = function(ws, log) {
        if (this.groupList[ws.user.uid]) {
            this.groupList[ws.user.uid].pushLog(log);
        }
    }

    this.event = function(ws, event) {
        if (this.groupList[ws.user.uid]) {
            this.groupList[ws.user.uid].event(event);
        }
    }

    this.updateSmurfs = function(ws, update) {
        if (this.groupList[ws.user.uid]) {
            this.groupList[ws.user.uid].updateSmurfs(update);
        }
    }
}

function Group(user) {
    this.user = user;
    this.controller = [];
    this.remote = [];

    this.getMembers = function() {
        var members = [];
        this.remote.forEach(function(rt) {
            members.push(rt);
        });
        this.controller.forEach(function(ct) {
            members.push(ct);
        });
        return members;
    }

    this.getRemoteCount = function() {
        return this.remote.length;
    }

    this.getControllerCount = function() {
        return this.controller.length;
    }

    this.removeRemote = function(ws) {
        var found = false;
        this.remote.forEach(function(rmt) {
            if (rmt.socket == ws) {
                found = rmt;
            }
        });
        if (found) {
            var index = this.remote.indexOf(found);
            this.remote.splice(index, 1);
            //console.log("User " + this.user.uid + " left as Remote");
            this.updateStatus();
        }
    }

    this.removeController = function(ws) {
        if (this.controller[0].socket == ws) {
            //console.log("User " + this.user.uid + " left as Controller");
            this.controller = [];
            this.updateStatus();
        }
    }

    this.addRemote = function(ws) {
        var found = false;
        this.remote.forEach(function(rmt) {
            if (rmt.socket == ws) {
                found = true;
            }
        });
        if (!found) {
            //console.log("User " + this.user.uid + " logged in as Remote");
            this.remote.push({
                ip: ws.upgradeReq.connection.remoteAddress,
                socket: ws
            })
            this.updateStatus();
        }
    }

    this.addController = function(ws) {
        //console.log("User " + this.user.uid + " logged in as Controller");
        this.controller[0] = {
            ip: ws.upgradeReq.connection.remoteAddress,
            socket: ws
        };
        this.updateStatus();
    }

    this.updateStatus = function() {
        var status = {
            remoteLength: this.remote.length,
            controller: false,
            type: "status"
        }
        if (this.controller.length > 0) {
            status.controller = {
                ip: this.controller[0].ip
            }
        }
        this.getMembers().forEach(function(member) {
            member.socket.send(JSON.stringify(status));
        });
    }

    this.cmdOutput = function(text) {
        //console.log("User " + this.user.uid + " console output recieved.");
        this.remote.forEach(function(rmt) {
            if (rmt) {
                rmt.socket.send(JSON.stringify({
                    type: "cmdLog",
                    text: text
                }));
            }
        });
    }

    this.cmdWrite = function(text) {
        //console.log("User " + this.user.uid + " console data sent.");
        if (this.controller[0]) {
            this.controller[0].socket.send(JSON.stringify({
                type: "cmdWrite",
                text: text
            }));
        }
    }

    this.updateSettings = function(settings) {
        if (this.controller[0]) {
            this.controller[0].socket.send(JSON.stringify({
                type: "updateSettings",
                settings: settings
            }));
        }
    }

    this.pushLog = function(log) {
        this.remote.forEach(function(rmt) {
            if (rmt) {
                rmt.socket.send(JSON.stringify({
                    type: "log",
                    log: log
                }));
            }
        });
    }

    this.event = function(event) {
        if (this.controller[0]) {
            this.controller[0].socket.send(JSON.stringify(event));
        }
    }

    this.updateSmurfs = function(update) {
        this.remote.forEach(function(rmt) {
            if (rmt) {
                rmt.socket.send(JSON.stringify({
                    type: "liveData",
                    update: update
                }));
            }
        });
    }
}

var cliTable = new Table({
    head: ['#', 'User Id', 'Controller', 'Remote', 'Account Type', 'Trial'],
    colWidths: [3, 35, 15, 15, 20, 20]
});

var updateTable = function() {
    console.log('\033[2J');
    var x = 1;
    cliTable.splice(0, cliTable.length)
    for (var gr in socketMap.groupList) {
        if (gr) {
            cliTable.push([x, socketMap.groupList[gr].user.id, socketMap.groupList[gr].getControllerCount(), socketMap.groupList[gr].getRemoteCount(), socketMap.groupList[gr].user.type, socketMap.groupList[gr].user.trial]);
            x++;
        }
    }
    console.log(cliTable.toString());
}

function handleMessage(data, ws) {
    switch (data.type) {
        case 'login':
            if (data.token) {
                verifyTokenDetectUserSocket(data.token, function(user) {
                    if (user) {
                        if (data.hwid) {
                            ws.user = user;
                            ws.rType = "controller";
                            socketMap.addSocket(ws, "controller", user)
                        } else {
                            ws.user = user;
                            ws.rType = "remote";
                            socketMap.addSocket(ws, "remote", user)
                        }
                    }
                });
            }
            break;

        case 'cmdLog':
            if (ws.user && ws.user.uid) {
                socketMap.cmdLog(ws, data.text);
            }
            break;

        case 'cmdWrite':
            if (ws.user && ws.user.uid) {
                socketMap.cmdWrite(ws, data.text);
            }
            break;

        case 'updateSettings':
            if (ws.user && ws.user.uid) {
                socketMap.updateSettings(ws, data.settings);
            }
            break;

        case 'log':
            if (ws.user && ws.user.uid) {
                var log = {
                    text: data.text,
                    date: data.date,
                    code: data.code,
                    smurf: data.smurf
                }
                ws.user.logs.push(log);
                ws.user.save();
                socketMap.pushLog(ws, log);
            }
            break;

        case 'smurf':
            if (ws.user && ws.user.uid) {
                socketMap.event(ws, data);
            }
            break;

        case 'group':
            if (ws.user && ws.user.uid) {
                socketMap.event(ws, data);
            }
            break;

        case 'smurfupdate':
            if (ws.user && ws.user.uid) {
                socketMap.updateSmurfs(ws, data);
            }
            break;
    }
}

var socketMap = new SocketMap();


var WebSocketServer = require('ws').Server,
    wss = new WebSocketServer({
        port: 4447
    });

wss.on('connection', function(ws) {
    ws.on('error', function(err) {
        return false;
    });

    ws.on('message', function(message) {
        var res = tryParseJSON(message);
        if (res != false) {
            handleMessage(res, ws);
        }
        updateTable();
    });

    ws.on('close', function() {
        if (ws.user) {
            socketMap.removeSocket(ws, ws.rType, ws.user);
        }
        updateTable();
    });
});

function tryParseJSON(jsonString) {
    try {
        var o = JSON.parse(jsonString);
        if (o && typeof o === "object" && o !== null && o.type) {
            return o;
        }
    } catch (e) {}
    return false;
};

function tryParseValidJSON(jsonString) {
    try {
        var o = JSON.parse(jsonString);

        // Handle non-exception-throwing cases:
        // Neither JSON.parse(false) or JSON.parse(1234) throw errors, hence the type-checking,
        // but... JSON.parse(null) returns 'null', and typeof null === "object", 
        // so we must check for that, too.
        if (o && typeof o === "object" && o !== null) {
            return o;
        }
    } catch (e) {}

    return false;
};

net.createServer(function(socket) {    
    socket.on('data', function(data) {

    });

    socket.on('error', function(data) {
    	console.log(data);
    });

    socket.on('end', function() {

    });
}).listen(4450);


module.exports = router;