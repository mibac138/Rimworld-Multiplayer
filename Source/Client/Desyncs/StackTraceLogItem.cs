using System;
using System.Collections.Generic;
using System.Text;
using LudeonTK;
using Multiplayer.Client.Desyncs;
using Verse;

namespace Multiplayer.Client
{
    public abstract class StackTraceLogItem : IDisposable
    {
        public int tick;
        public int hash;

        public abstract string AdditionalInfo { get; }

        public abstract string StackTraceString { get; }

        public override string ToString()
        {
            return $"Tick:{tick} Hash:{hash} '{AdditionalInfo}'\n{StackTraceString}";
        }

        public virtual void Dispose()
        {
        }
    }

    public class StackTraceLogItemObj : StackTraceLogItem
    {
        public string info1;
        public string info2;

        public override string AdditionalInfo => $"{info1} {info2}";

        public override string StackTraceString => "";
    }

    public class StackTraceLogItemRaw : StackTraceLogItem
    {
        public const int MaxDepth = 32;
        public long[] raw = new long[MaxDepth];
        public int depth;

        public int ticksGame;
        public ulong rngState;
        public ThingDef thingDef;
        public int thingId;
        public string factionName;
        public string moreInfo;

        public override string AdditionalInfo => $"{ticksGame} {thingDef}{thingId} {factionName} {depth} {rngState} {moreInfo}";

        private static Dictionary<long, string> methodNameCache = new();

        // Enabled to help debug an issue where a desync happens because the hash differs, despite the stack trace being
        // the same.
        [TweakValue("Multiplayer")] private static bool verboseStackTraces = true;
        private static bool lastVerboseStackTraces = verboseStackTraces;

        public override string StackTraceString
        {
            get
            {
                if (verboseStackTraces != lastVerboseStackTraces)
                {
                    lastVerboseStackTraces = verboseStackTraces;
                    methodNameCache.Clear();
                }

                var builder = new StringBuilder();
                for (int i = 0; i < depth; i++)
                {
                    var addr = raw[i];

                    if (!methodNameCache.TryGetValue(addr, out string method))
                    {
                        var resolution = verboseStackTraces ? MethodResolution.UseBoth : MethodResolution.PreferPatch;
                        method = Native.MethodNameFromAddr(raw[i], resolution);
                        if (verboseStackTraces)
                            method += $" [H: {DeferredStackTracingImpl.hashTable.GetAddrInfoCopy(addr)?.addr}]";
                        methodNameCache[addr] = method;
                    }

                    builder.AppendLine(method != null ? SyncCoordinator.MethodNameWithIL(method) : "Null");
                }

                return builder.ToString();
            }
        }

        public static StackTraceLogItemRaw GetFromPool()
        {
            return SimplePool<StackTraceLogItemRaw>.Get();
        }

        public override void Dispose()
        {
            depth = 0;
            ticksGame = 0;
            rngState = 0;
            thingId = 0;
            thingDef = null;
            factionName = null;
            moreInfo = null;

            SimplePool<StackTraceLogItemRaw>.Return(this);
        }
    }
}
