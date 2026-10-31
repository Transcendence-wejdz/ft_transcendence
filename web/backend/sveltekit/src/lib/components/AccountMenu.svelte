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
