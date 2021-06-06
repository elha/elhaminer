using CommandLine;
using System;

namespace elhaminer
{
    class CmdOptions
    {
        [Option(shortName: 'o', longName: "url", Required = true, HelpText = "Stratum URL for mining server in the format \"server:Port\" (e.g. btc.ss.poolin.com:1883)")]
        public string Url
        {
            set
            {
                var arr = value.Split(':');
                PlainServer = arr[0];
                PlainPort = Convert.ToInt16(arr[1]);
            }
        }
        public string PlainServer { get; set; }
        public int PlainPort { get; set; }

        [Option(shortName: 'u', longName: "user", Required = true, HelpText = "Username for mining server")]
        public string Username { get; set; }

        [Option(shortName: 'p', longName: "pass", Required = true, HelpText = "Password for mining server")]
        public string Password { get; set; }

        [Option(shortName: 'r', longName: "reconnect", Required = false, HelpText = "Time in seconds for automatic reconnect", Default = 30)]
        public int Reconnect { get; set; }

        [Option(shortName: 't', longName: "threads", Required = false, HelpText = "Number of Minigthreads")]
        public int? Threads { get; set; }
    }
}
