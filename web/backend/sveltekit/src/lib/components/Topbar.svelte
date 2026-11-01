<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';
	import AccountMenu from '$lib/components/AccountMenu.svelte';

	type Props = {
		user: App.Locals['user'];
	};

	let { user }: Props = $props();
</script>

<header class="topbar">
	<a class="brand" href="/" aria-label="Transcendence home">
		<span class="brand-mark">T</span>
		<span class="brand-name">Transcendence</span>
	</a>

	<nav class="nav" aria-label="Main navigation">
		<a href="/game" data-sveltekit-reload>Play</a>
		{#if user}
			<a href="/friends-list">Friends</a>
		{/if}
	</nav>

	<div class="actions">
		{#if user}
			<AccountMenu {user} />
		{:else}
			<a class="login" href="/login">Log in</a>
			<Button href="/register" size="sm">Sign up</Button>
		{/if}
	</div>
</header>

<style>
	.topbar {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-sm) 1.25rem;
		padding: 0.75rem clamp(1rem, 4vw, 3.5rem);
		background: var(--color-bg-bar);
		border-bottom: var(--border-width) solid var(--color-drop);
	}

	.brand {
		display: inline-flex;
		align-items: center;
		gap: 0.75rem;
		color: var(--color-text);
		text-decoration: none;
	}

	.brand-mark {
		display: grid;
		place-items: center;
		width: 2.875rem;
		height: 2.875rem;
		background: var(--color-gold);
		color: var(--color-parchment-text);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		box-shadow: inset 0 -4px 0 var(--color-gold-shadow);
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: 1.375rem;
	}

	.brand-name {
		font-family: var(--font-title);
		font-size: 1.75rem;
		letter-spacing: 0.02em;
	}

	.nav {
		display: flex;
		flex-wrap: wrap;
		gap: 0.25rem;
		margin-left: auto;
	}

	.nav a, .login {
		display: inline-flex;
		align-items: center;
		min-height: var(--control-sm);
		padding: 0 0.75rem;
		border-radius: var(--radius);
		font-family: var(--font-ui);
		font-size: 1.0625rem;
		color: var(--color-text-secondary);
		text-decoration: none;
	}

	.nav a:hover, .login:hover {
		color: var(--color-link-hover);
	}

	.actions {
		display: flex;
		align-items: center;
		gap: var(--space-sm);
	}
</style>
