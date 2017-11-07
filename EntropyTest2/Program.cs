﻿/*
 * https://stackoverflow.com/questions/22158278/wait-some-seconds-without-blocking-ui-execution found a good wait that doesnt sleeplock thread
 * 
 */
using System;
using System.Collections.Generic;
using System.Text;
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
            Toolkit toolkit = new Toolkit(true, false,true);
            long avg =0;
            for(int counter = 0; counter < 40000;counter++) // will get a total of 40,000 seed values
            {
                Thread threadholder = new Thread(() => { numlist.Add(toolkit.LinearDis());});
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
            
            for (int counter = 0; counter < numlist.Count; counter++)
            {
                
                stdev += ((numlist[counter] - avg) * (numlist[counter] - avg))/numlist.Count; 
            }
            
            stdev = Convert.ToInt64(Math.Sqrt(stdev));
            using (StreamWriter output = File.AppendText(filepath))
            {
                output.WriteLine("Seed StdDev is: " + stdev);
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
        bool Entropy;
        bool Type;
        long seed;
        long multiplier;
        long modulo;
        long increment;
        long arrival;
        bool debug;
        List<long> waitinglist;
        public Toolkit(bool entropy = false, bool linearorexpo = false, bool Debug = false) // instance of generator is started with either in built crypto PRG or homebrewed entropy source, debug turns on console output
        {
            Entropy = entropy;
            debug = Debug;
            Type = linearorexpo; // if false than homebrewed algorithm uses linear distribution, else exponential distribution
            if (Entropy == true)
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
                arrival = 1;
            }
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
            long randomnum =1;
            if (Entropy == false | bypass ==true)
            {
                RNGCryptoServiceProvider gibberish = new RNGCryptoServiceProvider();
                byte[] buffer = new byte[8];
                gibberish.GetBytes(buffer);
                randomnum = BitConverter.ToInt64(buffer, 0);
            }
            else if(Type == false)
            {
                randomnum = LinearDis();
            }
            else
            {
                randomnum = ExpoDis();
            }

            return Convert.ToInt64(Math.Abs(randomnum));
        }

        public long LinearDis()
        {

            long waitingnum = ReallyRandom(true);
            waitinglist.Add(waitingnum);
            while(waitinglist[0] != waitingnum) // access control so multiple threads can't get the same value
            {
                Thread.Sleep(1);
            }
            seed = ((seed * multiplier) + increment) % modulo;
            waitinglist.RemoveAt(0);
            return seed;
        }

        public long ExpoDis()
        {
            long waitingnum = ReallyRandom(true);
            waitinglist.Add(waitingnum);
            while (waitinglist[0] != waitingnum) // access control so multiple threads can't get the same value
            {
                Thread.Sleep(1);
            }
            seed = Convert.ToInt64(-(1 / arrival) * Convert.ToInt64(Math.Log((Convert.ToDouble(LinearDis() / modulo)) % 4294967295))); //some typecasting bullshittery is needed to  get the Log() function to play nice with long typecasting
            waitinglist.RemoveAt(0);
            return seed;

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