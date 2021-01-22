using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GHLib.Common.Enums;
using GHLib.Models;
using GHLib.Utils.Extensions;
using static GHLib.Globals;
using static GHLib.Utils.MemoryTools;
using static GHLib.Utils.PointerTools;
using static GHLib.Utils.AoBTools;

namespace GHLib.Utils
{
    public static class HackTools
    {
        private static HackInput[] hackInputs;

        private static Hack[] hacks;

        public static Hack[] Hacks
        {
            get => hacks;
            set
            {
                hacks = value;
                hackInputs = GetHackInputs(hacks);
            }
        }

        private static CancellationTokenSource updateHackTaskTokenSource = new CancellationTokenSource();

        private static CancellationTokenSource initializeHackTaskTokenSource = new CancellationTokenSource();

        private static bool initializing;

        public static HackSettings HackToolSettings = new HackSettings();

        public static string MemoryValueToString(HackInput hackInput)
        {
            if (hackInput.ByteSize == 0) hackInput.ByteSize = GetByteSize(hackInput);

            return MemoryTools.MemoryValueToString(hackInput.Address, hackInput.ByteSize, hackInput.MemType,
                hackInput.MemValMod, hackInput.MemTypeModifiers);
        }

        public static byte[] MemoryStringToBytes(HackInput hackInput)
        {
            if (hackInput.ByteSize == 0) hackInput.ByteSize = GetByteSize(hackInput);

            return MemoryTools.MemoryStringToBytes(hackInput.Value, hackInput.Address, hackInput.ByteSize,
                hackInput.MemType, hackInput.MemValMod, hackInput.MemTypeModifiers);
        }

        public static IntPtr OffsetHack(IntPtr address, HackOffset hackOffset)
        {
            if (hackOffset.IsPointer)
                return GetPointer(address, hackOffset.Offset);
            return address + hackOffset.Offset;
        }

        public static IntPtr OffsetHack(IntPtr address, HackOffset[] hackOffsets)
        {
            var addr = address;
            for (var i = 0; i < hackOffsets.Length; i++) addr = OffsetHack(addr, hackOffsets[i]);
            return addr;
        }

        public static HackInput[] GetHackInputs(Hack[] hacks)
        {
            var hackInputsList = new List<HackInput>();
            foreach (var h in hacks)
            {
                var hackInput = h as HackInput;
                if (hackInput != null)
                    hackInputsList.Add(hackInput);
                if (h.ChildHacks != null && h.ChildHacks.Length != 0)
                    hackInputsList.AddRange(GetHackInputs(h.ChildHacks));
            }

            return hackInputsList.ToArray();
        }

        public static Hack[] GetAllHacks(Hack[] hacks)
        {
            var hackList = new List<Hack>();
            foreach (var h in hacks)
            {
                hackList.Add(h);
                if (h.ChildHacks != null && h.ChildHacks.Length != 0)
                    hackList.AddRange(GetAllHacks(h.ChildHacks));
            }

            return hackList.ToArray();
        }

        public static void ReadjustHackParents(Hack hack)
        {
            if (hack.ChildHacks == null)
                return;

            var cha = hack.ChildHacks;
            hack.ChildHacks = new Hack[cha.Length];
            for (var i = 0; i < cha.Length; i++)
            {
                var h = cha[i];
                if (h is HackInput)
                {
                    var hi = ((HackInput)h).Clone();
                    hack.ChildHacks[i] = hi;
                }
                else
                {
                    hack.ChildHacks[i] = h.Clone();
                }
            }

            foreach (var ch in hack.ChildHacks)
            {
                ReadjustHackParents(ch);
                ch.Parent = hack;
            }
        }

        public static int GetByteSize(HackInput hackInput)
        {
            return MemoryTools.GetByteSize((MemValueType) hackInput.MemType, hackInput.MemTypeModifiers);
        }

        public static bool ValidateValue(HackInput hackInput)
        {
            if (hackInput.Address == IntPtr.Zero)
            {
                DisableHack(hackInput);
                return false;
            }

            var bytes = ReadMemoryBytes(hackInput.Address, hackInput.ByteSize);

            return bytes != null;
        }

