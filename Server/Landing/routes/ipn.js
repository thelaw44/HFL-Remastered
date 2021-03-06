var express = require('express');
var router = express.Router();
var mongoose = require('mongoose');
mongoose.connect("mongodb://127.0.0.1:27017/hflRest");
var db = mongoose.connection;
var fs = require("fs");
var js2lua = require("js2lua");
var request = require("request");
var Schema = mongoose.Schema;
db.on('error', console.error.bind(console, 'connection error:'));

db.once('open', function(callback) {
    console.log("connection to db open")
});

var User = require("../models/user.js");

router.post('/gotPaymentHolyFuckYes', function(req,res,next){
    if(req.body.payment_status){
      if (req.body.payment_status == 'Completed') {
        if(req.body.custom.replace(".","")){
          if(req.body.mc_gross == "35.00"){
            User.update({_id: req.body.custom}, {$set:{
              type: 2, 
            }}, function(err, numberAffected, rawResponse) {
              if(!err){
                console.log("INCOME:"+ req.body.mc_gross+" Multi smurf");
              }
            })
          }
          if(req.body.mc_gross == "25.00"){
            User.update({_id: req.body.custom}, {$set:{
              type: 1, 
            }}, function(err, numberAffected, rawResponse) {
              if(!err){
                console.log("INCOME:"+ req.body.mc_gross+" Single smurf");
              }
            })
          }
          if(req.body.mc_gross == "15.00"){
            User.update({_id: req.body.custom}, {$set:{
              trial: Date.now()+2592000000, 
            }}, function(err, numberAffected, rawResponse) {
              if(!err){
                console.log("INCOME:"+ req.body.mc_gross+" Monthly");
              }
            })
          }
          if(req.body.mc_gross == "16.00"){
            User.findOne({_id: req.body.custom}, {type:1}, function(err,user){
              if(!err && user){
                if(user.type === 1){
                  user.type = 2;
                  user.save();
                }
              }
            });
          }
        }
      }
    }
    res.sendStatus(200);
});

router.get("/getScriptAI/:bolid/:champ", function(req,res,next){
    User.findOne({ "bol" : { $regex : new RegExp(req.params.bolid, "i") } }, {ai:1} ,function(err, user){
        if (req.params.champ == "monkeyking"){
          req.params.champ = "wukong";
        }
        var luaCode = "return ";
        if(user){
            var response = user.ai;
            response.items = user.ai.items[req.params.champ];
            response.plugin = user.ai.plugins[req.params.champ];
            delete response.plugins;
            luaCode += js2lua.convert(response);
            res.end("<data>"+luaCode+"</data>");
        }else{
            luaCode += "nil";
            res.end("<data>"+luaCode+"</data>");
        }
    });
});

var script = "";

request('https://raw.githubusercontent.com/thelaw44/HFL_Remastered_/master/HFL.lua', function (error, response, body) {
  if (!error && response.statusCode == 200) {
    script = body;
  }
});

var t = setInterval(function(){
  request('https://raw.githubusercontent.com/thelaw44/HFL_Remastered_/master/HFL.lua', function (error, response, body) {
    if (!error && response.statusCode == 200) {
      script = body;
      console.log("Script just updated");
    }
  });
},1000 * 60 * 10);

router.get("/script", function(req,res,next){
  res.writeHead(200, {'Content-Type': 'application/force-download','Content-disposition':'attachment; filename=HFL.lua'});
  res.end(script);
});

router.get('/version', function(req,res,next){
    res.end("1.0");
})


module.exports = router;