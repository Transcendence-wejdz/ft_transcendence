<script lang="ts">
	import LegalPage from '$lib/components/LegalPage.svelte';

	const sections = [
		{ id: 'service', title: 'The service', content: service },
		{ id: 'account', title: 'Your account', content: account },
		{ id: 'play', title: 'Fair play', content: play },
		{ id: 'content', title: 'What you share', content: shared },
		{ id: 'end', title: 'Closing an account', content: end },
		{ id: 'changes', title: 'Changes', content: changes },
		{ id: 'contact', title: 'Contact', content: contact }
	];
</script>

<LegalPage
	title="Terms of Service"
	updated="November 9, 2026"
	{sections}
	other={{ href: '/privacy', label: 'Privacy Policy' }}
/>

{#snippet service()}
	<p>
		Transcendence is a free student project from 42 Lyon. It is provided as is, without any
		guarantee: it may be offline at times, lose data, and stop when the project ends.
	</p>
{/snippet}

{#snippet account()}
	<ul>
		<li>One account per person. Keep your password private.</li>
		<li>Your display name must not impersonate someone else or be offensive.</li>
		<li>You are responsible for what happens under your account.</li>
	</ul>
{/snippet}

{#snippet play()}
	<ul>
		<li>No cheating, bots, or exploiting bugs on purpose. If you find a bug, tell us.</li>
		<li>No harassment, hate or spam, in the game or in your display name and avatar.</li>
	</ul>
{/snippet}

{#snippet shared()}
	<p>
		You keep the rights to the avatar you upload. You let us store it and show it to other
		players so the site works. We may remove a display name or avatar that breaks these rules.
	</p>
{/snippet}

{#snippet end()}
	<p>
		You can ask us to delete your account at any time, using the contact below. We may suspend
		or delete accounts that break these rules.
	</p>
{/snippet}

{#snippet changes()}
	<p>
		If these terms change, we update the date at the top of this page. Using the site after a
		change means you accept the new terms.
	</p>
{/snippet}

{#snippet contact()}
	<p>
		Reach the project on GitHub:
		<a href="https://github.com/Transcendence-wejdz">github.com/Transcendence-wejdz</a>.
	</p>
{/snippet}
