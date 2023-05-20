﻿#pragma warning disable IDE0008 // Use explicit type

using System;
using System.Reflection;
using MonoMod.Utils;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using MonoMod.SourceGen.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace MonoMod.Cil {
    [EmitILOverloads("ILOpcodes.txt", ILOverloadKind.Matcher)]
    public static partial class ILPatternMatchingExt {
        #region Equivalence definitions
        private static bool IsEquivalent(int l, int r) => l == r;
        private static bool IsEquivalent(int l, uint r) => unchecked((uint) l) == r;
        private static bool IsEquivalent(long l, long r) => l == r;
        private static bool IsEquivalent(long l, ulong r) => unchecked((ulong) l) == r;
        private static bool IsEquivalent(float l, float r) => l == r;
        private static bool IsEquivalent(double l, double r) => l == r;
        private static bool IsEquivalent(string l, string r) => l == r;

        private static bool IsEquivalent(ILLabel l, ILLabel r) => l == r;
        private static bool IsEquivalent(ILLabel l, Instruction r) => IsEquivalent(l.Target, r);
        private static bool IsEquivalent(Instruction? l, Instruction? r) => l == r;

        private static bool IsEquivalent(TypeReference l, TypeReference r) => l == r;
        private static bool IsEquivalent(TypeReference l, Type r) => l.Is(r);

        private static bool IsEquivalent(MethodReference l, MethodReference r) => l == r;
        private static bool IsEquivalent(MethodReference l, MethodBase r) => l.Is(r);
        private static bool IsEquivalent(MethodReference l, Type type, string name)
            => l.DeclaringType.Is(type) && l.Name == name;

        private static bool IsEquivalent(FieldReference l, FieldReference r) => l == r;
        private static bool IsEquivalent(FieldReference l, FieldInfo r) => l.Is(r);
        private static bool IsEquivalent(FieldReference l, Type type, string name)
            => l.DeclaringType.Is(type) && l.Name == name;

        private static bool IsEquivalent(ILLabel[] l, ILLabel[] r)
            => l == r || l.SequenceEqual(r);
        private static bool IsEquivalent(ILLabel[] l, Instruction[] r) {
            if (l.Length != r.Length)
                return false;
            for (var i = 0; i < l.Length; i++) {
                if (!IsEquivalent(l[i].Target, r[i]))
                    return false;
            }
            return true;
        }

        private static bool IsEquivalent(IMethodSignature l, IMethodSignature r)
            => l == r
            || (
                l.CallingConvention == r.CallingConvention
             && l.HasThis == r.HasThis
             && l.ExplicitThis == r.ExplicitThis
             && IsEquivalent(l.ReturnType, r.ReturnType)
             && l.Parameters.SequenceEqual(r.Parameters, ParameterRefEqualityComparer.Instance)
            );

        private sealed class ParameterRefEqualityComparer : IEqualityComparer<ParameterReference> {
            public static readonly ParameterRefEqualityComparer Instance = new();

            public bool Equals(ParameterReference? x, ParameterReference? y) {
                if (x is null) {
                    return y is null;
                }
                if (y is null) {
                    return false;
                }

                return IsEquivalent(x.ParameterType, y.ParameterType);
            }

            public int GetHashCode([DisallowNull] ParameterReference obj) {
                return obj.ParameterType.GetHashCode();
            }
        }

        private static bool IsEquivalent(IMetadataTokenProvider l, IMetadataTokenProvider r)
            => l == r || l.MetadataToken == r.MetadataToken; // TODO: is this valid?
        private static bool IsEquivalent(IMetadataTokenProvider l, Type r)
            => l is TypeReference tl ? IsEquivalent(tl, r) : false;
        private static bool IsEquivalent(IMetadataTokenProvider l, FieldInfo r)
            => l is FieldReference fl ? IsEquivalent(fl, r) : false;
        private static bool IsEquivalent(IMetadataTokenProvider l, MethodBase r)
            => l is MethodReference ml ? IsEquivalent(ml, r) : false;
        #endregion

        /// <summary>Matches an instruction with opcode <see cref="OpCodes.Ldarg"/>.</summary>
        /// <param name="instr">The instruction to try to match.</param>
        /// <param name="value">The operand value of the instruction.</param>
        /// <returns><see langword="true"/> if the instruction matches; <see langword="false"/> otherwise.</returns>
        public static bool MatchLdarg(this Instruction instr, out int value) {
            Helpers.ThrowIfArgumentNull(instr);
            if (instr.OpCode == OpCodes.Ldarg) {
                value = (int)instr.Operand;
                return true;
            } else if (instr.OpCode == OpCodes.Ldarg_S) {
                value = (byte)instr.Operand;
                return true;
            } else if (instr.OpCode == OpCodes.Ldarg_0) {
                value = 0;
                return true;
            } else if (instr.OpCode == OpCodes.Ldarg_1) {
                value = 1;
                return true;
            } else if (instr.OpCode == OpCodes.Ldarg_2) {
                value = 2;
                return true;
            } else if (instr.OpCode == OpCodes.Ldarg_3) {
                value = 3;
                return true;
            } else {
                value = default;
                return false;
            }
        }

        /// <summary>Matches an instruction with opcode <see cref="OpCodes.Ldloc"/>.</summary>
        /// <param name="instr">The instruction to try to match.</param>
        /// <param name="value">The operand value of the instruction.</param>
        /// <returns><see langword="true"/> if the instruction matches; <see langword="false"/> otherwise.</returns>
        public static bool MatchLdloc(this Instruction instr, out int value) {
            Helpers.ThrowIfArgumentNull(instr);
            if (instr.OpCode == OpCodes.Ldloc || instr.OpCode == OpCodes.Ldloc_S) {
                value = instr.Operand switch {
                    VariableReference vr => vr.Index,
                    int i => i,
                    byte b => b,
                    _ => throw new ArgumentException("Instruction has bad operand kind!")
                };
                return true;
            } else if (instr.OpCode == OpCodes.Ldloc_0) {
                value = 0;
                return true;
            } else if (instr.OpCode == OpCodes.Ldloc_1) {
                value = 1;
                return true;
            } else if (instr.OpCode == OpCodes.Ldloc_2) {
                value = 2;
                return true;
            } else if (instr.OpCode == OpCodes.Ldloc_3) {
                value = 3;
                return true;
            } else {
                value = default;
                return false;
            }
        }

        /// <summary>Matches an instruction with opcode <see cref="OpCodes.Stloc"/>.</summary>
        /// <param name="instr">The instruction to try to match.</param>
        /// <param name="value">The operand value of the instruction.</param>
        /// <returns><see langword="true"/> if the instruction matches; <see langword="false"/> otherwise.</returns>
        public static bool MatchStloc(this Instruction instr, out int value) {
            Helpers.ThrowIfArgumentNull(instr);
            if (instr.OpCode == OpCodes.Stloc || instr.OpCode == OpCodes.Stloc_S) {
                value = instr.Operand switch {
                    VariableReference vr => vr.Index,
                    int i => i,
                    byte b => b,
                    _ => throw new ArgumentException("Instruction has bad operand kind!")
                };
                return true;
            } else if (instr.OpCode == OpCodes.Stloc_0) {
                value = 0;
                return true;
            } else if (instr.OpCode == OpCodes.Stloc_1) {
                value = 1;
                return true;
            } else if (instr.OpCode == OpCodes.Stloc_2) {
                value = 2;
                return true;
            } else if (instr.OpCode == OpCodes.Stloc_3) {
                value = 3;
                return true;
            } else {
                value = default;
                return false;
            }
        }

        /// <summary>Matches an instruction with opcode <see cref="OpCodes.Ldloca"/>.</summary>
        /// <param name="instr">The instruction to try to match.</param>
        /// <param name="value">The operand value of the instruction.</param>
        /// <returns><see langword="true"/> if the instruction matches; <see langword="false"/> otherwise.</returns>
        public static bool MatchLdloca(this Instruction instr, out int value) {
            Helpers.ThrowIfArgumentNull(instr);
            if (instr.OpCode == OpCodes.Ldloca || instr.OpCode == OpCodes.Ldloca_S) {
                value = instr.Operand switch {
                    VariableReference vr => vr.Index,
                    int i => i,
                    byte b => b,
                    _ => throw new ArgumentException("Instruction has bad operand kind!")
                };
                return true;
            } else {
                value = default;
                return false;
            }
        }

        /// <summary>Matches an instruction with opcode <see cref="OpCodes.Ldc_I4"/>.</summary>
        /// <param name="instr">The instruction to try to match.</param>
        /// <param name="value">The operand value of the instruction.</param>
        /// <returns><see langword="true"/> if the instruction matches; <see langword="false"/> otherwise.</returns>
        public static bool MatchLdcI4(this Instruction instr, out int value) {
            Helpers.ThrowIfArgumentNull(instr);
            if (instr.OpCode == OpCodes.Ldc_I4) {
                value = (int) instr.Operand;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_S) {
                value = (byte) instr.Operand;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_0) {
                value = 0;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_1) {
                value = 1;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_2) {
                value = 2;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_3) {
                value = 3;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_4) {
                value = 4;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_5) {
                value = 5;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_6) {
                value = 6;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_7) {
                value = 7;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_8) {
                value = 8;
                return true;
            } else if (instr.OpCode == OpCodes.Ldc_I4_M1) {
                value = -1;
                return true;
            } else {
                value = default;
                return false;
            }
        }

        /// <summary>Matches an instruction with opcode <see cref="OpCodes.Call"/> or <see cref="OpCodes.Callvirt"/>.</summary>
        /// <param name="instr">The instruction to try to match.</param>
        /// <param name="value">The operand value of the instruction.</param>
        /// <returns><see langword="true"/> if the instruction matches; <see langword="false"/> otherwise.</returns>
        public static bool MatchCallOrCallvirt(this Instruction instr, [MaybeNullWhen(false)] out MethodReference value) {
            Helpers.ThrowIfArgumentNull(instr);
            if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) {
                value = (MethodReference) instr.Operand;
                return true;
            } else {
                value = default;
                return false;
            }
        }
    }
}
