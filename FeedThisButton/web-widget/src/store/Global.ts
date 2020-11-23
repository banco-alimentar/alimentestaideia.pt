import { Writable, writable } from 'svelte/store';

export type ExecutionMode = 'mock' | 'dev' | 'prod';

export const loading = writable(false);
export const errorMessage:Writable<string> = writable(null);
export const executionMode:Writable<ExecutionMode> = writable('mock');