export type RunnerMode = 'read-only' | 'workspace-write';

export interface RunnerSettings {
    workspaceRoot: string;
    reposRoot: string;
    mode: RunnerMode;
}
