import { useMemo } from 'react';

export function useTestHook(): number {
    return useMemo(() => 
    {
        return 1;
    }, []);
}