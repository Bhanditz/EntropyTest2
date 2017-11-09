/*
 * https://stackoverflow.com/questions/22158278/wait-some-seconds-without-blocking-ui-execution found a good wait that doesnt sleeplock thread
 * 
 */
using System;
using System.Collections.Generic;
using GenericTools;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EntropyTest
{
    class core
    {
       
        public static void Main()
        {
            string filepath = @".\results.txt";
            filepath = Path.GetFullPath(filepath);
           
            
            using (StreamWriter output = File.CreateText(filepath))
            {
                output.WriteLine("test results");
            }
           
            long max = 0;
            long min = 1000000000;
            List<long> numlist = new List<long>();
            List<Thread> threadlist = new List<Thread>();
           // Toolkit toolkit = new Toolkit(true, false,true);
            long avg =0;
            for(int counter = 0; counter < 10000;counter++) // will get a total of 40,000 seed values
            {
                Thread threadholder = new Thread(() => { Toolkit threadtool = new Toolkit(1); numlist.Add(threadtool.getInc()); numlist.Add(threadtool.getMod()); numlist.Add(threadtool.getMult()); numlist.Add(threadtool.getSeed()); });
                threadlist.Add(threadholder);
                threadholder.Start();
                Console.WriteLine("Launched " + counter + "-th thread");
                Console.WriteLine(Process.GetCurrentProcess().Threads.Count + " currently running");

                if(Process.GetCurrentProcess().Threads.Count > 750)
                {
                    Console.WriteLine("Waiting on threads to finish up");
                }
                while (Process.GetCurrentProcess().Threads.Count > 750)
                {
                    Task.Delay(1);
                    Thread.Sleep(1);
                }
            }
            
            for (int counter = 0; counter < numlist.Count; counter++)
            {

                Console.WriteLine("Waiting on seed: " + counter);
                if (counter < threadlist.Count)
                {
                    threadlist[counter].Join();
                }
                Console.WriteLine("Processing seed: " + counter);
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
                output.WriteLine("Seed avg is: " + avg);
                output.WriteLine("Seed median is: " + (min + max) / 2);
                output.WriteLine("Seed min is: " + min);
                output.WriteLine("Seed max is: " + max);
            }
            long stdev = 0;

            
            long kurtupper = 0;
            long kurtlower = 0;
            
            for (int counter = 0; counter < numlist.Count; counter++)
            {
                
                stdev += ((numlist[counter] - avg) * (numlist[counter] - avg))/numlist.Count;
                kurtupper += ((numlist[counter] - avg) * (numlist[counter] - avg) * (numlist[counter] - avg) * (numlist[counter] - avg))/numlist.Count;
                kurtlower += (((numlist[counter] - avg) * (numlist[counter] - avg))/numlist.Count) * (((numlist[counter] - avg) * (numlist[counter] - avg)) / numlist.Count);
            }
            

            stdev = Convert.ToInt64(Math.Sqrt(stdev));
            using (StreamWriter output = File.AppendText(filepath))
            {
                output.WriteLine("Seed StdDev is: " + stdev);
            }

            long skew = 3* (((min + max) / 2) - avg)/stdev;
            using (StreamWriter output = File.AppendText(filepath))
            {
                output.WriteLine("Seed Skewv is: " + skew);
                output.WriteLine("Kurtosis is: " + (kurtupper / kurtlower));
            }

            

            Console.WriteLine("done");
            Console.ReadLine();




        }

        


    }
     
}



namespace GenericTools
{
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
        
        public Toolkit(int type =0, bool Debug = false) // instance of generator is started with either in built crypto PRG or homebrewed entropy source, debug turns on console output
        {
            
            debug = Debug;
            Type = type; // 0= internal crypto library, 1 = linear distribution, 2 = exponential distribution, 3  = entropy generated list
            if (type >0)
            {
                waitinglist = new List<long>();
                
                Thread seedthread = new Thread(() => { seed = SeedGen(); });
                seedthread.Start();
                Thread multhread = new Thread(() => { multiplier = SeedGen(); });
                multhread.Start();
                Thread modthread = new Thread(() => { modulo = SeedGen(); });
                modthread.Start();
                Thread incthread = new Thread(() => { increment = SeedGen(); });
                incthread.Start();
                seedthread.Join();
                multhread.Join();
                modthread.Join();
                incthread.Join();
                
            }
            if (type ==2)
            {
                TimeList = new List<long>();
                TimeList.Add(1);
                LastTime = DateTime.Now;
            }
            if(type ==3)
            {
                Seedlist = new List<long>();
                Seedlist.Add(seed);
                Seedlist.Add(increment);
                Seedlist.Add(multiplier);
                Seedlist.Add(modulo);
                Thread Genthread = new Thread(() => { ListGenerator(); });
                Genthread.Start();
            }

        
        }

