/*
 * https://stackoverflow.com/questions/22158278/wait-some-seconds-without-blocking-ui-execution found a good wait that doesnt sleeplock thread
 * 
 */
using System;
using System.Collections.Generic;
using GenericTools;

using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace EntropyTest
{
    class core
    {
       
        public static void Main()
        {
            Console.WriteLine("select sample size: ");
            int scale = Convert.ToInt32(Console.ReadLine());
            string filepath = @".\results"+Convert.ToString(scale)+@".txt";
            filepath = Path.GetFullPath(filepath);
           
            
            using (StreamWriter output = File.CreateText(filepath))
            {
                output.WriteLine("test results");
            }
           
            decimal max = 0;
            decimal min = 1000000000;
            
            List<decimal> numlist = new List<decimal>();
            Console.WriteLine("initiating "+scale+" samples");
           List <Thread> threadlist = new List<Thread>();
           // Toolkit toolkit = new Toolkit(true, false,true);
            decimal avg =0;
            Toolkit threadtool = new Toolkit(3);
            Stopwatch bigtimer = new Stopwatch(); //timer for runtime statistics
            bigtimer.Start();
            for (int counter = 0; counter < scale;counter++) // will get a total of 5,000 seed values
            {
                numlist.Add(Convert.ToDecimal(threadtool.ReallyRandom()));
                Console.WriteLine("Gathered " + counter + "-th number "+ numlist[counter]);
               

                
                
            }
            bigtimer.Stop();
            
            TimeSpan time = bigtimer.Elapsed;
            double timeavg = time.TotalMilliseconds/scale;
            for (int counter = 0; counter < numlist.Count-1; counter++)
            {

                
                
                Console.WriteLine("Processing " + counter);
                avg += numlist[counter]/numlist.Count; 
                if(min > numlist[counter])
                {
                    min = numlist[counter];
                }
                if (max < numlist[counter])
                {
                    max = numlist[counter];
                }
                using (StreamWriter output = File.AppendText(filepath))
                {
                    output.WriteLine("Seed value is: " + numlist[counter]);
                }
            }
            
            using (StreamWriter output = File.AppendText(filepath))
            {
                output.WriteLine("Avg time to generate value is: " + timeavg);
                output.WriteLine("Seed avg is: " + avg);
                output.WriteLine("Seed median is: " + (((decimal)min + (decimal)max) / (decimal)2));
                output.WriteLine("Seed min is: " + min);
                output.WriteLine("Seed max is: " + max);
            }
            decimal stdev = 0;


            decimal kurtupper = 0;
            decimal kurtlower = 0;
            
            for (int counter = 0; counter < numlist.Count-1; counter++)
            {
                
                stdev += ((numlist[counter] - avg) * (numlist[counter] - avg))/numlist.Count;
                kurtupper += (((numlist[counter] - avg) / numlist.Count) * ((numlist[counter] - avg) / numlist.Count) * ((numlist[counter] - avg) / numlist.Count) * ((numlist[counter] - avg) / numlist.Count));
                kurtlower += ((((numlist[counter] - avg) / numlist.Count) * ((numlist[counter] - avg) / numlist.Count))) * ((((numlist[counter] - avg) / numlist.Count) * ((numlist[counter] - avg) / numlist.Count)) );
            }


            stdev = Convert.ToDecimal(Math.Sqrt(Convert.ToDouble(stdev)));
            using (StreamWriter output = File.AppendText(filepath))
            {
                output.WriteLine("Seed StdDev is: " + stdev);
            }

            decimal skew = 3* (((min + max) / 2) - avg)/stdev;
            using (StreamWriter output = File.AppendText(filepath))
            {
                output.WriteLine("Seed Skew is: " + skew);
                output.WriteLine("Kurtosis is: " + (kurtupper / kurtlower));
            }


            threadtool.StopGenerating();
            Console.WriteLine("done, press enter to return");
            Console.ReadLine();




        }

        


    }
     
}





namespace GenericTools
{
    /// <summary>
    /// Number generator with PRNG (linear and exponential distributions) or TRNG capabilities
    /// </summary>
    public class Toolkit
    {

        int Type;
        long seed;
        long multiplier;
        long modulo;
        long increment;

        bool debug;
        List<long> waitinglist;
        List<long> TimeList;
        List<long> Seedlist;
        DateTime LastTime;
        bool active;

