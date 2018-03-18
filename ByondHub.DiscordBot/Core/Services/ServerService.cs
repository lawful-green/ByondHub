﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ByondHub.DiscordBot.Core.Globals;
using ByondHub.Shared.Logs;
using ByondHub.Shared.Updates;
using ByondHub.Shared.Web;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ByondHub.DiscordBot.Core.Services
{
    public class ServerService
    {
        private readonly HttpClient _http;
        private readonly string _secret;

        public ServerService(IConfiguration config)
        {
            _secret = config["Bot:Backend:SecretCode"];
            var uriBuilder = new UriBuilder("http", config["Bot:Backend:Host"], int.Parse(config["Bot:Backend:Port"]));
            _http = new HttpClient
            {
                BaseAddress = uriBuilder.Uri,
                DefaultRequestHeaders = {Accept = {MediaTypeWithQualityHeaderValue.Parse("application/json")}},
                Timeout = TimeSpan.FromMinutes(20)
            };
        }

        public async Task<ServerStartStopResult> SendStartRequestAsync(string serverId, int port)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("port", port.ToString()),
                new KeyValuePair<string, string>("secret", _secret)
            });
            var responseMessage = await _http.PostAsync($"{ApiEndpoints.ServerStart}/{serverId}", content);
            string resultJson = await responseMessage.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(resultJson))
            {
                throw new Exception($"Got {responseMessage.StatusCode} code.");
            }

            var result = JsonConvert.DeserializeObject<ServerStartStopResult>(resultJson);
            return result;
        }


        public async Task<ServerStartStopResult> SendStopRequestAsync(string serverId)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _secret)
            });
            var responseMessage = await _http.PostAsync($"{ApiEndpoints.ServerStop}/{serverId}", content);
            string resultJson = await responseMessage.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(resultJson))
            {
                throw new Exception($"Got {responseMessage.StatusCode} code.");
            }

            var result = JsonConvert.DeserializeObject<ServerStartStopResult>(resultJson);
            return result;
        }

        public async Task<UpdateResult> SendUpdateRequestAsync(string serverId, string branch, string commitHash)
        {
            var request = new UpdateRequest() {Branch = branch, CommitHash = commitHash, SecretKey = _secret, Id = serverId};
            HttpContent content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync($"{ApiEndpoints.ServerUpdate}/{serverId}", content);
            string resultJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Got {response.StatusCode} with following JSON {resultJson}");
            }

            var result = JsonConvert.DeserializeObject<UpdateResult>(resultJson);
            return result;
        }

        public async Task<WorldLogResult> SendWorldLogRequestAsync(string serverId)
        {
            var response = await _http.GetAsync($"{ApiEndpoints.WorldLog}/{serverId}?secret={_secret}");
            string contentType = response.Content.Headers.ContentType.MediaType;
            if (contentType != "application/json")
            {
                return new WorldLogResult() {LogFileStream = await response.Content.ReadAsStreamAsync()};
            }
            string resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<WorldLogResult>(resultJson);
            return result;
        }
    }
}
