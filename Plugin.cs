using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeetleX.EventArgs;
using Bumblebee;
using Bumblebee.Plugins;
using Consul;
using Newtonsoft.Json.Linq;

namespace Qbcode.Consul
{
    public class Plugin : IPluginStatus, IGatewayLoader, IPlugin, IPluginInfo
    {
        public string Name
        {
            get
            {
                return "qbcode.consul";
            }
        }

        public string Description
        {
            get
            {
                return "qbcode.consul";
            }
        }

        public PluginLevel Level
        {
            get
            {
                return PluginLevel.None;
            }
        }

        public bool Enabled { get; set; }

        public string IconUrl
        {
            get
            {
                return "";
            }
        }

        public string EditorUrl
        {
            get
            {
                return "";
            }
        }

        public string InfoUrl
        {
            get
            {
                return "";
            }
        }

        public string ConsulAddress { get; set; }

        public string[] Services { get; set; } = new string[0];

        public string DataCenter { get; set; } = "dc1";

        public string Token { get; set; }

        public void Init(Gateway gateway, Assembly assembly)
        {
            this.mGateway = gateway;
            this.mGateway.HttpServer.ResourceCenter.LoadManifestResource(assembly);
            this.mGetServicesTimer = new Timer(new TimerCallback(this.OnGetServices), null, 5000, 5000);
        }

        private void OnGetServices(object state)
        {
            this.mGetServicesTimer.Change(-1, -1);
            try
            {
                if (this.Enabled && this.mConsulClient != null)
                {
                    Task<QueryResult<Dictionary<string, AgentService>>> task = this.mConsulClient.Agent.Services(default(CancellationToken));
                    task.Wait();
                    foreach (KeyValuePair<string, AgentService> keyValuePair in task.Result.Response)
                    {
                        if (this.Services.Contains(keyValuePair.Value.Service))
                        {
                            
                            string http = string.Format("http://{0}:{1}", keyValuePair.Value.Address, keyValuePair.Value.Port);
                            string ws = string.Format("ws://{0}:{1}", keyValuePair.Value.Address, keyValuePair.Value.Port);
                            
                            if (this.mGateway.Agents.Get(http) == null)
                            {
                                //默认添加http服务
                                if (this.mGateway.HttpServer.EnableLog(LogType.Info))
                                {
                                    this.mGateway.HttpServer.Log(LogType.Info, string.Concat(new string[]
                                    {
                                        "Gateway add ",
                                        http,
                                        " from ",
                                        this.ConsulAddress,
                                        " consul"
                                    }));
                                }
                                this.mGateway.SetServer(http, keyValuePair.Value.Service, null, 200);

                                //获取配置的路由信息
                                IDictionary<string, string> meta = keyValuePair.Value.Meta;
                                string texthttp = null;
                                string textws = null;
                                if (meta != null)
                                {
                                    meta.TryGetValue("path", out texthttp);
                                    meta.TryGetValue("path-ws", out textws);
                                }
                                if (!string.IsNullOrEmpty(texthttp))
                                {
                                    //添加到path定好的路由
                                    foreach (string url in texthttp.Split(';', StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        this.mGateway.SetRoute(url, keyValuePair.Value.Service, null).AddServer(new string[]
                                        {
                                            http
                                        });
                                    }
                                }
                                else
                                {
                                    //添加到默认路由
                                    this.mGateway.Routes.Default.AddServer(new string[]
                                    {
                                        http
                                    });
                                }

                                //如果注册了 ws路由，就加一个 ws服务并且添加路由
                                if (!string.IsNullOrWhiteSpace(textws))
                                {
                                    this.mGateway.SetServer(ws, keyValuePair.Value.Service, null, 200);
                                    foreach (string url in textws.Split(';', StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        this.mGateway.SetRoute(url, keyValuePair.Value.Service, null).AddServer(new string[]
                                        {
                                            ws
                                        });
                                    }
                                }

                                this.mGateway.SaveConfig();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.mGateway.HttpServer.EnableLog(LogType.Error))
                {
                    this.mGateway.HttpServer.Log(LogType.Error, "Gateway consul get servers error " + ex.Message + "@" + ex.StackTrace);
                }
            }
            finally
            {
                this.mGetServicesTimer.Change(5000, 5000);
            }
        }

        public void LoadSetting(JToken setting)
        {
            this.ConsulAddress = setting["ConsulAddress"].Value<string>();
            this.Services = setting["Services"].ToObject<string[]>();
            this.DataCenter = setting["DataCenter"].Value<string>();
            this.Token = setting["Token"].ToObject<string>();
            Uri url = new Uri(this.ConsulAddress);
            this.mConsulClient = new ConsulClient(delegate (ConsulClientConfiguration a)
            {
                a.Address = url;
                a.Datacenter = this.DataCenter;
                a.Token = this.Token;
            });
        }

        public object SaveSetting()
        {
            return new
            {
                this.ConsulAddress,
                this.Services,
                this.Token,
                this.DataCenter
            };
        }

        private Gateway mGateway;

        private Timer mGetServicesTimer;

        private ConsulClient mConsulClient;
    }
}
