<script lang="ts">
    import { onMount } from 'svelte';

    type Friend = {
        id: string;
        displayName: string;
        color: string;
        online: boolean;
        lastSeen: string | null;
        relationshipId: string;
    };

    type FriendRequest = {
        id: string;
        displayName: string;
        color: string;
        relationshipId: string;
    };

    type BlockedPlayer = {
        id: string;
        displayName: string;
    };

    let search = $state('');
    let identifier = $state('');
    let error = $state('');
    let loading = $state(false);

    let friends = $state<Friend[]>([]);
    let requests = $state<FriendRequest[]>([]);
    let blockedPlayers = $state<BlockedPlayer[]>([]);

    function formatLastSeen(value: string | null) {
        if (!value) return 'Last seen: unknown';
        const elapsed = Math.max(0, Date.now() - new Date(value).getTime());
        const minutes = Math.floor(elapsed / 60_000);
        if (minutes < 1) return 'Last seen just now';
        if (minutes < 60) return `Last seen ${minutes}m ago`;
        const hours = Math.floor(minutes / 60);
        if (hours < 24) return `Last seen ${hours}h ago`;
        const days = Math.floor(hours / 24);
        if (days < 7) return `Last seen ${days}d ago`;
        return `Last seen ${new Date(value).toLocaleDateString()}`;
    }

    async function loadFriends() {
        const response = await fetch('/api/friends-list');
        const result = await response.json();
        if (!response.ok) throw new Error(result.error ?? 'Could not load friends.');
        friends = result.friends;
        requests = result.requests;
    }

    async function loadBlockedPlayers() {
        const response = await fetch('/api/friends-list/blocked');
        const result = await response.json();
        if (!response.ok) throw new Error(result.error ?? 'Could not load blocked players.');
        blockedPlayers = result.blockedPlayers;
    }
    const PRESENCE_REFRESH_INTERVAL_MS = 5_000;
    let intervalId: ReturnType<typeof setInterval> | undefined;

    async function refreshFriends() {
        try {
            await loadFriends();
        } catch (cause) {
            error = cause instanceof Error
                ? cause.message
                : 'Could not refresh friends.';
        }
    }

    onMount(() => {
        loading = true;
        Promise.all([loadFriends(), loadBlockedPlayers()])
            .catch((cause: unknown) => { error = cause instanceof Error ? cause.message : 'Could not load friends.'; })
            .finally(() => { loading = false; });

        intervalId = setInterval(() => {
            void refreshFriends();
        }, PRESENCE_REFRESH_INTERVAL_MS);

        return () => {
            if (intervalId !== undefined) clearInterval(intervalId);
        };
    });

    async function updateRelationship(relationshipId: string, method: 'PATCH' | 'DELETE', body?: object) {
        const response = await fetch(`/api/friends-list/relationships/${relationshipId}`, {
            method,
            ...(body ? { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) } : {})
        });
        const result = await response.json();
        if (!response.ok) throw new Error(result.error ?? 'Could not update relationship.');
        await loadFriends();
    }

    async function handleRelationship(action: 'accept' | 'decline' | 'remove', relationshipId: string) {
        error = '';
        loading = true;
        try {
            await updateRelationship(relationshipId, action === 'accept' ? 'PATCH' : 'DELETE', action === 'accept' ? { action } : undefined);
        } catch (cause) {
            error = cause instanceof Error ? cause.message : 'Could not update relationship.';
        } finally {
            loading = false;
        }
    }

    async function handleBlock(playerId: string) {
        error = '';
        loading = true;
        try {
            const response = await fetch(`/api/friends-list/players/${playerId}/block`, {
                method: 'POST'
            });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error ?? 'Could not block player.');
            await Promise.all([loadFriends(), loadBlockedPlayers()]);
        } catch (cause) {
            error = cause instanceof Error ? cause.message : 'Could not block player.';
        } finally {
            loading = false;
        }
    }

    async function handleUnblock(playerId: string) {
        error = '';
        loading = true;
        try {
            const response = await fetch(`/api/friends-list/players/${playerId}/block`, {
                method: 'DELETE'
            });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error ?? 'Could not unblock player.');
            await loadBlockedPlayers();
        } catch (cause) {
            error = cause instanceof Error ? cause.message : 'Could not unblock player.';
        } finally {
            loading = false;
        }
    }

    const filteredFriends = $derived(
        friends.filter((friend) =>
            friend.displayName.toLowerCase().includes(search.toLowerCase())
        )
    );

    async function sendRequest(event: SubmitEvent) {
        event.preventDefault();
        error = '';

        const name = identifier.trim();

        if (!name) {
            error = 'Enter a display name.';
            return;
        }

        loading = true;
        try {
            const response = await fetch('/api/friends-list', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ displayName: name })
            });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error ?? 'Could not send request.');
            identifier = '';
            await loadFriends();
        } catch (cause) {
            error = cause instanceof Error ? cause.message : 'Could not send request.';
        } finally {
            loading = false;
        }
    }
