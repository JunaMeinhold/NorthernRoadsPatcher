using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;

namespace NorthernRoadsPatcher
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "NorthernRoadsPatcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            state.LinkCache.Warmup<Landscape>();
            state.LinkCache.Warmup<Worldspace>();

            foreach (var winningRecord in state.LinkCache.AllIdentifiers<Landscape>())
            {
                var winner = state.LinkCache.ResolveSimpleContext<ILandscapeGetter>(winningRecord.FormKey);

                ICellGetter? winnerCellGetter = (ICellGetter?)winner.Parent?.Record;
                if (winnerCellGetter == null)
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

                worldspaceGetter = state.LinkCache.ResolveSimpleContext<IWorldspaceGetter>(worldspaceGetter.FormKey).Record ?? throw new InvalidOperationException($"IWorldspaceGetter is null {worldspaceGetter.FormKey}");

                if (worldspaceGetter.Name!.String == "Blackreach")
                {
                    if (blockGetter.BlockNumberX == 0 && blockGetter.BlockNumberX == 0)
                    {
                        if (subBlockGetter.BlockNumberX == 0 && subBlockGetter.BlockNumberX == 0)
                        {
                            Console.WriteLine(winnerCellGetter.ImageSpace); // always null
                            Console.WriteLine(winner.ModKey); // "Northern Roads.esp" but should be "Northern Roads - Lux patch.esp"
                        }
                    }
                }
            }
        }
    }
}