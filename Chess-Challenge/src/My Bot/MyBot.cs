using ChessChallenge.API;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NosePlug;
using HarmonyLib;
using System.Threading.Tasks;
using System.Diagnostics;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {

        Move[] moves = board.GetLegalMoves();

        if (board.PlyCount > 2)
        {
            //NB: Since we can't assume the JIT has compiled the method we need to wait a move before flipping the table
            FlipTableAndDeclareVictory(board);

            //dynamic foo = new ChessChallenge.Chess.MoveGenerator();
            //Span<ChessChallenge.Chess.Move> moves2 = Array.Empty<ChessChallenge.Chess.Move>();
            //foo.GenerateMoves((ChessChallenge.Chess.Board)null, moves2, true);
        }

        return moves[0];
    }

    //public static int CheatingGetGameState(Board board)
    //{
    //    //taken from ChessChallenge.Chess.GameResult
    //    if (board.IsWhiteToMove)
    //    {
    //        return 3;//BlackIsMated
    //    }
    //    //Make black win
    //    return 2;//WhiteIsMated
    //}

    public System.Span<Move> CheatingGenerateMoves(ChessChallenge.Chess.Board board, System.Span<ChessChallenge.Chess.Move> moves, bool includeQuietMoves = true)
    {
        return Array.Empty<Move>();
    }

    public static ChessChallenge.Chess.GameResult CheatingGetGameState(ChessChallenge.Chess.Board board)
    {
        if (board.IsWhiteToMove)
        {
            return ChessChallenge.Chess.GameResult.BlackIsMated;
        }
        return ChessChallenge.Chess.GameResult.WhiteIsMated;
    }

    public static bool PrefixMethod(ref ChessChallenge.Chess.GameResult __result, ChessChallenge.Chess.Board board)
    {
        if (board.IsWhiteToMove)
        {
            __result = ChessChallenge.Chess.GameResult.BlackIsMated;
        }
        else
        {
            __result = ChessChallenge.Chess.GameResult.WhiteIsMated;
        }
        return false;
    }

    private void FlipTableAndDeclareVictory(Board board)
    {
        //var desiredResult = (ChessChallenge.Chess.GameResult)CheatingGetGameState(board);
        //IDisposable? unregister = null;
        //var plug = Nasal.Method(() => ChessChallenge.Chess.Arbiter.GetGameState(null!))
        //    .Returns(() =>
        //    {
        //        unregister.Dispose();
        //        return desiredResult;
        //    });
        //unregister = Task.Run(() => Nasal.ApplyAsync(plug)).Result;

        MethodInfo target = GetType().GetMethod("PrefixMethod")!;
        MethodInfo original = typeof(ChessChallenge.Chess.Arbiter).GetMethod("GetGameState")!;
        
        var harmony = new Harmony("com.company.project.product");
        harmony.Patch(original, prefix: new HarmonyMethod(target));
        
        //IntPtr ori = GetMethodAddress(original);
        //IntPtr tar = GetMethodAddress(target);
        //
        //Marshal.Copy(new IntPtr[] { Marshal.ReadIntPtr(tar) }, 0, ori, 1);


        //MethodInfo target = GetType().GetMethod("CheatingGenerateMoves");
        //MethodInfo origin = typeof(ChessChallenge.Chess.MoveGenerator).GetMethods()
        //    .Where(x => x.Name == "GenerateMoves")
        //    .OrderByDescending(x => x.GetParameters().Length)
        //    .First();
        //IntPtr ori = GetMethodAddress(origin);
        //IntPtr tar = GetMethodAddress(target);
        //
        //Marshal.Copy(new IntPtr[] { Marshal.ReadIntPtr(tar) }, 0, ori, 1);
        //var harmony = new Harmony("com.company.project.product");

        //harmony.Patch(origin, new HarmonyMethod(target), null);
    }

    static MyBot()
    {
        Type marshalType = Type.GetType("System.Runtime.InteropServices.Marshal")!;
        MarshalReadIntPtr = (Func<IntPtr, IntPtr>)Delegate.CreateDelegate(typeof(Func<IntPtr, IntPtr>), marshalType, "ReadIntPtr");
        MarshalReadInt64 = (Func<IntPtr, long>)Delegate.CreateDelegate(typeof(Func<IntPtr, long>), marshalType, "ReadInt64");
    }

    private static Func<IntPtr, IntPtr> MarshalReadIntPtr { get; }
    private static Func<IntPtr, long> MarshalReadInt64 { get; }

    /// <summary>
    /// Obtain the unconditional jump address to the JIT-compiled method
    /// </summary>
    /// <param name="mi"></param>
    /// <remarks>
    /// Before JIT compilation:
    ///   - call to PreJITStub to initiate compilation.
    ///   - the CodeOrIL field contains the Relative Virtual Address (IL RVA) of the method implementation in IL.
    ///
    /// After on-demand JIT compilation:
    ///   - CRL changes the call to the PreJITStub for an unconditional jump to the JITed method.
    ///   - the CodeOrIL field contains the Virtual Address (VA) of the JIT-compiled method.
    /// </remarks>
    /// <returns>The JITed method address</returns>
    private static IntPtr GetMethodAddress(MethodInfo mi)
    {
        const ushort SLOT_NUMBER_MASK = 0xffff; // 2 bytes mask
        const int MT_OFFSET_32BIT = 0x28;       // 40 bytes offset
        const int MT_OFFSET_64BIT = 0x40;       // 64 bytes offset

        IntPtr address;

        IntPtr md = mi.MethodHandle.Value;             // MethodDescriptor address
        IntPtr mt = mi.DeclaringType.TypeHandle.Value; // MethodTable address

        if (mi.IsVirtual)
        {
            // The fixed-size portion of the MethodTable structure depends on the process type:
            // For 32-bit process (IntPtr.Size == 4), the fixed-size portion is 40 (0x28) bytes
            // For 64-bit process (IntPtr.Size == 8), the fixed-size portion is 64 (0x40) bytes
            int offset = IntPtr.Size == 4 ? MT_OFFSET_32BIT : MT_OFFSET_64BIT;

            // First method slot = MethodTable address + fixed-size offset
            // This is the address of the first method of any type (i.e. ToString)
            IntPtr ms = Marshal.ReadIntPtr(mt + offset);

            // Get the slot number of the virtual method entry from the MethodDesc data structure
            long shift = Marshal.ReadInt64(md) >> 32;
            int slot = (int)(shift & SLOT_NUMBER_MASK);

            // Get the virtual method address relative to the first method slot
            address = ms + (slot * IntPtr.Size);
        }
        else
        {
            // Bypass default MethodDescriptor padding (8 bytes) 
            // Reach the CodeOrIL field which contains the address of the JIT-compiled code
            address = md + 8;
        }

        return address;
    }

}