        /// <summary>
        /// Initializes a new instance of the <see cref="Toolkit"/> class.
        /// </summary>
        /// <param name="type">0 or empty= internal number generator library, 1= linear distribution seeded with TRNG, 2= exponential distribution, 3= Full TRNG, list maintained in background, be sure to invoke stopGenerating before program conclusion</param>
        /// <param name="Debug">if set to <c>true</c> [debug].</param>
        public Toolkit(int type = 0, bool Debug = false) // instance of generator is started with either in built crypto PRG or homebrewed entropy source, debug turns on console output
        {

            debug = Debug;
            Type = type; // 0= internal crypto library, 1 = linear distribution, 2 = exponential distribution, 3  = entropy generated list
            waitinglist = new List<long>();
            active = true;
            if (type > 0)
            {


                

                if(type ==1)
                {
                    Thread seedthread = new Thread(() => { seed = SeedGen(); });
                    seedthread.Start();
                    Thread.Sleep(1);
                    Thread multhread = new Thread(() => { multiplier = SeedGen(); });
                    multhread.Start();
                    Thread.Sleep(1);
                    Thread modthread = new Thread(() => { modulo = SeedGen(); });
                    modthread.Start();
                    Thread.Sleep(1);
                    Thread incthread = new Thread(() => { increment = SeedGen(); });
                    incthread.Start();
                    Thread.Sleep(1);
                    seedthread.Join();
                    multhread.Join();
                    modthread.Join();
                    incthread.Join();
                }

                if (type == 2)
                {
                    Thread seedthread = new Thread(() => { seed = SeedGen(); });
                    seedthread.Start();
                    Thread.Sleep(1);
                    Thread multhread = new Thread(() => { multiplier = SeedGen(); });
                    multhread.Start();
                    Thread.Sleep(1);
                    Thread modthread = new Thread(() => { modulo = SeedGen(); });
                    modthread.Start();
                    Thread.Sleep(1);
                    Thread incthread = new Thread(() => { increment = SeedGen(); });
                    incthread.Start();
                    Thread.Sleep(1);
                    TimeList = new List<long>();
                    TimeList.Add(1);
                    LastTime = DateTime.Now;
                    seedthread.Join();
                    multhread.Join();
                    modthread.Join();
                    incthread.Join();
                }
                if (type == 3)
                {
                    Seedlist = new List<long>();
                    Thread[] threadarray = new Thread[75];
                    for (int count = 0; count < 75; count++)
                    {
                        threadarray[count] = new Thread(() => { Seedlist.Add(SeedGen()); });
                        threadarray[count].Start();
                    }
                    
                    Thread Genthread = new Thread(() => { ListGenerator(); });
                    Genthread.Start();
                }
                
            }


        }

        /// <summary>
        /// Stops the generating.
        /// </summary>
        public void StopGenerating()
        {
            active = false;
        }

        void ListGenerator()
        {
            while (active == true)
            {
                Thread.Sleep(1);
                if (Seedlist.Count < 300)
                {
                    Thread[] threadarray = new Thread[75];
                    for (int count = 0; count < 75; count++)
                    {
                        threadarray[count] = new Thread(() => { Seedlist.Add(SeedGen()); });
                        threadarray[count].Start();
                    }
                    for (int count = 0; count < 75; count++)
                    {
                        threadarray[count].Join();
                    }
                }
            }
        }
        double TimeAverage()
        {
            DateTime temp = DateTime.Now;
            long TimeDif = LastTime.Millisecond - temp.Millisecond;
            LastTime = temp;
            TimeList.Add(TimeDif);
            double avg = 0;
            for (int count = 0; count < TimeList.Count; count++)
            {
                avg += TimeList[count] / TimeList.Count;
            }
            while (TimeList.Count > 100)
            {
                TimeList.RemoveAt(0);
            }
            return avg;
        }


