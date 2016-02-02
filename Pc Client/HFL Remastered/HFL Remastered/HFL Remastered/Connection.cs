﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;

namespace HFL_Remastered
{
    public static class Connection
    {
        public static async Task<bool> login(string username, string password, string hwid)
        {
            Properties.Settings.Default.token = "";
            var values = new Dictionary<string, string>();
            values.Add("username", username);
            values.Add("password", password);
            values.Add("hwid", "osman");
            var content = new FormUrlEncodedContent(values);

            using (var client = new HttpClient())
            {
                try
                {
                    var httpResponseMessage = await client.PostAsync("http://localhost:3000/api/remotelogin", content);

                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        String data = await httpResponseMessage.Content.ReadAsStringAsync();
                        dynamic res = JsonConvert.DeserializeObject<object>(data);

                        if (IsPropertyExists(res, "err"))
                        {
                            throw new Exception((string)res.err);
                        }
                        else { 
                            if (IsPropertyExists(res, "userData"))
                            {
                                App.Client = JsonConvert.DeserializeObject<User>(data);
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Server not responding to your request.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Application.Current.Shutdown();
                    return false;
                }
            }
        }


        public static async Task<bool> updateCheck()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://handsfreeleveler.com/");
            client.DefaultRequestHeaders.Accept.Clear();

            // HTTP GET
            try
            {
                HttpResponseMessage response = await client.GetAsync("client_version.txt");
                if (response.IsSuccessStatusCode)
                {
                    String data = await response.Content.ReadAsStringAsync();
                    if (data == Application.Current.FindResource("version").ToString())
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    MessageBox.Show("Cant check for updates terminating.");
                    Application.Current.Shutdown();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Cant check for updates terminating.");
                Application.Current.Shutdown();
            }
            return false;
        }

        public static bool IsPropertyExists(dynamic dynamicObj, string property)
        {
            try
            {
                var value = dynamicObj[property].Value;
                return true;
            }
            catch (RuntimeBinderException)
            {

                return false;
            }

        }
    }
}