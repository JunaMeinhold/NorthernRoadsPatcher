using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using System.Collections.Concurrent;
using System.Reflection;

namespace NorthernRoadsPatcher
{
    public class Program
    {
        public static Cell.TranslationMask CellMask { get; set; } = new(true, true)
        {
            Temporary = false,
            Persistent = false,
            ImageSpace = true
        };

        public static Worldspace.TranslationMask? WorldspaceMask { get; private set; } = new(true, true) { SubCells = new(false, false) };

        public static WorldspaceBlock.TranslationMask? WorldspaceBlockMask { get; private set; } = new(true, true) { Items = new(false, false) };

        public static WorldspaceSubBlock.TranslationMask? WorldspaceSubBlockMask { get; private set; } = new(true, true) { Items = new(false, false) };

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "NorthernRoadsPatcher.esp")
                .Run(args);
        }

        public struct LandscapeItem
        {
            public IModContext<ILandscapeGetter> Winner;
            public IModContext<ILandscapeGetter> ModContext;
            public ICellGetter WinnerCellGetter;
            public ICellGetter CellGetter;
            public IWorldspaceSubBlockGetter SubBlockGetter;
            public IWorldspaceBlockGetter BlockGetter;
            public IWorldspaceGetter WorldspaceGetter;

            public LandscapeItem(IModContext<ILandscapeGetter> winner, IModContext<ILandscapeGetter> modContext, ICellGetter winnerCellGetter, ICellGetter cellGetter, IWorldspaceSubBlockGetter subBlockGetter, IWorldspaceBlockGetter blockGetter, IWorldspaceGetter worldspaceGetter)
            {
                Winner = winner;
                ModContext = modContext;
                WinnerCellGetter = winnerCellGetter;
                CellGetter = cellGetter;
                SubBlockGetter = subBlockGetter;
                BlockGetter = blockGetter;
                WorldspaceGetter = worldspaceGetter;
            }
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            HashSet<string> masters = [];

            void AddMasterRecursive(ModKey key)
            {
                if (!masters.Add(key.FileName)) return;
                MasterReference reference = new()
                {
                    Master = key
                };

                state.PatchMod.MasterReferences.Add(reference);

                foreach (var refer in state.LoadOrder[key].Mod!.MasterReferences)
                {
                    AddMasterRecursive(refer.Master);
                }
            }

            state.LinkCache.Warmup<Landscape>();
            state.LinkCache.Warmup<Worldspace>();

            Queue<LandscapeItem> queue = new();

            foreach (var winningRecord in state.LinkCache.AllIdentifiers<Landscape>())
            {
                var winner = state.LinkCache.ResolveSimpleContext<ILandscapeGetter>(winningRecord.FormKey);

                IModContext<ILandscapeGetter>? last = null;
                foreach (var land in state.LinkCache.ResolveAllSimpleContexts<ILandscapeGetter>(winningRecord.FormKey))
                {
                    AddMasterRecursive(land.ModKey);
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

                ICellGetter? winnerCellGetter = (ICellGetter?)winner.Parent?.Record;
                if (winnerCellGetter == null)
                    continue;
                ICellGetter? cellGetter = (ICellGetter?)last.Parent?.Record;
                if (cellGetter == null)
                    continue;
                IWorldspaceSubBlockGetter? subBlockGetter = (IWorldspaceSubBlockGetter?)winner.Parent?.Parent?.Record;
                if (subBlockGetter == null)
                    continue;
                IWorldspaceBlockGetter? blockGetter = (IWorldspaceBlockGetter?)winner.Parent?.Parent?.Parent?.Record;
                if (blockGetter == null)
                    continue;
                IWorldspaceGetter? worldspaceGetter = (IWorldspaceGetter?)winner.Parent?.Parent?.Parent?.Parent?.Record;
                if (worldspaceGetter == null)
                    continue;

                AddMasterRecursive(last.ModKey);

                worldspaceGetter = state.LinkCache.ResolveSimpleContext<IWorldspaceGetter>(worldspaceGetter.FormKey).Record ?? throw new InvalidOperationException($"IWorldspaceGetter is null {worldspaceGetter.FormKey}");

                LandscapeItem item = new(winner, last, winnerCellGetter, cellGetter, subBlockGetter, blockGetter, worldspaceGetter);
                queue.Enqueue(item);
            }

            while (queue.TryDequeue(out var queueItem))
            {
                Worldspace? worldspace = state.PatchMod.Worldspaces.FirstOrDefault(x => x.FormKey == queueItem.WorldspaceGetter.FormKey);
                if (worldspace == null)
                {
                    worldspace = queueItem.WorldspaceGetter.DeepCopy(WorldspaceMask);
                    worldspace = state.PatchMod.Worldspaces.GetOrAddAsOverride(worldspace);
                }

                Cell cell = queueItem.WinnerCellGetter.DeepCopy(CellMask);

                var sourceLand = (ILandscapeGetter)((IModContext)queueItem.ModContext).Record!;

                cell.ImageSpace.SetTo(queueItem.WinnerCellGetter.ImageSpace);
                cell.Landscape = sourceLand.DeepCopy();
                cell.MaxHeightData = queueItem.CellGetter.MaxHeightData?.DeepCopy();
                cell.WaterHeight = queueItem.CellGetter.WaterHeight;
                if (queueItem.CellGetter.OcclusionData != null)
                {
                    cell.OcclusionData = queueItem.CellGetter.OcclusionData.Value.Span.ToArray();
                }

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
                    block = queueItem.BlockGetter.DeepCopy(WorldspaceBlockMask);

                    WorldspaceSubBlock subBlock = queueItem.SubBlockGetter.DeepCopy(WorldspaceSubBlockMask);
                    subBlock.Items.Add(cell);

                    block.Items.Add(subBlock);

                    worldspace.SubCells.Add(block);
                }
            }
        }
    }
}