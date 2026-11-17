<script lang="ts">
	import LegalPage from '$lib/components/LegalPage.svelte';

	const sections = [
		{ id: 'service', title: 'Le service', content: service },
		{ id: 'account', title: 'Votre compte', content: account },
		{ id: 'play', title: 'Fair-play', content: play },
		{ id: 'content', title: 'Ce que vous partagez', content: shared },
		{ id: 'end', title: "Fermeture d'un compte", content: end },
		{ id: 'changes', title: 'Modifications', content: changes },
		{ id: 'contact', title: 'Contact', content: contact }
	];
</script>

<LegalPage
	title="Conditions d'utilisation"
	updated="2026-11-09"
	{sections}
	other={{ href: '/privacy', label: 'Politique de confidentialité' }}
/>

{#snippet service()}
	<p>
		Transcendence est un projet étudiant gratuit de 42 Lyon. Il est fourni tel quel, sans aucune
		garantie : il peut être indisponible par moments, perdre des données, et s'arrêter à la fin
		du projet.
	</p>
{/snippet}

{#snippet account()}
	<ul>
		<li>Un compte par personne. Gardez votre mot de passe pour vous.</li>
		<li>Votre pseudo ne doit pas usurper l'identité de quelqu'un ni être offensant.</li>
		<li>Vous êtes responsable de ce qui se passe sur votre compte.</li>
	</ul>
{/snippet}

{#snippet play()}
	<ul>
		<li>Pas de triche, de bots, ni d'exploitation volontaire de bugs. Si vous trouvez un bug, signalez-le-nous.</li>
		<li>Pas de harcèlement, de haine ni de spam, que ce soit dans le jeu, dans votre pseudo ou dans votre avatar.</li>
	</ul>
{/snippet}

{#snippet shared()}
	<p>
		Vous gardez les droits sur l'avatar que vous envoyez. Vous nous autorisez à le conserver et à
		le montrer aux autres joueurs pour que le site fonctionne. Nous pouvons retirer un pseudo ou
		un avatar qui ne respecte pas ces règles.
	</p>
{/snippet}

{#snippet end()}
	<p>
		Vous pouvez nous demander la suppression de votre compte à tout moment, en utilisant le
		contact ci-dessous. Nous pouvons suspendre ou supprimer les comptes qui ne respectent pas ces
		règles.
	</p>
{/snippet}

{#snippet changes()}
	<p>
		Si ces conditions changent, nous mettons à jour la date en haut de cette page. Continuer à
		utiliser le site après une modification vaut acceptation des nouvelles conditions.
	</p>
{/snippet}

{#snippet contact()}
	<p>
		Retrouvez le projet sur GitHub :
		<a href="https://github.com/Transcendence-wejdz">github.com/Transcendence-wejdz</a>.
	</p>
{/snippet}
