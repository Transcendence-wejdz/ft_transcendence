<script lang="ts">
	import { DropdownMenu } from 'bits-ui';
	import Avatar, { type Team } from '$lib/components/ui/Avatar.svelte';

	type Props = {
		user: NonNullable<App.Locals['user']>;
	};

	let { user }: Props = $props();
	const team = $derived(user.color.toLowerCase() as Team);

	async function logout() {
		const response = await fetch('/api/logout', {
			method: 'POST'
		});

		if (response.ok) {
			window.location.assign('/login');
		}
	}
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger class="account-trigger">
		<Avatar size="sm" name={user.display_name} {team} src={user.avatar_url ?? undefined} />
		<span>{user.display_name}</span>
		<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="square" aria-hidden="true"><path d="M6 9l6 6 6-6" /></svg>
	</DropdownMenu.Trigger>
	<DropdownMenu.Portal>
		<DropdownMenu.Content class="account-menu" align="end" sideOffset={8}>
			<DropdownMenu.Item class="account-item">
				{#snippet child({ props })}
					<a href="/profile/{user.display_name}" {...props}>Profile</a>
				{/snippet}
			</DropdownMenu.Item>
			<DropdownMenu.Item class="account-item">
				{#snippet child({ props })}
					<a href="/settings" {...props}>Settings</a>
				{/snippet}
			</DropdownMenu.Item>
			<DropdownMenu.Separator class="account-separator" />
			<DropdownMenu.Item class="account-item" onSelect={logout}>Log out</DropdownMenu.Item>
		</DropdownMenu.Content>
	</DropdownMenu.Portal>
</DropdownMenu.Root>

<style>
	:global(.account-trigger) {
		display: inline-flex;
		align-items: center;
		gap: var(--space-xs);
		height: var(--control-sm);
		padding: 0 0.625rem 0 0.25rem;
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		background: var(--color-bg-raised);
		color: var(--color-text);
		box-shadow: inset 0 -4px 0 var(--color-bg-raised-shadow);
		font-family: var(--font-ui);
		font-size: var(--text-md);
		cursor: pointer;
	}

	:global(.account-trigger:hover) {
		background: var(--color-bg-raised-hover);
	}

	:global(.account-trigger:focus-visible) {
		outline: 3px solid var(--color-focus);
		outline-offset: 3px;
	}

	:global(.account-menu) {
		min-width: 11rem;
		padding: var(--space-xs);
		background: var(--color-bg-card);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		box-shadow: var(--shadow-drop);
		z-index: 50;
	}

	:global(.account-item) {
		display: flex;
		align-items: center;
		min-height: var(--control-sm);
		padding: 0 0.75rem;
		border-radius: var(--radius);
		font-family: var(--font-ui);
		font-size: var(--text-md);
		color: var(--color-text);
		text-decoration: none;
		cursor: pointer;
		outline: none;
	}

	:global(.account-item[data-highlighted]) {
		background: var(--color-bg-raised);
	}

	:global(.account-separator) {
		height: 2px;
		margin: var(--space-xs) 0;
		background: var(--color-border-subtle);
	}
</style>
