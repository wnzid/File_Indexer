﻿using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Master
{
    internal class Program
    {
        static Dictionary<string, Dictionary<string, int>> mergedData = new();
        static object lockObj = new();

        static void Main(string[] args)
        {
            Console.Title = "Master - Threaded Multi-Agent Receiver";
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)0x1;

            Console.WriteLine("==========================================");
            Console.WriteLine("           Master is starting up          ");
            Console.WriteLine("==========================================\n");

            Thread t1 = new Thread(() => HandleAgent("agent1"));
            Thread t2 = new Thread(() => HandleAgent("agent2"));

            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();

            Console.WriteLine("\nAll agent data received. Combined result:\n");

            foreach (var fileEntry in mergedData)
            {
                foreach (var wordEntry in fileEntry.Value)
                {
                    Console.WriteLine($"{fileEntry.Key}:{wordEntry.Key}:{wordEntry.Value}");
                }
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }

        static void HandleAgent(string pipeName)
        {
            try
            {
                using NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In);
                Console.WriteLine($"Waiting for connection on pipe: {pipeName}");
                pipeServer.WaitForConnection();
                Console.WriteLine($"Connected to {pipeName}.\n");

                using StreamReader reader = new StreamReader(pipeServer, Encoding.UTF8);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine($"{pipeName.ToUpper()} -> {line}");
                    ProcessLine(line);
                }

                Console.WriteLine($"{pipeName.ToUpper()} disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with {pipeName}: {ex.Message}");
            }
        }

        static void ProcessLine(string line)
        {
            string[] parts = line.Split(':');
            if (parts.Length != 3) return;

            string file = parts[0];
            string word = parts[1];
            if (!int.TryParse(parts[2], out int count)) return;

            lock (lockObj)
            {
                if (!mergedData.ContainsKey(file))
                {
                    mergedData[file] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                }

                if (mergedData[file].ContainsKey(word))
                {
                    mergedData[file][word] += count;
                }
                else
                {
                    mergedData[file][word] = count;
                }
            }
        }
    }
}