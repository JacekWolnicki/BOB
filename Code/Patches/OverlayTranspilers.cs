﻿using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;


namespace BOB
{
    /// <summary>
    /// Harmony transpilers for registering overlays to render.
    /// </summary>
    public static class OverlayTranspilers
    {
        /// <summary>
        /// Harmony transpiler for BuildingAI.RenderProps, to insert calls to highlight selected props/trees
        /// </summary>
        /// <param name="original">Original method</param>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="generator">IL generator</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> BuildingTranspiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // ILCode local variable indexes.
            const int PositionIndex = 8;
            const int IVarIndex = 11;
            const int BuildingInfoPropIndex = 12;
            const int TreeVarIndex = 15;
            const int TreePositionVarIndex = 29;
            const int DataVector2VarIndex = 30;

            // ILCode argument indexes.
            const int BuildingArg = 3;

            // Instruction parsing.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;


            // Iterate through all instructions in original method.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction and add it to output.
                instruction = instructionsEnumerator.Current;
                yield return instruction;

                // Is this stloc.s 17 (should be only one in entire method)?
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder localBuilder1 && localBuilder1.LocalIndex == 17)
                {
                    // Yes - insert call to RenderOverlays.HighlightBuildingProp(i, prop, ref building, position)
                    Logging.KeyMessage("adding building HighlightBuildingProp call after stloc.s 17");
                    yield return new CodeInstruction(OpCodes.Ldloc_S, IVarIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, BuildingInfoPropIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, BuildingArg);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, PositionIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightBuildingProp)));
                }

                // Tree candidate - ldloc.s 2
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == DataVector2VarIndex)
                {
                    // Found a possible candidate - are there following instructions?
                    if (instructionsEnumerator.MoveNext())
                    {
                        // Yes - get the next instruction.
                        instruction = instructionsEnumerator.Current;
                        yield return instruction;

                        // Is this new instruction a call to Void RenderInstance?
                        if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().StartsWith("Void RenderInstance"))
                        {
                            // Yes - insert call to PropOverlays.Highlight method after original call.
                            Logging.KeyMessage("adding building HighlightTree call after RenderInstance");
                            yield return new CodeInstruction(OpCodes.Ldloc_S, IVarIndex);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, TreeVarIndex);
                            yield return new CodeInstruction(OpCodes.Ldarg_S, BuildingArg);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, TreePositionVarIndex);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightBuildingTree)));
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Harmony transpiler for NetLane.RenderInstance, to insert calls to highlight selected props/tree.
        /// </summary>
        /// <param name="original">Original method</param>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="generator">IL generator</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> NetTranspiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // ILCode local variable indexes.
            const int SurfaceMapping2Index = 9;
            const int IVarIndex = 11;
            const int PropVarIndex = 16;
            const int PropPositionVarIndex = 23;
            const int TreeVarIndex = 31;
            const int TreePositionVarIndex = 38;


            // Instruction parsing.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;
            bool foundPropCandidate = false, foundTreeCandidate = false;
            string candidateName, methodName;
            int prefabIndex, positionIndex;


            // Iterate through all instructions in original method.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction and add it to output.
                instruction = instructionsEnumerator.Current;
                yield return instruction;

                // Looking for possible precursor calls to "Void RenderInstance".
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == SurfaceMapping2Index)
                {
                    // ldloc.s 9
                    foundPropCandidate = true;
                }
                else if (instruction.opcode == OpCodes.Ldc_I4_1)
                {
                    // ldc.i4.1.
                    foundPropCandidate = true;
                }
                else if (instruction.opcode == OpCodes.Ldsfld)
                {
                    // Tree candidate - ldsfld
                    foundTreeCandidate = true;
                }

                // Did we find a possible candidate?
                if (foundPropCandidate || foundTreeCandidate)
                {
                    // Yes - are there following instructions?
                    if (instructionsEnumerator.MoveNext())
                    {
                        // Yes - get the next instruction.
                        instruction = instructionsEnumerator.Current;
                        yield return instruction;

                        // Is this new instruction a call to Void RenderInstance?
                        if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().StartsWith("Void RenderInstance"))
                        {
                            // Yes - prop or tree?
                            if (foundPropCandidate)
                            {
                                // Prop.
                                candidateName = "Prop";
                                prefabIndex = PropVarIndex;
                                positionIndex = PropPositionVarIndex;
                                methodName = nameof(RenderOverlays.HighlightProp);
                            }
                            else
                            {
                                // Tree.
                                candidateName = "Tree";
                                prefabIndex = TreeVarIndex;
                                positionIndex = TreePositionVarIndex;
                                methodName = nameof(RenderOverlays.HighlightTree);
                            }

                            // Insert call to PropOverlays.Highlight method after original call.
                            Logging.KeyMessage("adding network Highlight", candidateName, " call after RenderInstance");
                            yield return new CodeInstruction(OpCodes.Ldloc_S, prefabIndex);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, positionIndex);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), methodName));
                        }
                    }

                    // Reset flags for next pass.
                    foundPropCandidate = false;
                    foundTreeCandidate = false;
                }
            }
        }


        /// <summary>
        /// Harmony transpiler for TreeInstance.RenderInstance, to insert calls to highlight selected props/tree.
        /// </summary>
        /// <param name="original">Original method</param>
        /// <param name="instructions">Original ILCode</param>
        /// <param name="generator">IL generator</param>
        /// <returns>Patched ILCode</returns>
        public static IEnumerable<CodeInstruction> TreeTranspiler(MethodBase original, IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            // ILCode local variable indexes.
            const int TreeVarIndex = 0;
            const int TreePositionVarIndex = 1;
            const int DefaultColorLocationVarIndex = 5;


            // Instruction parsing.
            IEnumerator<CodeInstruction> instructionsEnumerator = instructions.GetEnumerator();
            CodeInstruction instruction;

            // Iterate through all instructions in original method.
            while (instructionsEnumerator.MoveNext())
            {
                // Get next instruction and add it to output.
                instruction = instructionsEnumerator.Current;
                yield return instruction;

                // Looking for possible precursor calls to "Void DrawMesh".
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == DefaultColorLocationVarIndex)
                {
                    // Found ldloc.1 - are there following instructions?
                    if (instructionsEnumerator.MoveNext())
                    {
                        // Yes - get the next instruction.
                        instruction = instructionsEnumerator.Current;
                        yield return instruction;

                        // Is this new instruction a call to Void RenderInstance?

                        if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().StartsWith("Void RenderInstance"))
                        {
                            // Yes - insert call to PropOverlays.Highlight method after original call.
                            Logging.KeyMessage("adding tree Highlight call after RenderInstance");
                            yield return new CodeInstruction(OpCodes.Ldloc_S, TreeVarIndex);
                            yield return new CodeInstruction(OpCodes.Ldloc_S, TreePositionVarIndex);
                            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RenderOverlays), nameof(RenderOverlays.HighlightTree)));
                        }
                    }
                }
            }
        }
    }
}