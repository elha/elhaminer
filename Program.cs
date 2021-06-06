using CommandLine;
using DotNetStratumMiner;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

// mostly from https://github.com/ma261065/DotNetStratumMiner
/// <summary>
/// (tries to) mine Bitcoin
/// fully working, but you'll never be able to find a share as CPU Mining is way to slow
/// so just for fun, can be usefull for checking stratum servers and explaining all the magic
/// </summary>
namespace elhaminer
{
    class Program
    {
        private static Miner CoinMiner;
        private static int CurrentDifficulty;
        private static Queue<Job> IncomingJobs = new Queue<Job>();
        private static Stratum stratum;
        private static int SharesSubmitted = 0;
        private static int SharesAccepted = 0;
        private static Job CurrentJob;

        static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<CmdOptions>(args)
                    .WithParsed(opts => RunMiner(opts) );
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error! " + ex.ToString());
                Environment.ExitCode = -3; // Unhandled error
            }
        }

        static void RunMiner(CmdOptions opts)
        {
            CoinMiner = new Miner(opts.Threads);
            stratum = new Stratum();

            // Set up event handlers
            stratum.GotResponse += stratum_GotResponse;
            stratum.GotSetDifficulty += stratum_GotSetDifficulty;
            stratum.GotNotify += stratum_GotNotify;

            // Connect to the server
            stratum.ConnectToServer(opts.PlainServer, opts.PlainPort, opts.Username, opts.Password);

            // Workaround for pools that keep disconnecting if no work is submitted in a certain time period. Send regular mining.authorize commands to keep the connection open
            AutoReconnectTask(opts).Start();

            // start miner
            CoinMinerTask().RunSynchronously();
        }

         static Task CoinMinerTask()
        {
            return new Task(() => { 
                while (true)
                {
                    // Wait for a new job to appear in the queue
                    while (IncomingJobs.Count == 0)
                        Thread.Sleep(500);

                    // Get the job
                    CurrentJob = IncomingJobs.Dequeue();

                    if (CurrentJob.CleanJobs)
                        stratum.ExtraNonce2 = 0;

                    // Increment ExtraNonce2
                    stratum.ExtraNonce2++;

                    // Calculate MerkleRoot and Target
                    string MerkleRoot = Utilities.GenerateMerkleRoot(CurrentJob.Coinb1, CurrentJob.Coinb2, stratum.ExtraNonce1, stratum.ExtraNonce2.ToString("x8"), CurrentJob.MerkleNumbers);

                    // Update the inputs on this job
                    CurrentJob.Target = Utilities.GenerateTarget(CurrentDifficulty);
                    CurrentJob.Data = CurrentJob.Version + CurrentJob.PreviousHash + MerkleRoot + CurrentJob.NetworkTime + CurrentJob.NetworkDifficulty;

                    CoinMiner.Mine(CurrentJob);

                    // If the miner returned a result, submit it
                    if (CurrentJob.Answer != 0)
                    {
                        SharesSubmitted++;
                        Console.Write("New Share: {0} ", SharesSubmitted);
                        stratum.SendSUBMIT(CurrentJob.JobID, CurrentJob.Data.Substring(68 * 2, 8), CurrentJob.Answer.ToString("x8"), CurrentDifficulty);
                    }
                }
            });
        }

        static Task AutoReconnectTask(CmdOptions opts)
        {
            return new Task(() => {
                while (true)
                {
                    if ((DateTime.Now - stratum.LastTransmision).TotalSeconds > opts.Reconnect)
                    {
                        Console.Write("Keepalive - ");
                        stratum.SendSUGGESTDIFFICULTY(2);
                    }
                    Task.Delay(1000).Wait();
                };
            });
        }

        static void stratum_GotResponse(object sender, StratumEventArgs e)
        {
            StratumResponse Response = (StratumResponse)e.MiningEventArg;

            Console.Write("Got Response to {0} - ", (string)sender);

            switch ((string)sender)
            {
                case "mining.authorize":
                    if ((bool)Response.result)
                        Console.WriteLine("Worker authorized");
                    else
                    {
                        Console.WriteLine("Worker rejected");
                        Environment.Exit(-1);
                    }
                    break;

                case "mining.subscribe":
                    stratum.ExtraNonce1 = (string)((object[])Response.result)[1];
                    Console.WriteLine("Subscribed. ExtraNonce1 set to " + stratum.ExtraNonce1);
                    break;

                case "mining.submit":
                    if (Response.result != null && (bool)Response.result)
                    {
                        SharesAccepted++;
                        Console.WriteLine("Share accepted ({0} of {1})", SharesAccepted, SharesSubmitted);
                    }
                    else
                        Console.WriteLine("Share rejected. {0}", Response.error[1]);
                    break;
            }
        }

        static void stratum_GotSetDifficulty(object sender, StratumEventArgs e)
        {
            StratumCommand Command = (StratumCommand)e.MiningEventArg;
            CurrentDifficulty = Convert.ToInt32(Command.parameters[0]);
            IncomingJobs.Clear();

            Console.WriteLine("Got Set_Difficulty " + CurrentDifficulty + ". restarting miner");
            CoinMiner.Stop(); // restart mining threads
        }

        static void stratum_GotNotify(object sender, StratumEventArgs e)
        {
            Job ThisJob = new Job();
            StratumCommand Command = (StratumCommand)e.MiningEventArg;

            ThisJob.JobID = (string)Command.parameters[0];
            ThisJob.PreviousHash = (string)Command.parameters[1];
            ThisJob.Coinb1 = (string)Command.parameters[2];
            ThisJob.Coinb2 = (string)Command.parameters[3];
            Array a = (Array)Command.parameters[4];
            ThisJob.Version = (string)Command.parameters[5];
            ThisJob.NetworkDifficulty = (string)Command.parameters[6];
            ThisJob.NetworkTime = (string)Command.parameters[7];
            ThisJob.CleanJobs = (bool)Command.parameters[8];

            ThisJob.MerkleNumbers = new string[a.Length];

            int i = 0;
            foreach (string s in a)
                ThisJob.MerkleNumbers[i++] = s;

            // Cancel the existing mining threads and clear the queue if CleanJobs = true
            if (ThisJob.CleanJobs)
            {
                Console.WriteLine("Stratum detected a new block. restarting miner");

                IncomingJobs.Clear();
                CoinMiner.Stop();
            }

            IncomingJobs.Enqueue(ThisJob);
        }
    }
}


