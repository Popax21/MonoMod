﻿using System;

namespace MonoMod.Core {
    [Flags]
    public enum RuntimeFeature {
        None,

        PreciseGC = 0x01,
        CompileMethodHook = 0x02,

        // No runtime supports this *at all* at the moment, but it's here for future use
        ILDetour = 0x04,

        GenericSharing = 0x08,
        ListGenericInstantiations = 0x40,

        DisableInlining = 0x10,
        Uninlining = 0x20,

        RequiresMethodPinning = 0x80,
        RequiresMethodIdentification = 0x100,

        RequiresBodyThunkWalking = 0x200,

        HasKnownABI = 0x400,

        // TODO: what other runtime feature flags would be useful to have?
    }
}