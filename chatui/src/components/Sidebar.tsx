import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Chat } from '../types/ChatTypes';
import { RunnerSettings } from '../types/RunnerSettings';

interface SidebarProps {
    chats: Chat[];
    selectedChatId: string | null;
    loadingChats: boolean;
    handleDeleteChat: (e: React.MouseEvent, chatId: string) => void;
    onNewChat: () => void;
    runnerSettings: RunnerSettings;
    onRunnerSettingsChange: (settings: RunnerSettings) => void;
}

const Sidebar: React.FC<SidebarProps> = ({
    chats,
    selectedChatId,
    loadingChats,
    handleDeleteChat,
    onNewChat,
    runnerSettings,
    onRunnerSettingsChange
}) => {
    const navigate = useNavigate();

    const updateSettings = (patch: Partial<RunnerSettings>) => {
        onRunnerSettingsChange({ ...runnerSettings, ...patch });
    };
    
    return (
        <div className="sidebar">
            <div className="sidebar-header">
                <h2>Chats</h2>
                {loadingChats && <p>Loading...</p>}
            </div>
            <button onClick={onNewChat} className="new-chat-button sidebar-new-chat">
                + New chat
            </button>
            <div className="settings-panel">
                <div className="settings-title">Runner</div>
                <label className="settings-label" htmlFor="workspace-root">Workspace root</label>
                <input
                    id="workspace-root"
                    className="settings-input"
                    type="text"
                    placeholder="~/kipperbit/kipperbit-shared"
                    value={runnerSettings.workspaceRoot}
                    onChange={(e) => updateSettings({ workspaceRoot: e.target.value })}
                />
                <label className="settings-label" htmlFor="repos-root">Repos root</label>
                <input
                    id="repos-root"
                    className="settings-input"
                    type="text"
                    placeholder="~/kipperbit/repos"
                    value={runnerSettings.reposRoot}
                    onChange={(e) => updateSettings({ reposRoot: e.target.value })}
                />
                <label className="settings-label" htmlFor="runner-mode">Mode</label>
                <select
                    id="runner-mode"
                    className="settings-select"
                    value={runnerSettings.mode}
                    onChange={(e) => updateSettings({ mode: e.target.value as RunnerSettings['mode'] })}
                >
                    <option value="read-only">read-only</option>
                    <option value="workspace-write">workspace-write</option>
                </select>
            </div>
            <ul className="chat-list">
                {chats.map(chat => (
                    <li
                        key={chat.id}
                        onClick={() => navigate(`/chat/${chat.id}`)}
                        className={`chat-item ${selectedChatId === chat.id ? 'selected' : ''}`}
                    >
                        <span className="chat-name">{chat.name}</span>
                        <button
                            className="delete-chat-button"
                            onClick={(e) => handleDeleteChat(e, chat.id)}
                            title="Delete chat"
                        >
                            ×
                        </button>
                    </li>
                ))}
            </ul>
        </div>
    );
};

export default Sidebar;