        void ListGenerator()
        {
            while(true)
            {
                Thread.Sleep(1);
                if(Seedlist.Count < 30)
                {
                    Thread Seed1 = new Thread(() => { Seedlist.Add(SeedGen()); });
                    Thread Seed2 = new Thread(() => { Seedlist.Add(SeedGen()); });
                    Thread Seed3 = new Thread(() => { Seedlist.Add(SeedGen()); });
                    Thread Seed4 = new Thread(() => { Seedlist.Add(SeedGen()); });
                    Seed1.Start();
                    Seed2.Start();
                    Seed3.Start();
                    Seed4.Start();
                    Seed1.Join();
                    Seed2.Join();
                    Seed3.Join();
                    Seed4.Join();
                }
            }
        }
        long TimeAverage()
        {
            DateTime temp = DateTime.Now;
            long TimeDif = LastTime.Millisecond - temp.Millisecond;
            LastTime = temp;
            TimeList.Add(TimeDif);
            long avg = 0;
            for(int count = 0; count < TimeList.Count; count++)
            {
                avg += TimeList[count] / TimeList.Count;
            }
            while(TimeList.Count > 100)
            {
                TimeList.RemoveAt(0);
            }
            return avg;
        }

        public long getSeed()
        {
            return seed;
        }
        public long getMult()
        {
            return multiplier;
        }

        public long getInc()
        {
            return increment;
        }

        public long getMod()
        {
            return modulo;
        }
        
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

        public long ReallyRandom(bool bypass=false)
        {

            long waitingnum = ReallyRandom(true);
            waitinglist.Add(waitingnum);
            while (waitinglist[0] != waitingnum) // access control so multiple threads can't get the same value
            {
                Thread.Sleep(1);
            }
            long randomnum =1;
            if (Type == 0 | bypass ==true)
            {
                RNGCryptoServiceProvider gibberish = new RNGCryptoServiceProvider();
                byte[] buffer = new byte[8];
                gibberish.GetBytes(buffer);
                randomnum = BitConverter.ToInt64(buffer, 0);
            }
            else if(Type == 1)
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
            waitinglist.RemoveAt(0);
            return Convert.ToInt64(Math.Abs(randomnum));
        }

        long LinearDis()
        {

            
            seed = ((seed * multiplier) + increment) % modulo;
            
            return seed;
        }

        long ExpoDis()
        {
            
            seed = Convert.ToInt64(-(1 / TimeAverage()) * Convert.ToInt64(Math.Log((Convert.ToDouble(LinearDis() / modulo))))); //some typecasting bullshittery is needed to  get the Log() function to play nice with long typecasting
            
            return seed;

        }

        long EntroDis()
        {
            while(Seedlist.Count == 0)
            {
                Thread.Sleep(1);
            }
            long temp = Seedlist[0];
            Seedlist.RemoveAt(0);
            return temp;
        }

        public long SeedGen() //generates a random long number
        {
            long initialnum = 1;
            Ping Sender = new Ping();
            List<Thread> Pinglist = new List<Thread>();
            if (debug == true)
            {
                Console.WriteLine("Generating Seed value");
            }
            int count = 0;
            while (initialnum < 4093082899) // multiply until passing a sufficiently large number
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

                if (count%50 == 0)
                {
                    if (debug == true)
                    {
                        Console.WriteLine("Waiting on pings to finish up");
                    }
                    for (int counter = 0; counter < Pinglist.Count; counter++)
                    {
                        Pinglist[counter].Join();
                    }
                }
                count++;
                
            }
            
            initialnum = initialnum % 4093082899; //large prime number close to the limit of an long
            if (debug == true)
            {
                Console.WriteLine("seed is " + initialnum);
            }
            return Convert.ToInt64(initialnum);

        }

        IPAddress ValidIP() // returns a valid non-reserved IP address
        {
            long oct1 = (ReallyRandom(true) % 255);
            long oct2 = (ReallyRandom(true) % 255);
            long oct3 = (ReallyRandom(true) % 255);
            long oct4 = (ReallyRandom(true) % 255);
            string IPstring;
            IPAddress newaddr;

            while (oct1 == 10 | oct1 == 127)
            {
                oct1 = (ReallyRandom(true) % 255);
            }
            while ((oct1 == 172 & (oct2 < 32 & oct2 > 15)) | (oct1 == 192 & oct2 == 168))
            {
                oct1 = (ReallyRandom(true) % 255);
                oct2 = (ReallyRandom(true) % 255);
            }
            IPstring = oct1 + "." + oct2 + "." + oct3 + "." + oct4;
            IPAddress.TryParse(IPstring, out newaddr);

            return newaddr;
        }
    }
}
