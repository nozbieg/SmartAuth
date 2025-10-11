import React, { createContext, useContext, useEffect, useState } from "react";
import {type FeatureFlags, getFeatureFlags } from "../commons/featureFlags";


const FlagsCtx = createContext<FeatureFlags | null>(null);
const FlagsLoadingCtx = createContext<boolean>(true);


export const FeatureFlagsProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [flags, setFlags] = useState<FeatureFlags | null>(null);
    const [loading, setLoading] = useState(true);


    useEffect(() => {
        let mounted = true;
        (async () => {
            try {
                const f = await getFeatureFlags();
                if (mounted) setFlags(f);
            } finally {
                if (mounted) setLoading(false);
            }
        })();
        return () => { mounted = false; };
    }, []);


    return (
        <FlagsCtx.Provider value={flags}>
            <FlagsLoadingCtx.Provider value={loading}>{children}</FlagsLoadingCtx.Provider>
        </FlagsCtx.Provider>
    );
};


export function useFeatureFlags() {
    const flags = useContext(FlagsCtx);
    const loading = useContext(FlagsLoadingCtx);
    return { flags, loading };
}