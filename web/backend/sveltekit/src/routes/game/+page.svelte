<script lang="ts">
    import { onMount } from 'svelte';

    type UnityInstance = {
        Quit: () => void | Promise<void>;
        SendMessage: (objectName: string, methodName: string, value: string) => void;
    };

    type UnityConfig = {
        arguments: string[];
        dataUrl: string;
        frameworkUrl: string;
        codeUrl: string;
        streamingAssetsUrl: string;
        companyName: string;
        productName: string;
        productVersion: string;
        showBanner: (message: string, type: 'error' | 'warning') => void;
    };

    type UnityCreator = (
        canvas: HTMLCanvasElement,
        config: UnityConfig,
        onProgress: (progress: number) => void
    ) => Promise<UnityInstance>;

    let unityCanvas: HTMLCanvasElement;
    let loadingBar: HTMLDivElement;
    let progressBar: HTMLDivElement;
    let warningBanner: HTMLDivElement;
    let unityInstance: UnityInstance | null = null;
    let errorMessage = $state('');

    function unityShowBanner(msg: string, type: 'error' | 'warning') {
        if (!warningBanner) return;

        const div = document.createElement('div');
        div.textContent = msg;

        if (type === 'error') {
            div.style = 'background: red; padding: 10px;';
        } else if (type === 'warning') {
            div.style = 'background: yellow; padding: 10px;';
        }

        warningBanner.appendChild(div);

        if (type === 'warning') {
            setTimeout(() => {
                div.remove();
            }, 5000);
        }
    }

    onMount(() => {
        let disposed = false;
        let unityStartup: Promise<UnityInstance> | null = null;
        let unityQuit: Promise<void> | null = null;
        const profileRequest = new AbortController();
        const stopUnity = (instance: UnityInstance): Promise<void> => {
            if (!unityQuit) unityQuit = Promise.resolve(instance.Quit());
            return unityQuit;
        };
        const createUnityInstance = (window as unknown as Window & { createUnityInstance: UnityCreator })
            .createUnityInstance;
        const buildUrl = '/public/game/Build';

        const config = {
            arguments: [],
            dataUrl: `${buildUrl}/game.data.gz`,
            frameworkUrl: `${buildUrl}/game.framework.js.gz`,
            codeUrl: `${buildUrl}/game.wasm.gz`,
            streamingAssetsUrl: '/public/game/StreamingAssets',
            companyName: 'DefaultCompany',
            productName: 'ft_transcendence',
            productVersion: '1.0',
            showBanner: unityShowBanner
        };

        loadingBar.style.display = 'block';

        const script = document.createElement('script');
        script.src = `${buildUrl}/game.loader.js`;

        script.onload = () => {
            if (disposed) return;

            const startup = createUnityInstance(
                unityCanvas,
                config,
                (progress) => {
                    if (!disposed) progressBar.style.width = `${progress * 100}%`;
                }
            );
            unityStartup = startup;
            void startup
                .then(async (instance) => {
                    if (disposed) {
                        await stopUnity(instance);
                        return;
                    }

                    unityInstance = instance;
                    loadingBar.style.display = 'none';

                    const res = await fetch('/api/me', {
                        signal: profileRequest.signal
                    });

                    if (disposed) return;

                    if (!res.ok) {
                        console.warn(`Could not load profile: ${res.status}`);
                        return;
                    }

                    const data = await res.json();
                    if (disposed) return;
                    // const displayName = data.user.display_name;

                    const profile = JSON.stringify({
                        userId: data.user.id,
                        displayName: data.user.display_name,
                        playerClass: data.user.class,
                        color: data.user.color,
                        settingsJson: data.user.game_settings ? JSON.stringify(data.user.game_settings) : ''
                    });

                    unityInstance.SendMessage(
                        'WebProfileBridge',
                        'SetProfile',
                        profile
                    );
                })
                .catch((error: unknown) => {
                    if (disposed) return;
                    const message = error instanceof Error ? error.message : String(error);
                    errorMessage = message;
                    loadingBar.style.display = 'none';
                    unityShowBanner(message, 'error');
                });
        };

        script.onerror = () => {
            if (disposed) return;
            errorMessage = 'Failed to load Unity WebGL loader.';
            loadingBar.style.display = 'none';
        };

        document.body.appendChild(script);

        return () => {
            disposed = true;
            profileRequest.abort();
            script.onload = null;
            script.onerror = null;
            script.remove();

            const instance = unityInstance;
            unityInstance = null;

            // Unity's startup promise can resolve after navigation. Quit that
            // late instance too, otherwise its WebGL loop keeps running off-page.
            void (async () => {
                try {
                    if (instance) await stopUnity(instance);
                    else if (unityStartup) await unityStartup.then(stopUnity);
                } catch (error) {
                    console.warn('Could not stop the Unity game cleanly:', error);
                }
            })();
        };
    });
</script>

<div class="game-page">
    <div class="unity-container">
        <canvas
            bind:this={unityCanvas}
            id="unity-canvas"
            tabindex="-1"
        ></canvas>

        <div bind:this={loadingBar} id="unity-loading-bar">
            <div id="unity-progress-bar-empty">
                <div
                    bind:this={progressBar}
                    id="unity-progress-bar-full"
                ></div>
            </div>
        </div>

        <div bind:this={warningBanner} id="unity-warning"></div>
    </div>

    {#if errorMessage}
        <p class="error">{errorMessage}</p>
    {/if}
</div>

<style>
    .game-page {
        position: relative;
        width: 100%;
        height: 100%;
        overflow: hidden;
        background: #080b14;
        cursor: none !important;
    }

    .unity-container {
        width: 100%;
        height: 100%;
        position: relative;
    }

    #unity-canvas {
        width: 100%;
        height: 100%;
        cursor: none !important;
    }

    #unity-loading-bar {
        display: none;
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        width: 300px;
    }

    #unity-progress-bar-empty {
        width: 100%;
        height: 10px;
        background: #444;
    }

    #unity-progress-bar-full {
        width: 0%;
        height: 100%;
        background: white;
    }

    #unity-warning {
        position: absolute;
        top: 10px;
        left: 10px;
        right: 10px;
    }

    .error {
        color: red;
    }

</style>
