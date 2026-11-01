<script lang="ts">
	import { toast } from 'svelte-sonner';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Label from '$lib/components/ui/Label.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import Tag from '$lib/components/ui/Tag.svelte';
	import Ribbon from '$lib/components/ui/Ribbon.svelte';
	import Avatar, { type Team } from '$lib/components/ui/Avatar.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';

	const teams: Team[] = ['black', 'blue', 'purple', 'red', 'yellow'];

	let open = $state(false);
	let name = $state('');
	let taken = $state('Kael');
</script>

<h1>Styleguide</h1>
<p class="intro">Every shared component, in each of its states. Only available with <code>npm run dev</code>.</p>

<section>
	<h2>Button</h2>
	<div class="row">
		<Button>Primary</Button>
		<Button variant="secondary">Secondary</Button>
		<Button variant="danger">Danger</Button>
		<Button variant="42">Continue with 42</Button>
	</div>
	<div class="row">
		<Button size="sm">Small</Button>
		<Button href="/">Link</Button>
		<Button disabled>Disabled</Button>
		<Button busy>Busy</Button>
	</div>
</section>

<section>
	<h2>Input + Label</h2>
	<div class="fields">
		<div class="field">
			<Label for="name">Display name</Label>
			<Input id="name" bind:value={name} placeholder="Your name" />
		</div>
		<div class="field">
			<Label for="taken">Display name (error)</Label>
			<Input id="taken" bind:value={taken} error="This name is already taken" />
		</div>
	</div>
</section>

<section>
	<h2>Card</h2>
	<div class="row">
		<Card variant="parchment">Parchment card</Card>
		<Card>Board card</Card>
	</div>
</section>

<section>
	<h2>Tag</h2>
	<div class="row">
		<Tag>Warrior</Tag>
		<Tag dot="var(--color-online)">Online</Tag>
		<Tag dot="var(--color-team-red)">Red team</Tag>
	</div>
</section>

<section>
	<h2>Ribbon</h2>
	<div class="row">
		<Ribbon>Best score</Ribbon>
	</div>
</section>

<section>
	<h2>Avatar</h2>
	<div class="row">
		<Avatar name="Kael" size="sm" />
		<Avatar name="Kael" size="md" />
		<Avatar name="Kael" size="lg" />
		<Avatar name="Kael" src="/favicon.svg" />
	</div>
	<div class="row">
		{#each teams as team (team)}
			<Avatar name={team} {team} />
		{/each}
	</div>
</section>

<section>
	<h2>Modal</h2>
	<div class="row">
		<Button variant="danger" onclick={() => (open = true)}>Delete my account</Button>
	</div>
	<Modal
		bind:open
		title="Delete your account?"
		description="Your profile, friends, messages, achievements and level will be removed for good. This can't be undone."
		confirmLabel="Yes, delete my account"
		onConfirm={async () => {
			await new Promise((resolve) => setTimeout(resolve, 2000));
			toast.success('Account deleted', { description: 'Just kidding, this is the styleguide.' });
			}}
	/>
</section>

<section>
	<h2>Toasts</h2>
	<div class="row">
		<Button onclick={() => toast.success('Settings saved', { description: 'Your profile is up to date.' })}>Success</Button>
		<Button variant="danger" onclick={() => toast.error('Message not sent', { description: 'Check your connection.' })}>Error</Button>
	</div>
</section>

<style>
	h1 {
		margin: 0 0 var(--space-xs);
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 3rem;
	}

	h2 {
		margin: 0 0 var(--space-md);
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 2rem;
	}

	.intro {
		margin: 0 0 2.5rem;
		color: var(--color-text-muted);
	}

	section {
		margin-bottom: 2.5rem;
	}

	.row {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-md);
	}

	.row + .row {
		margin-top: var(--space-md);
	}

	.fields {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr));
		gap: var(--space-lg);
	}

	.field {
		display: grid;
		gap: var(--space-xs);
	}
</style>
