using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetStratumMiner
{
    class Miner
    {
        // General Variables
        public volatile uint FinalNonce = 0;
        public uint threadCount = 1;
        public bool stopped = false;

        public Miner(int? optThreadCount = null)
        {
            threadCount = (uint?)optThreadCount ?? (uint)Environment.ProcessorCount;
        }
       
        public void Mine(Job ThisJob)
        {
            Console.WriteLine("Miner: started - {0} threads", threadCount);

            DateTime StartTime = DateTime.Now;
            double Hashcount = 0;

            // Gets the data to hash and the target from the work
            byte[] data = Utilities.ReverseByteArrayByFours(Utilities.HexStringToByteArray(ThisJob.Data));
            byte[] target = Utilities.HexStringToByteArray(ThisJob.Target);

            stopped = false;
            FinalNonce = 0;
            uint batchSize = 1 << 18;

            Parallel.For(0, threadCount, i => 
            {
                var work = new MiniMiner.Work(data, target, (uint)i, threadCount, batchSize);

                Debug.WriteLine("New thread  {0}", i);

                // Loop until stopped or hash meets target
                
                while (!stopped)
                {
                    if (work.WorkBatch())
                    {
                        FinalNonce = work.CurrentNonce;
                        stopped = true;
                    }

                    Hashcount += batchSize;
                    if (Hashcount > 0xFFFFFF00) stopped = true; // prevent overrun
                }
            });

            ThisJob.Answer = FinalNonce;

            double Elapsedtime = (DateTime.Now - StartTime).TotalSeconds;
            Console.WriteLine("Miner: finished - {0:0} hashes in {1:0.00} s. Speed: {2:0.00} kHash/s, {3}", Hashcount
                , Elapsedtime, Elapsedtime > 0 ? Hashcount / Elapsedtime / 1000 : 0
                , ThisJob.Answer !=0 ? " FOUND ANSWER" :"");
        }

        internal void Stop()
        {
            stopped = true;
        }
    }

}