        public static void UpdateValue(HackInput hackInput, bool changed = false)
        {
            var value = MemoryValueToString(hackInput);
            if (hackInput.Value != value &&
                value != null &&
                !updateHackTaskTokenSource.IsCancellationRequested)
            {
                var hex = hackInput.MemValMod == MemValueModifier.Hexadecimal;
                if (changed && hackInput.Value != null &&
                    (hackInput.Value.IsDecimalFormat() && !hex ||
                     hackInput.Value.IsHexFormat() && hex ||
                     hackInput.MemType == MemValueType.String))
                {
                    WriteMemoryBytes(hackInput.Address, MemoryStringToBytes(hackInput));
                    return;
                }

                hackInput.Value = value;
            }
        }

        public static void UpdatePointer(HackInput hackInput)
        {
            if (hackInput.Parent == null && hackInput.Offsets != null && hackInput.Offsets.Length != 0)
                return;

            hackInput.Address = OffsetHack(hackInput.Parent.Address, hackInput.Offsets);

            if (hackInput.Address == IntPtr.Zero)
                DisableHack(hackInput);
        }

        public static void InitializeHack(Hack hack, bool reverseScan = false)
        {
            var hackInput = hack as HackInput;

            if (hack.Parent != null)
                if (hack.RelativeAddress && hack.Parent.Enabled)
                    hack.Address = hack.Parent.Address;

            if (hack.AoBScripts != null && hack.AoBScripts.Length != 0)
            {
                if (!hack.Enabled)
                    Array.ForEach(hack.AoBScripts, aobs =>
                    {
                        hack.Enabled = CheckAoBScript(aobs, hack.Address);
                        if (!hack.Enabled && reverseScan)
                        {
                            hack.Enabled = CheckAoBScript(aobs, hack.Address, reverseScan);
                            if (!hack.Enabled)
                                return;
                            hack.Activated = hack.Enabled;
                        }

                        if (hackInput != null)
                            hackInput.Address = GetLastAoBScript(aobs).Address;
                    });
                else
                    hack.Address = hack.AoBScripts[0].Address;
            }

            if (hack.Offsets != null && hack.Offsets.Length != 0) hack.Address = OffsetHack(hack.Address, hack.Offsets);

            if (hackInput != null)
            {
                if (hackInput.Address == IntPtr.Zero)
                {
                    hackInput.Initialized = true;
                    if (hackInput.ChildHacks != null && hackInput.ChildHacks.Length != 0)
                        foreach (var h in GetAllHacks(hackInput.ChildHacks))
                            h.Initialized = true;
                    hackInput.Enabled = false;
                    return;
                }

                if (hackInput.MemType != null)
                {
                    var maxshift = Is64Bit ? 63 : 31;
                    if (hackInput.MemTypeModifiers != null && hackInput.MemTypeModifiers.Length > 1)
                        hackInput.MemTypeModifiers[1] = hackInput.MemTypeModifiers[1] > maxshift
                            ? maxshift
                            : hackInput.MemTypeModifiers[1];

                    hackInput.Enabled = hackInput.IsValid();
                    if (hackInput.Enabled) hackInput.Update();
                }
                else
                {
                    hackInput.Enabled = true;
                }
            }

            hack.Initialized = true;

            if (hack.ChildHacks != null && hack.ChildHacks.Length != 0)
                foreach (var h in hack.ChildHacks)
                    InitializeHack(h, reverseScan);
        }

        public static void ToggleHack(Hack hack, bool disableChildren = false)
        {
            var isHackInput = hack is HackInput;
            if (!isHackInput && (!hack.Enabled || !hack.Activated && disableChildren))
                return;

            if (disableChildren)
                hack.Activated = false;

            if (isHackInput)
            {
                if (((HackInput) hack).MemType != null && !disableChildren)
                    hack.Activated = !hack.Activated;
            }
            else
            {
                if (hack.AoBScripts != null && hack.AoBScripts.Length != 0)
                    Array.ForEach(hack.AoBScripts, aobs => { ToggleAoB(aobs, hack.Activated); });
            }

            if (hack.ChildHacks != null && hack.ChildHacks.Length != 0)
                foreach (var h in hack.ChildHacks)
                    if (h.Activated || isHackInput)
                        ToggleHack(h, true);
        }

