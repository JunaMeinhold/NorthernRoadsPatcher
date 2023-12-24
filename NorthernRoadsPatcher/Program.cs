using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NorthernRoadsPatcher
{
    public class Program
    {
        public static Worldspace.TranslationMask? WorldspaceMask { get; private set; } = new(true, true) { SubCells = new(false, false) };
        public static WorldspaceBlock.TranslationMask? WorldspaceBlockMask { get; private set; } = new(true, true) { Items = new(false, false) };
        public static WorldspaceSubBlock.TranslationMask? WorldspaceSubBlockMask { get; private set; } = new(true, true) { Items = new(false, false) };

        private static readonly object _lock = new();

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "NorthernRoadsPatcher.esp")
                .Run(args);
        }

        public struct LandscapeItem
        {
            public IModContext<Landscape> ModContext;
            public ICellGetter CellGetter;
            public IWorldspaceSubBlockGetter SubBlockGetter;
            public IWorldspaceBlockGetter BlockGetter;
            public IWorldspaceGetter WorldspaceGetter;

            public LandscapeItem(IModContext<Landscape> modContext, ICellGetter cellGetter, IWorldspaceSubBlockGetter subBlockGetter, IWorldspaceBlockGetter blockGetter, IWorldspaceGetter worldspaceGetter)
            {
                ModContext = modContext;
                CellGetter = cellGetter;
                SubBlockGetter = subBlockGetter;
                BlockGetter = blockGetter;
                WorldspaceGetter = worldspaceGetter;
            }
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            state.LinkCache.Warmup<Landscape>();

            ConcurrentQueue<LandscapeItem> queue = new();

            foreach (var winningRecord in state.LinkCache.AllIdentifiers<Landscape>())
            {
                var landscapes = state.LinkCache.ResolveAllSimpleContexts<Landscape>(winningRecord.FormKey).ToArray();

                IModContext<Landscape>? last = null;
                for (var i = 0; i < landscapes.Length; i++)
                {
                    var land = landscapes[i];
                    if (land.ModKey.Name.Contains("Northern Roads", StringComparison.OrdinalIgnoreCase))
                    {
                        last = land;
                        break;
                    }
                }

                if (last == null)
                {
                    continue;
                }

                ICellGetter? cellGetter = (ICellGetter?)last.Parent?.Record;
                if (cellGetter == null)
                    continue;
                IWorldspaceSubBlockGetter? subBlockGetter = (IWorldspaceSubBlockGetter?)last.Parent?.Parent?.Record;
                if (subBlockGetter == null)
                    continue;
                IWorldspaceBlockGetter? blockGetter = (IWorldspaceBlockGetter?)last.Parent?.Parent?.Parent?.Record;
                if (blockGetter == null)
                    continue;
                IWorldspaceGetter? worldspaceGetter = (IWorldspaceGetter?)last.Parent?.Parent?.Parent?.Parent?.Record;
                if (worldspaceGetter == null)
                    continue;

                LandscapeItem item = new(last, cellGetter, subBlockGetter, blockGetter, worldspaceGetter);
                queue.Enqueue(item);
            }

            while (queue.TryDequeue(out var queueItem))
            {
                MasterReference reference = new()
                {
                    Master = queueItem.ModContext.ModKey
                };

                state.PatchMod.MasterReferences.Add(reference);

                Worldspace? worldspace = state.PatchMod.Worldspaces.FirstOrDefault(x => x.FormKey == queueItem.WorldspaceGetter.FormKey);
                if (worldspace == null)
                {
                    worldspace = queueItem.WorldspaceGetter.DeepCopy(WorldspaceMask);
                    worldspace = state.PatchMod.Worldspaces.GetOrAddAsOverride(worldspace);
                }

                Cell cell = queueItem.CellGetter.DeepCopy();

                WorldspaceBlock? block = worldspace.SubCells.Find(x => x.BlockNumberX == queueItem.BlockGetter.BlockNumberX && x.BlockNumberY == queueItem.BlockGetter.BlockNumberY);
                if (block != null)
                {
                    WorldspaceSubBlock? subBlock = block.Items.Find(x => x.BlockNumberX == queueItem.SubBlockGetter.BlockNumberX && x.BlockNumberY == queueItem.SubBlockGetter.BlockNumberY);
                    if (subBlock == null)
                    {
                        subBlock = queueItem.SubBlockGetter.DeepCopy(WorldspaceSubBlockMask);
                        block.Items.Add(subBlock);
                    }

                    Cell? existingCell = subBlock.Items.Find(x => x.FormKey == queueItem.CellGetter.FormKey);
                    if (existingCell != null)
                    {
                        subBlock.Items.Remove(existingCell);
                    }
                    subBlock.Items.Add(cell);
                }
                else
                {
                    WorldspaceSubBlock subBlock = queueItem.SubBlockGetter.DeepCopy(WorldspaceSubBlockMask);
                    subBlock.Items.Add(cell);

                    block = queueItem.BlockGetter.DeepCopy(WorldspaceBlockMask);
                    block.Items.Add(subBlock);

                    worldspace.SubCells.Add(block);
                }
            }
        }
    }
}