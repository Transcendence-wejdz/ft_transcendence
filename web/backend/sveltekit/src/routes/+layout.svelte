<script lang="ts">
    import { page } from '$app/state';

    let { data, children } = $props();
    const HEARTBEAT_INTERVAL_MS = 5_000;

    $effect(() => {
        if (!data.user) return;

        async function sendHeartbeat() {
            try {
                const response = await fetch('/api/friends-list/presence', {
                    method: 'POST'
                });

                if (!response.ok) {
                    console.error('Heartbeat request failed.');
                }
            } catch (cause) {
                console.error('Heartbeat network error:', cause);
            }
        }

        void sendHeartbeat();
        const intervalId = setInterval(() => {
            void sendHeartbeat();
        }, HEARTBEAT_INTERVAL_MS);

        return () => clearInterval(intervalId);
    });

    async function logout() {
        const response = await fetch('/api/logout', {
            method: 'POST'
        });

        if (response.ok) {
            window.location.assign('/login');
        }
    }
</script>

<!-- Full page loads in and out of /game so the Unity instance never outlives its page and other pages refetch fresh data. -->
<div class="app-shell" class:game-route={page.url.pathname === '/game'}
     data-sveltekit-reload={page.url.pathname === '/game' ? '' : 'off'}>
    <header class="topbar">
        <a class="brand" href="/" aria-label="Transcendence home">
            <span class="brand-mark">T</span>
            <span>Transcendence</span>
        </a>

        <nav class="main-nav" aria-label="Main navigation">
            <a href="/game" data-sveltekit-reload>Play</a>
            <a href="/setting">Settings</a>
            <a href="/project">Project</a>
            <a href="/team">Team</a>
        </nav>

        <div class="account-nav">
            {#if data.user}
                <span class="welcome">Hi, {data.user.display_name}</span>
                <button class="button button-small button-quiet" onclick={logout}>Log out</button>
            {:else}
                <a class="login-link" href="/login">Log in</a>
                <a class="button button-small" href="/register">Sign up</a>
            {/if}
        </div>
    </header>

    <main class="page-content">
        {@render children()}
    </main>
</div>

<style>
    :global(*) {
        box-sizing: border-box;
    }

    :global(body) {
        margin: 0;
        min-width: 320px;
        min-height: 100vh;
        color: #edf2ff;
        background:
            radial-gradient(ellipse at 15% 0%, rgba(48, 76, 130, 0.24), transparent 38rem),
            #0c1020;
        font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
    }

    :global(button), :global(input) {
        font: inherit;
    }

    :global(a) {
        color: inherit;
    }

    .app-shell {
        min-height: 100vh;
    }

    .topbar {
        min-height: 76px;
        display: flex;
        align-items: center;
        gap: 2rem;
        padding: 0 clamp(1rem, 5vw, 4.5rem);
        border-bottom: 1px solid rgba(189, 204, 242, 0.1);
        background: rgba(12, 16, 32, 0.78);
        backdrop-filter: blur(14px);
    }

    .brand {
        display: inline-flex;
        align-items: center;
        gap: 0.7rem;
        color: #f4f6ff;
        font-size: 0.98rem;
        font-weight: 750;
        letter-spacing: 0.02em;
        text-decoration: none;
        white-space: nowrap;
    }

    .brand-mark {
        display: grid;
        width: 2rem;
        height: 2rem;
        place-items: center;
        border-radius: 0.7rem;
        color: #161222;
        background: #f5b85c;
        font-weight: 900;
    }

    .main-nav, .account-nav {
        display: flex;
        align-items: center;
        gap: 1.25rem;
    }

    .main-nav {
        margin-left: auto;
    }

    .main-nav a, .login-link {
        color: #aeb8d3;
        font-size: 0.9rem;
        text-decoration: none;
        transition: color 150ms ease;
    }

    .main-nav a:hover, .login-link:hover {
        color: #fff;
    }

    .account-nav {
        gap: 1rem;
    }

    .welcome {
        color: #aeb8d3;
        font-size: 0.88rem;
    }

    :global(.button) {
        display: inline-flex;
        min-height: 2.8rem;
        align-items: center;
        justify-content: center;
        padding: 0.75rem 1.15rem;
        border: 1px solid #f5b85c;
        border-radius: 0.75rem;
        color: #171321;
        background: #f5b85c;
        font-weight: 750;
        text-decoration: none;
        cursor: pointer;
        transition: transform 150ms ease, background 150ms ease;
    }

    :global(.button:hover) {
        transform: translateY(-1px);
        background: #ffca78;
    }

    :global(.button:disabled) {
        cursor: wait;
        opacity: 0.65;
        transform: none;
    }

    :global(.button-small) {
        min-height: 2.35rem;
        padding: 0.55rem 0.9rem;
        font-size: 0.85rem;
    }

    :global(.button-quiet) {
        border-color: rgba(189, 204, 242, 0.2);
        color: #e4e9fa;
        background: rgba(255, 255, 255, 0.05);
    }

    :global(.page-content) {
        width: min(100% - 2rem, 1100px);
        margin: 0 auto;
        padding: clamp(2rem, 7vh, 5rem) 0;
    }

    .game-route :global(.page-content) {
        flex: 1;
        width: 100%;
        min-height: 0;
        height: auto;
        overflow: hidden;
        padding: 0;
    }

    .game-route {
        display: flex;
        height: 100vh;
        height: 100dvh;
        min-height: 0;
        flex-direction: column;
        overflow: hidden;
    }

    .game-route .topbar {
        position: fixed;
        top: 0;
        right: 0;
        left: 0;
        z-index: 10;
        transform: translateY(-100%);
        transition: transform 200ms ease 300ms;
    }

    .game-route .topbar::after {
        content: '';
        position: absolute;
        right: 0;
        bottom: -12px;
        left: 0;
        height: 12px;
    }

    .game-route .topbar:hover, .game-route .topbar:focus-within {
        transform: translateY(0);
        transition-delay: 0ms;
    }

    @media (max-width: 720px) {
        .topbar {
            flex-wrap: wrap;
            gap: 0.8rem 1rem;
            padding-top: 0.8rem;
            padding-bottom: 0.8rem;
        }

        .main-nav {
            order: 3;
            width: 100%;
            justify-content: space-between;
            gap: 0.5rem;
        }

        .main-nav a {
            font-size: 0.82rem;
        }

        .account-nav {
            margin-left: auto;
        }

    }
</style>
