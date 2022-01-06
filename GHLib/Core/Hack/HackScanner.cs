using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MemoryLib.Core;

namespace GHLib.Core.Hack;

public class HackScanner
{
    public HackScanner(Process process)
    {
        Process = process;
        Memory = new HackMemory(new Memory
        {
            ReadProcess = process
        });
        SigScans = new Dictionary<ProcessModule, SigScan>();
        foreach (ProcessModule module in process.Modules)
            SigScans.Add(module, new SigScan
            {
                Process = process,
                Address = module.BaseAddress,
                Size = module.ModuleMemorySize
            });
    }

    public HackScanner(Process process, IntPtr address, int size)
    {
        Process = process;
        Memory = new HackMemory(new Memory
        {
            ReadProcess = process
        });
        SigScans = new Dictionary<ProcessModule, SigScan>
        {
            {
                null,
                new SigScan
                {
                    Process = process,
                    Address = address,
                    Size = size
                }
            }
        };
    }

    public HackScanner(Stream stream, long imageBase, int codeOff)
    {
        Process = null;
        Memory = new HackMemory(stream, imageBase, codeOff);
        SigScans = new Dictionary<ProcessModule, SigScan>
        {
            {Process.GetCurrentProcess().Modules[0], new SigScan(Memory.Stream)}
        };
    }

    public Process Process { get; set; }

    public HackMemory Memory { get; }

    private Dictionary<ProcessModule, SigScan> SigScans { get; }

    public SigScan GetSigScan(string moduleName)
    {
        return SigScans.Where(ss => ss.Key.ModuleName == moduleName).Select(ss => ss.Value).FirstOrDefault();
    }

    public SigScan GetSigScan(ProcessModule module = null)
    {
        return module == null ? SigScans.FirstOrDefault().Value : SigScans[module];
    }

    public SigScan[] GetSigScans(string moduleName = null)
    {
        return string.IsNullOrWhiteSpace(moduleName)
            ? SigScans.Values.ToArray()
            : SigScans.Where(ss => ss.Key.ModuleName == moduleName).Select(ss => ss.Value).ToArray();
    }
}