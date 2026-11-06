<script lang="ts">
	import LegalPage from '$lib/components/LegalPage.svelte';

	const sections = [
		{ id: 'who', title: 'Who we are', content: who },
		{ id: 'collect', title: 'What we collect', content: collect },
		{ id: 'why', title: 'Why we use it', content: why },
		{ id: 'services', title: 'Services we rely on', content: services },
		{ id: 'cookies', title: 'Cookies and storage', content: cookies },
		{ id: 'keep', title: 'How long we keep it', content: keep },
		{ id: 'rights', title: 'Your rights', content: rights },
		{ id: 'contact', title: 'Contact', content: contact }
	];
</script>

<LegalPage
	title="Privacy Policy"
	updated="November 4, 2026"
	{sections}
	other={{ href: '/terms', label: 'Terms of Service' }}
/>

{#snippet who()}
	<p>
		Transcendence is a student project made by five students at 42 Lyon. It is not a commercial
		service. For any question about your data, use the contact at the end of this page.
	</p>
{/snippet}

{#snippet collect()}
	<ul>
		<li><strong>Account:</strong> your email, your display name and your password, which we only store as an Argon2id hash.</li>
		<li><strong>Profile:</strong> your character class and color, and the avatar picture you upload.</li>
		<li><strong>Game:</strong> your runs (mode, score, wave, kills, duration and result), your gold and upgrades, and your in-game settings and key bindings.</li>
		<li><strong>Friends:</strong> your friend requests, your friends, the players you block, and when you were last online.</li>
	</ul>
{/snippet}

{#snippet why()}
	<p>
		To run your account, show your profile to other players, keep your progress in sync between
		the site and the game, show your friends when you are online, and keep the site secure. We do
		not sell your data, share it for advertising or show ads.
	</p>
{/snippet}

{#snippet services()}
	<p>
		The game is made with Unity. To play online, it uses Unity Gaming Services (Relay and Friends),
		which receive a Unity player ID linked to your account, your display name and connection data.
		Unity handles this data under its own privacy policy.
	</p>
{/snippet}

{#snippet cookies()}
	<p>
		We use a single cookie to keep you logged in. It cannot be read by
		scripts, lasts 7 days and is removed when you log out. We use no tracking or analytics
		cookies. The game also keeps its files and settings in your browser's storage so it loads
		faster.
	</p>
{/snippet}

{#snippet keep()}
	<p>
		We keep your data as long as your account exists. Login sessions expire after 7 days.
	</p>
{/snippet}

{#snippet rights()}
	<p>
		You can see and change your profile in <a href="/settings">Settings</a>. You can also ask us
		for a copy of your data, to correct it, or to delete your account, using the contact below.
	</p>
{/snippet}

{#snippet contact()}
	<p>
		Reach the project owner on GitHub:
		<a href="https://github.com/SaruM4N3">github.com/SaruM4N3</a>.
	</p>
{/snippet}