</script>

<svelte:head>
    <title>Friends</title>
    <meta
        name="description"
        content="Manage your friends and friendship requests."
    />
</svelte:head>

<section class="friends-page">
    <header class="page-header">
        <p class="eyebrow">YOUR COMMUNITY</p>
        <h1>Friends</h1>
        <p class="description">
            Find your allies, manage requests, and get ready for your next match.
        </p>
    </header>

    {#if error}
        <p class="error" role="alert">{error}</p>
    {/if}

    <section class="panel">
        <div class="section-heading">
            <div>
                <h2>Your friends</h2>
                <p class="section-description">
                    {friends.length} {friends.length === 1 ? 'friend' : 'friends'}
                </p>
            </div>
        </div>

        <label for="friend-search">Search friends</label>
        <input
            id="friend-search"
            type="search"
            bind:value={search}
            placeholder="Search by display name..."
        />

        {#if filteredFriends.length > 0}
            <ul class="friend-list">
                {#each filteredFriends as friend (friend.id)}
                    <li class="friend-item">
                        <div
                            class="avatar"
                            style={`--avatar-color: ${friend.color}`}
                            aria-hidden="true"
                        >
                            {friend.displayName.charAt(0).toUpperCase()}
                        </div>

                        <div class="friend-info">
                            <span class="friend-name">{friend.displayName}</span>
                            <span class:online={friend.online} class="friend-status">
                                <span class="status-dot"></span>
                                {friend.online ? 'Online' : formatLastSeen(friend.lastSeen)}
                            </span>
                        </div>

                        <button
                            class="button secondary"
                            type="button"
                            disabled={loading}
                            onclick={() => {
                                void handleRelationship('remove', friend.relationshipId);
                            }}
                        >
                            Remove
                        </button>
                        <button
                            class="button secondary"
                            type="button"
                            disabled={loading}
                            onclick={() => void handleBlock(friend.id)}
                        >
                            Block
                        </button>
                    </li>
                {/each}
            </ul>
        {:else}
            <p class="empty-state">
                {search ? 'No friends match your search.' : 'Your friend list is empty.'}
            </p>
        {/if}
    </section>

    <section class="panel">
        <div class="section-heading">
            <div>
                <h2>Blocked players</h2>
                <p class="section-description">
                    {blockedPlayers.length} {blockedPlayers.length === 1 ? 'player' : 'players'} blocked
                </p>
            </div>
        </div>

        {#if blockedPlayers.length > 0}
            <ul class="friend-list">
                {#each blockedPlayers as player (player.id)}
                    <li class="friend-item">
                        <div class="friend-info">
                            <span class="friend-name">{player.displayName}</span>
                            <span class="friend-status">Blocked by you</span>
                        </div>
                        <button
                            class="button secondary"
                            type="button"
                            disabled={loading}
                            onclick={() => void handleUnblock(player.id)}
                        >
                            Unblock
                        </button>
                    </li>
                {/each}
            </ul>
        {:else}
            <p class="empty-state">You have not blocked anyone.</p>
        {/if}
    </section>

    <section class="panel">
        <div class="section-heading">
            <div>
                <h2>Friend requests</h2>
                <p class="section-description">
                    {requests.length} pending
                </p>
            </div>
        </div>

        {#if requests.length > 0}
            <ul class="friend-list">
                {#each requests as request (request.relationshipId)}
                    <li class="friend-item">
                        <div
                            class="avatar"
                            style={`--avatar-color: ${request.color}`}
                            aria-hidden="true"
                        >
                            {request.displayName.charAt(0).toUpperCase()}
                        </div>

                        <div class="friend-info">
                            <span class="friend-name">{request.displayName}</span>
                            <span class="friend-status">Wants to be your friend</span>
                        </div>

                        <div class="request-actions">
                            <button
                                class="button"
                                type="button"
                                disabled={loading}
                                onclick={() => void handleRelationship('accept', request.relationshipId)}
                            >
                                Accept
                            </button>
                            <button
                                class="button secondary"
                                type="button"
                                disabled={loading}
                                onclick={() => void handleRelationship('decline', request.relationshipId)}
                            >
                                Decline
                            </button>
                            <button
                                class="button secondary"
                                type="button"
                                disabled={loading}
                                onclick={() => void handleBlock(request.id)}
                            >
                                Block
                            </button>
                        </div>
                    </li>
                {/each}
            </ul>
        {:else}
            <p class="empty-state">No pending friend requests.</p>
        {/if}
    </section>

    <section class="panel">
        <h2>Add a friend</h2>
        <p class="section-description">
            Enter the display name of the player you want to add.
        </p>

        <form onsubmit={sendRequest}>
            <label for="friend-identifier">Display name</label>
            <input
                id="friend-identifier"
                type="text"
                bind:value={identifier}
                placeholder="Enter a player's name"
                autocomplete="off"
                required
            />

            <button class="button" type="submit" disabled={loading}>
                {loading ? 'Sending request...' : 'Send friend request'}
            </button>
        </form>
    </section>
</section>

<style>
    .friends-page {
        width: min(100% - 2rem, 760px);
        margin: 2rem auto 4rem;
    }

    .page-header {
        margin-bottom: 2rem;
    }

    .eyebrow {
        margin: 0 0 0.65rem;
        color: #f5b85c;
        font-size: 0.72rem;
        font-weight: 800;
        letter-spacing: 0.15em;
    }

    h1 {
        margin: 0;
        font-size: clamp(2.2rem, 8vw, 3rem);
        letter-spacing: -0.05em;
    }

    h2 {
        margin: 0;
        font-size: 1.2rem;
    }

    .description,
    .section-description {
        color: #aeb8d3;
        line-height: 1.6;
    }

    .description {
        margin: 0.55rem 0 0;
    }

    .section-description {
        margin: 0.25rem 0 0;
        font-size: 0.88rem;
    }

    .panel {
        margin-bottom: 1.25rem;
        padding: clamp(1.1rem, 4vw, 1.6rem);
        border: 1px solid rgba(189, 204, 242, 0.13);
        border-radius: 1.25rem;
        background: linear-gradient(
            145deg,
            rgba(29, 37, 65, 0.92),
            rgba(19, 24, 43, 0.92)
        );
        box-shadow: 0 24px 60px rgba(0, 0, 0, 0.15);
    }

    .section-heading {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1.25rem;
    }

    label {
        display: block;
        margin: 0.5rem 0;
        color: #d6def3;
        font-size: 0.88rem;
        font-weight: 650;
    }

    input {
        box-sizing: border-box;
        width: 100%;
        min-height: 3rem;
        padding: 0.8rem 0.9rem;
        border: 1px solid rgba(189, 204, 242, 0.18);
        border-radius: 0.65rem;
        outline: none;
        color: #f3f5ff;
        background: rgba(9, 13, 27, 0.55);
    }

    input:focus {
        border-color: #f5b85c;
        box-shadow: 0 0 0 3px rgba(245, 184, 92, 0.13);
    }

    input::placeholder {
        color: #737f9c;
    }

    .friend-list {
        display: grid;
        gap: 0.75rem;
        margin: 1rem 0 0;
        padding: 0;
        list-style: none;
    }

    .friend-item {
        display: flex;
        align-items: center;
        gap: 0.85rem;
        min-width: 0;
        padding: 0.85rem;
        border: 1px solid rgba(189, 204, 242, 0.09);
        border-radius: 0.85rem;
        background: rgba(9, 13, 27, 0.3);
    }

    .avatar {
        display: grid;
        flex: 0 0 2.8rem;
        width: 2.8rem;
        height: 2.8rem;
        place-items: center;
        border: 1px solid var(--avatar-color);
        border-radius: 50%;
        color: var(--avatar-color);
        background: color-mix(in srgb, var(--avatar-color) 15%, transparent);
        font-weight: 800;
    }

    .friend-info {
        display: grid;
        flex: 1;
        gap: 0.3rem;
        min-width: 0;
    }

    .friend-name {
        overflow-wrap: anywhere;
        color: #f3f5ff;
        font-weight: 650;
    }

    .friend-status {
        display: flex;
        align-items: center;
        gap: 0.4rem;
        color: #8994b0;
        font-size: 0.8rem;
    }

    .friend-status.online {
        color: #91d7a5;
    }

    .status-dot {
        width: 0.45rem;
        height: 0.45rem;
        border-radius: 50%;
        background: currentColor;
    }

    .request-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
    }

    .button {
        min-height: 2.5rem;
        padding: 0.6rem 0.9rem;
        border: 1px solid #f5b85c;
        border-radius: 0.65rem;
        color: #171b2a;
        background: #f5b85c;
        font-weight: 700;
        cursor: pointer;
    }

    .button.secondary {
        border-color: rgba(189, 204, 242, 0.2);
        color: #d6def3;
        background: rgba(9, 13, 27, 0.4);
    }

    .button:hover:not(:disabled) {
        filter: brightness(1.08);
    }

    .button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    form {
        display: grid;
        gap: 0.7rem;
        margin-top: 1rem;
    }

    form label {
        margin-bottom: 0;
    }

    .empty-state {
        margin: 1rem 0 0;
        padding: 1rem;
        border: 1px dashed rgba(189, 204, 242, 0.16);
        border-radius: 0.75rem;
        color: #aeb8d3;
        text-align: center;
    }

    .error {
        margin: 0.25rem 0;
        color: #ff9eaa;
        font-size: 0.9rem;
    }

    @media (max-width: 520px) {
        .friend-item {
            flex-wrap: wrap;
        }

        .request-actions {
            width: 100%;
            padding-left: 3.65rem;
        }
    }
</style>
