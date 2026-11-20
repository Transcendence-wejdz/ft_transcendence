<script lang="ts">
	import LegalPage from '$lib/components/LegalPage.svelte';

	const sections = [
		{ id: 'who', title: 'Qui sommes-nous', content: who },
		{ id: 'collect', title: 'Ce que nous collectons', content: collect },
		{ id: 'why', title: 'À quoi cela sert', content: why },
		{ id: 'services', title: 'Les services que nous utilisons', content: services },
		{ id: 'cookies', title: 'Cookies et stockage', content: cookies },
		{ id: 'keep', title: 'Combien de temps nous les gardons', content: keep },
		{ id: 'rights', title: 'Vos droits', content: rights },
		{ id: 'contact', title: 'Contact', content: contact }
	];
</script>

<LegalPage
	title="Politique de confidentialité"
	updated="2026-11-17"
	{sections}
	other={{ href: '/terms', label: "Conditions d'utilisation" }}
/>

{#snippet who()}
	<p>
		Transcendence est un projet étudiant réalisé par cinq étudiants de 42 Lyon. Ce n'est pas un
		service commercial. Pour toute question sur vos données, utilisez le contact en bas de cette
		page.
	</p>
{/snippet}

{#snippet collect()}
	<ul>
		<li><strong>Compte :</strong> votre adresse e-mail, votre pseudo et votre mot de passe, que nous ne conservons que sous forme de hachage Argon2id.</li>
		<li><strong>Profil :</strong> la classe et la couleur de votre personnage, et l'avatar que vous envoyez.</li>
		<li><strong>Jeu :</strong> vos parties (mode, score, vague, ennemis tués, durée et résultat), votre or et vos améliorations, ainsi que vos réglages et vos touches dans le jeu.</li>
		<li><strong>Amis :</strong> vos demandes d'ami, vos amis, les joueurs que vous bloquez, et votre dernière connexion.</li>
	</ul>
{/snippet}

{#snippet why()}
	<p>
		Pour faire fonctionner votre compte, montrer votre profil aux autres joueurs, synchroniser
		votre progression entre le site et le jeu, indiquer à vos amis quand vous êtes en ligne, et
		protéger le site. Nous ne vendons pas vos données, ne les partageons pas à des fins
		publicitaires et n'affichons aucune publicité.
	</p>
{/snippet}

{#snippet services()}
	<p>
		Le jeu est développé avec Unity. Pour jouer en ligne, il utilise Unity Gaming Services (Relay
		et Friends), qui reçoivent un identifiant de joueur Unity lié à votre compte, votre pseudo et
		des données de connexion. Unity traite ces données selon sa propre politique de
		confidentialité.
	</p>
{/snippet}

{#snippet cookies()}
	<p>Nous utilisons deux cookies :</p>
	<ul>
		<li>
			<strong>Session :</strong> vous garde connecté. Il ne peut pas être lu par les scripts,
			dure 7 jours et est supprimé à la déconnexion.
		</li>
		<li>
			<strong>Langue</strong> (<code>PARAGLIDE_LOCALE</code>) : retient la langue choisie dans le
			sélecteur, pendant 400 jours.
		</li>
	</ul>
	<p>
		Nous n'utilisons aucun cookie de suivi ou de mesure d'audience. Le jeu garde aussi ses
		fichiers et ses réglages dans le stockage de votre navigateur, pour se charger plus vite.
	</p>
{/snippet}

{#snippet keep()}
	<p>
		Nous gardons vos données tant que votre compte existe. Les sessions de connexion expirent
		au bout de 7 jours.
	</p>
{/snippet}

{#snippet rights()}
	<p>
		Vous pouvez consulter et modifier votre profil dans les <a href="/settings">Paramètres</a>.
		Vous pouvez aussi nous demander une copie de vos données, leur correction ou la suppression
		de votre compte, en utilisant le contact ci-dessous.
	</p>
{/snippet}

{#snippet contact()}
	<p>
		Retrouvez le projet sur GitHub :
		<a href="https://github.com/Transcendence-wejdz">github.com/Transcendence-wejdz</a>.
	</p>
{/snippet}
