﻿using System;
using MonoMod;
using MonoMod.InlineRT;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Reflection.Emit;

namespace MonoMod {
    internal class QuickDebugTestObject {
        public int Value;
        public override string ToString()
            => $"{{QuickDebugTestObject:{Value}}}";
    }
    internal struct QuickDebugTestStruct {
        public int Value;
        public override string ToString()
            => $"{{QuickDebugTestStruct:{Value}}}";
    }
    internal static class QuickDebugTest {

        public static int Run(object[] args) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            return (
                TestReflectionHelperRef() &&
                true // TestReflectionHelperTime()
                ) ? 0 : -1;
        }

        public static bool TestReflectionHelperRef() {
            object[] args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };
            Console.WriteLine($"args: {args[0]} {args[1]} {args[2]} {(args[3] == null ? "null" : args[3])} {args[4]}");

            typeof(QuickDebugTest).GetMethod("TestA").GetDelegate()(null, args);
            Console.WriteLine($"args after Test via ReflectionHelper: {args[0]} {args[1]} {args[2]} {(args[3] == null ? "null" : args[3])} {args[4]}");

            return
                (int) args[0] == 1 &&
                (int) args[1] == 1 &&
                (int) args[2] == 2 &&
                ((QuickDebugTestObject) args[3])?.Value == 1 &&
                ((QuickDebugTestStruct) args[4]).Value == 1
                ;
        }

        public static bool TestReflectionHelperTime() {
            object[] args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };
            Console.WriteLine($"Initial args: {args[0]} {args[1]} {args[2]} {(args[3] == null ? "null" : args[3])} {args[4]}");

            MethodInfo method = typeof(QuickDebugTest).GetMethod("TestA");

            const long runs = 100000000;

            Console.WriteLine("Test-running Stopwatch");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            sw.Stop();
            TimeSpan timeNewBox = sw.Elapsed;
            TimeSpan timeLocals = sw.Elapsed;
            TimeSpan timeMutable = sw.Elapsed;
            sw.Reset();

            Console.WriteLine("Generating local-less delegate");
            DynamicMethodDelegate dmdNewBox = method.CreateDelegate(directBoxValueAccess: true);
            Console.WriteLine("Test-running dmdNewBox");
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };
            dmdNewBox(null, args);
            if (!(
                (int) args[0] == 1 &&
                (int) args[1] == 1 &&
                (int) args[2] == 2 &&
                ((QuickDebugTestObject) args[3])?.Value == 1 &&
                ((QuickDebugTestStruct) args[4]).Value == 1
                )) return false;
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };

            Console.WriteLine($"Timing dmdNewBox {runs} runs");
            sw.Start();
            for (long i = runs; i > -1; --i) {
                dmdNewBox(null, args);
            }
            sw.Stop();
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };
            timeNewBox = sw.Elapsed;
            sw.Reset();
            Console.WriteLine($"time: {timeNewBox}");


            Console.WriteLine("Generating mutability-ignoring delegate");
            DynamicMethodDelegate dmdMutable = method.CreateDelegate(directBoxValueAccess: false);
            Console.WriteLine("Test-running dmdMutable");
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };
            dmdMutable(null, args);
            if (!(
                (int) args[0] == 1 &&
                (int) args[1] == 1 &&
                (int) args[2] == 2 &&
                ((QuickDebugTestObject) args[3])?.Value == 1 &&
                ((QuickDebugTestStruct) args[4]).Value == 1
                )) return false;
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };

            Console.WriteLine("Timing dmdMutable {runs} runs");
            sw.Start();
            for (long i = runs; i > -1; --i) {
                dmdMutable(null, args);
            }
            sw.Stop();
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };
            timeMutable = sw.Elapsed;
            sw.Reset();
            Console.WriteLine($"time: {timeMutable}");


            Console.WriteLine("Generating localed delegate");
            DynamicMethodDelegate dmdLocals = method.CreateDelegateUsingLocals();
            Console.WriteLine("Test-running dmdLocals");
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };
            dmdLocals(null, args);
            if (!(
                (int) args[0] == 1 &&
                (int) args[1] == 1 &&
                (int) args[2] == 2 &&
                ((QuickDebugTestObject) args[3])?.Value == 1 &&
                ((QuickDebugTestStruct) args[4]).Value == 1
                )) return false;
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };

            Console.WriteLine("Timing dmdLocals {runs} runs");
            sw.Start();
            for (long i = runs; i > -1; --i) {
                dmdLocals(null, args);
            }
            sw.Stop();
            args = new object[] { 1, 0, 0, null, new QuickDebugTestStruct() };
            timeLocals = sw.Elapsed;
            sw.Reset();
            Console.WriteLine($"time: {timeLocals}");

            Console.WriteLine($"newbox / locals: {(double) timeNewBox.Ticks / (double) timeLocals.Ticks}");
            Console.WriteLine($"locals / newbox: {(double) timeLocals.Ticks / (double) timeNewBox.Ticks}");

            Console.WriteLine($"newbox / mutable: {(double) timeNewBox.Ticks / (double) timeMutable.Ticks}");
            Console.WriteLine($"mutable / newbox: {(double) timeMutable.Ticks / (double) timeNewBox.Ticks}");

            Console.WriteLine($"locals / mutable: {(double) timeLocals.Ticks / (double) timeMutable.Ticks}");
            Console.WriteLine($"mutable / locals: {(double) timeMutable.Ticks / (double) timeLocals.Ticks}");

            Console.WriteLine("Pass");
            return true;
        }

        public static void TestA(int a, ref int b, out int c, out QuickDebugTestObject d, ref QuickDebugTestStruct e) {
            b = b + 1;
            c = b * 2;
            d = new QuickDebugTestObject();
            d.Value = a;
            e.Value = a;
        }

        private static DynamicMethodDelegate CreateDelegateUsingLocals(this MethodBase method) {
            DynamicMethod dynam = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object), typeof(object[]) }, typeof(ReflectionHelper).Module, true);
            ILGenerator il = dynam.GetILGenerator();

            ParameterInfo[] args = method.GetParameters();

            LocalBuilder[] locals = new LocalBuilder[args.Length];
            for (int i = 0; i < args.Length; i++) {
                if (args[i].ParameterType.IsByRef)
                    locals[i] = il.DeclareLocal(args[i].ParameterType.GetElementType(), true);
                else
                    locals[i] = il.DeclareLocal(args[i].ParameterType, true);
            }

            for (int i = 0; i < args.Length; i++) {
                il.Emit(OpCodes.Ldarg_1);
                il.EmitFast_Ldc_I4(i);

                Type argType = args[i].ParameterType;
                bool argIsByRef = argType.IsByRef;
                if (argIsByRef)
                    argType = argType.GetElementType();
                bool argIsValueType = argType.IsValueType;

                il.Emit(OpCodes.Ldelem_Ref);
                if (argIsValueType) {
                    il.Emit(OpCodes.Unbox_Any, argType);
                } else {
                    il.Emit(OpCodes.Castclass, argType);
                }
                il.Emit(OpCodes.Stloc, locals[i]);
            }

            if (!method.IsStatic && !method.IsConstructor) {
                il.Emit(OpCodes.Ldarg_0);
                if (method.DeclaringType.IsValueType) {
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
                }
            }

            for (int i = 0; i < args.Length; i++) {
                if (args[i].ParameterType.IsByRef)
                    il.Emit(OpCodes.Ldloca_S, locals[i]);
                else
                    il.Emit(OpCodes.Ldloc, locals[i]);
            }

            if (method.IsConstructor) {
                il.Emit(OpCodes.Newobj, method as ConstructorInfo);
            } else if (method.IsFinal || !method.IsVirtual) {
                il.Emit(OpCodes.Call, method as MethodInfo);
            } else {
                il.Emit(OpCodes.Callvirt, method as MethodInfo);
            }

            Type returnType = method.IsConstructor ? method.DeclaringType : (method as MethodInfo).ReturnType;
            if (returnType != typeof(void)) {
                if (returnType.IsValueType) {
                    il.Emit(OpCodes.Box, returnType);
                }
            } else {
                il.Emit(OpCodes.Ldnull);
            }

            for (int i = 0; i < args.Length; i++) {
                if (args[i].ParameterType.IsByRef) {
                    il.Emit(OpCodes.Ldarg_1);
                    il.EmitFast_Ldc_I4(i);

                    il.Emit(OpCodes.Ldloc, locals[i]);
                    if (locals[i].LocalType.IsValueType)
                        il.Emit(OpCodes.Box, locals[i].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }

            il.Emit(OpCodes.Ret);

            return (DynamicMethodDelegate) dynam.CreateDelegate(typeof(DynamicMethodDelegate));
        }

    }
}