        /// <summary>
        /// Returns TRUE 'freq' percent of the time.
        /// </summary>
        /// <param name="freq">0-100 scale of chance that return value is TRUE</param>
        /// <returns></returns>
        public bool Eventgenerator(long freq) // using built in cryptographic random # generator to produce true returns 'freq' percentage of the time
        {
            long randomnum = ReallyRandom();

            if ((randomnum % 100) <= freq)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns Random number from initialized distribution type
        /// </summary>
        /// <param name="bypass">set to TRUE to force use of default PRNG library</param>
        /// <returns>Double</returns>
        public long ReallyRandom(bool bypass = false)
        {
            if (bypass == false)
            {
                long waitingnum = this.ReallyRandom(true);
                this.waitinglist.Add(waitingnum);
                while (waitinglist[0] != waitingnum) // access control so multiple threads can't get the same value
                {
                    Thread.Sleep(1);
                }
            }
            long randomnum = 1;

            if (Type == 0 | bypass == true)
            {
                RNGCryptoServiceProvider gibberish = new RNGCryptoServiceProvider();
                byte[] buffer = new byte[8];
                gibberish.GetBytes(buffer);
                randomnum = BitConverter.ToInt64(buffer, 0);
            }
            else if (Type == 1)
            {
                randomnum = LinearDis();
            }
            else if (Type == 2)
            {
                randomnum = ExpoDis();
            }
            else if (Type == 3)
            {
                randomnum = EntroDis();
            }

            if (bypass == false)
            {
                waitinglist.RemoveAt(0);
            }
            return Math.Abs(randomnum);
        }

        long LinearDis()
        {


            seed = ((seed * multiplier) + increment) % modulo;

            return seed;
        }

        long ExpoDis()
        {

            long temp = Convert.ToInt64((Math.Abs(-(1 / TimeAverage())  * Math.Log((LinearDis() + 1)/ modulo)))%long.MaxValue); //some typecasting bullshittery is needed to  get the Log() function to play nice with double typecasting

            return temp;

        }

        long EntroDis()
        {
            while (Seedlist.Count == 0)
            {
                Thread.Sleep(1);
            }
            long temp = Seedlist[0];
            Seedlist.RemoveAt(0);
            return temp;
        }

        long SeedGen() //generates a random double number
        {
            double initialnum = 1;
            Ping Sender = new Ping();
            List<Thread> Pinglist = new List<Thread>();
            if (debug == true)
            {
                Console.WriteLine("Generating Seed value");
            }
            int count = 0;
            while (initialnum < 4093082899 & active == true) // multiply until passing a sufficiently large number
            {

                Thread Pinger = new Thread(() =>
                {
                    IPAddress IP1 = ValidIP();
                    if (debug == true)
                    {
                        Console.WriteLine("Selected " + IP1);
                    }
                    try
                    {
                        PingReply reply = Sender.Send(IP1);
                        if (debug == true)
                        {
                            Console.WriteLine("Response time is " + reply.RoundtripTime);
                        }
                        if (reply.RoundtripTime != 0)
                        {
                            initialnum = initialnum * reply.RoundtripTime;

                        }
                        if (debug == true)
                        {
                            Console.WriteLine("Value is " + initialnum);
                        }
                    }
                    catch
                    {
                        if (debug == true)
                        {
                            Console.WriteLine("ping failed, trying another");
                        }
                    }
                });
                Pinglist.Add(Pinger);
                Pinger.Start();
                

                if (count % 150 == 0)
                {
                    if (debug == true)
                    {
                        Console.WriteLine("Waiting on pings to finish up");
                    }
                    for (int counter = 0; counter < Pinglist.Count; counter++)
                    {
                        Pinglist[counter].Join();
                    }
                    Pinglist.Clear();
                }
                count++;

            }
            for (int counter = 0; counter < Pinglist.Count; counter++)
            {
                Pinglist[counter].Join();
            }
            initialnum = initialnum % 2147483629; //large prime number close to the limit of an long
            if (debug == true)
            {
                Console.WriteLine("seed is " + initialnum);
            }
            return Convert.ToInt64(initialnum);

        }

        IPAddress ValidIP() // returns a valid non-reserved IP address
        {
            int oct1 = Convert.ToInt32(ReallyRandom(true) % 254);
            int oct2 = Convert.ToInt32(ReallyRandom(true) % 254);
            int oct3 = Convert.ToInt32(ReallyRandom(true) % 254);
            int oct4 = Convert.ToInt32(ReallyRandom(true) % 254);
            string IPstring;
            IPAddress newaddr;

            while (oct1 == 10 | oct1 == 127)
            {
                oct1 = Convert.ToInt32(ReallyRandom(true) % 254);
            }
            while ((oct1 == 172 & (oct2 < 32 & oct2 > 15)) | (oct1 == 192 & oct2 == 168))
            {
                oct1 = Convert.ToInt32(ReallyRandom(true) % 254);
                oct2 = Convert.ToInt32(ReallyRandom(true) % 254);
            }
            IPstring = oct1 + "." + oct2 + "." + oct3 + "." + oct4;
            IPAddress.TryParse(IPstring, out newaddr);

            return newaddr;
        }
    }
}
