﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ByondHub.Core.Configuration;
using ByondHub.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ByondHub.Core.Services.ServerService
{
    public class ServerService
    {
        private readonly Dictionary<string, ServerInstance> _servers;
        private readonly IConfiguration _config;
        private readonly ILogger<ServerService> _logger;
        private readonly BuildModel[] _builds;
        private readonly ServerUpdater _updater;
        private readonly List<string> _updating;

        public ServerService(IConfiguration config, ILogger<ServerService> logger)
        {
            _servers = new Dictionary<string, ServerInstance>();
            _config = config;
            _logger = logger;
            _builds = _config.GetSection("Hub").GetSection("Builds").Get<BuildModel[]>();
            _updater = new ServerUpdater(config, logger);
            _updating = new List<string>();
        }

        public void Start(string serverId, int port)
        {
            if (_servers.ContainsKey(serverId))
            {
                throw new Exception($"Server with id \"{serverId}\" is already started.");
            }
            var build = _builds.FirstOrDefault(x => x.Id == serverId);
            if (build == null)
            {
                throw new Exception($"Server with id \"{serverId}\" is not found.");
            }

            var server = new ServerInstance(build, _config["Hub:DreamDaemonPath"], port);
            _servers.Add(serverId, server);
            server.Start();
            _logger.LogInformation($"Starting server with id {serverId}, port: {port}");
        }

        public void Stop(string serverId)
        {
            if (!_servers.ContainsKey(serverId))
            {
                throw new Exception($"Server with id {serverId} is not started");
            }

            var server = _servers[serverId];
            server.Stop();

            _servers.Remove(serverId);
            _logger.LogInformation($"Killed server with id {serverId}.");
        }

        public string Update(string serverId)
        {
            try
            {
                if (_updating.Contains(serverId))
                {
                    throw new Exception(
                        $"Server with id \"{serverId}\" is already updating. Please wait until update process is finished.");
                }

                var build = _builds.SingleOrDefault(x => x.Id == serverId);
                if (build == null)
                {
                    throw new Exception($"Server with id \"{serverId}\" is not found.");
                }
                if (_servers.ContainsKey(serverId))
                {
                    throw new Exception($"Server with id \"{serverId}\" is started. Please stop it first.");
                }

                _updating.Add(serverId);
                var res = _updater.Update(build);
                string output = res.output;
                string errors = res.errors;
                if (!string.IsNullOrEmpty(errors) && !errors.Equals(Environment.NewLine))
                {
                    throw new Exception($"There was build errors:{Environment.NewLine}{res.errors}");
                }

                return output;

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _updating.RemoveAll(x => x == serverId);
            }

        }
    }
}