        private static void ToggleAoB(AoBScript aobs, bool activated)
        {
            if (aobs.AoBReplacements != null && aobs.AoBReplacements.Length != 0)
                Array.ForEach(aobs.AoBReplacements, aobr =>
                {
                    var offset = aobr.Offset;
                    var newLength = aobr.ReplaceAoB.Length;
                    var oldbytes = new byte[newLength];
                    Array.Copy(aobs.AoB, offset, oldbytes, 0, newLength);
                    var bytes = activated ? aobr.ReplaceAoB : oldbytes;
                    WriteMemoryBytes(aobs.Address, bytes, aobr.Offset);
                });

            if (aobs.AoBScripts != null && aobs.AoBScripts.Length != 0)
                Array.ForEach(aobs.AoBScripts, saobs => { ToggleAoB(saobs, activated); });
        }

        internal static void AutoUpdateHacksTask()
        {
            Reset();

            if (HackToolSettings.AutoUpdateHacks)
            {
                while (initializing) Task.Delay(200).Wait();
                initializing = false;
                updateHackTaskTokenSource = new CancellationTokenSource();
                initializeHackTaskTokenSource = new CancellationTokenSource();
                var initializeHackTaskToken = initializeHackTaskTokenSource.Token;
                Task.Run(() =>
                {
                    initializing = true;
                    if (!initializeHackTaskToken.IsCancellationRequested)
                        UpdateValuesTask(updateHackTaskTokenSource.Token);

                    foreach (var hack in Hacks) InitializeHack(hack, HackToolSettings.ReverseScan);
                }, initializeHackTaskToken).ContinueWith(t => { return initializing = false; });
            }
        }

        private static void UpdateValuesTask(CancellationToken updateHackTaskToken)
        {
            Task.Run(async () =>
            {
                var hackInputsClone = new HackInput[hackInputs.Length];
                Array.Copy(hackInputs, hackInputsClone, hackInputs.Length);
                while (!updateHackTaskToken.IsCancellationRequested)
                {
                    await Task.Delay(HackToolSettings.ValueUpdateInterval);
                    foreach (var hackInput in hackInputsClone)
                        if (!updateHackTaskToken.IsCancellationRequested)
                        {
                            if (hackInput.Offsets != null && hackInput.Offsets.Length != 0)
                                if (hackInput.Offsets.Any(o => o.IsPointer))
                                    UpdatePointer(hackInput);

                            if (hackInput.MemType != null)
                            {
                                hackInput.Enabled = ValidateValue(hackInput);
                                if (hackInput.Enabled)
                                    UpdateValue(hackInput, hackInput.Activated);
                            }

                            if (!hackInput.Enabled)
                            {
                                if (hackInput.Parent != null)
                                {
                                    var aobAllow = true;
                                    if (hackInput.AoBScripts != null && hackInput.AoBScripts.Length != 0)
                                    {
                                        aobAllow = hackInput.AoBScripts.Length == 1;
                                        var aobp = GetLastAoBScript(hackInput.AoBScripts[0]).AoBPointer;
                                        if (aobp != null && aobAllow)
                                            aobAllow = aobp.PointerType == AoBPointerType.Offset;
                                    }

                                    if (hackInput.Parent.Enabled && aobAllow)
                                        hackInput.Initialize();
                                }
                                else if (hackInput.AoBScripts == null && hackInput.AoBScripts.Length != 0)
                                {
                                    hackInput.Initialize();
                                }
                            }
                        }
                }
            }, updateHackTaskToken);
        }

        internal static void Process_Exited(object sender, EventArgs e)
        {
            Reset();
        }

        private static void CancelTokens()
        {
            if (!updateHackTaskTokenSource.IsCancellationRequested)
                updateHackTaskTokenSource.Cancel();

            if (!initializeHackTaskTokenSource.IsCancellationRequested)
                initializeHackTaskTokenSource.Cancel();
        }

        public static void DisableHacks(bool uninitialize = false)
        {
            if (Hacks != null && hacks.Length != 0)
                foreach (var h in Hacks)
                    DisableHack(h, uninitialize);
        }

        private static void DisableHack(Hack hack, bool uninitialize = false)
        {
            hack.Address = IntPtr.Zero;
            hack.Enabled = hack.Activated = false;
            if (uninitialize)
                hack.Initialized = false;
            var hackInput = hack as HackInput;
            if (hackInput != null)
                if (!string.IsNullOrEmpty(hackInput.Value))
                    hackInput.Value = string.Empty;

            if (hack.ChildHacks != null && hack.ChildHacks.Length != 0)
                foreach (var h in hack.ChildHacks)
                    DisableHack(h, uninitialize);
        }

        public static void Reset()
        {
            CancelTokens();
            DisableHacks(true);
        }
    }
}