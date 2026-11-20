<script lang="ts">
	import LegalPage from '$lib/components/LegalPage.svelte';

	const sections = [
		{ id: 'who', title: 'Quiénes somos', content: who },
		{ id: 'collect', title: 'Qué datos recogemos', content: collect },
		{ id: 'why', title: 'Para qué los usamos', content: why },
		{ id: 'services', title: 'Servicios que utilizamos', content: services },
		{ id: 'cookies', title: 'Cookies y almacenamiento', content: cookies },
		{ id: 'keep', title: 'Cuánto tiempo los guardamos', content: keep },
		{ id: 'rights', title: 'Tus derechos', content: rights },
		{ id: 'contact', title: 'Contacto', content: contact }
	];
</script>

<LegalPage
	title="Política de privacidad"
	updated="2026-11-17"
	{sections}
	other={{ href: '/terms', label: 'Términos del servicio' }}
/>

{#snippet who()}
	<p>
		Transcendence es un proyecto estudiantil creado por cinco estudiantes de 42 Lyon. No es un
		servicio comercial. Para cualquier pregunta sobre tus datos, usa el contacto al final de esta
		página.
	</p>
{/snippet}

{#snippet collect()}
	<ul>
		<li><strong>Cuenta:</strong> tu correo electrónico, tu nombre de jugador y tu contraseña, que solo guardamos como hash Argon2id.</li>
		<li><strong>Perfil:</strong> la clase y el color de tu personaje, y el avatar que subes.</li>
		<li><strong>Juego:</strong> tus partidas (modo, puntuación, oleada, enemigos eliminados, duración y resultado), tu oro y tus mejoras, y tus ajustes y controles del juego.</li>
		<li><strong>Amigos:</strong> tus solicitudes de amistad, tus amigos, los jugadores que bloqueas y tu última conexión.</li>
	</ul>
{/snippet}

{#snippet why()}
	<p>
		Para que tu cuenta funcione, mostrar tu perfil a otros jugadores, sincronizar tu progreso
		entre el sitio y el juego, mostrar a tus amigos cuándo estás en línea y mantener el sitio
		seguro. No vendemos tus datos, no los compartimos con fines publicitarios y no mostramos
		anuncios.
	</p>
{/snippet}

{#snippet services()}
	<p>
		El juego está hecho con Unity. Para jugar en línea, usa Unity Gaming Services (Relay y
		Friends), que reciben un identificador de jugador de Unity vinculado a tu cuenta, tu nombre de
		jugador y datos de conexión. Unity trata estos datos según su propia política de privacidad.
	</p>
{/snippet}

{#snippet cookies()}
	<p>Usamos dos cookies:</p>
	<ul>
		<li>
			<strong>Sesión:</strong> mantiene tu sesión iniciada. Los scripts no pueden leerla, dura 7
			días y se borra al cerrar sesión.
		</li>
		<li>
			<strong>Idioma</strong> (<code>PARAGLIDE_LOCALE</code>): recuerda el idioma elegido en el
			selector durante 400 días.
		</li>
	</ul>
	<p>
		No usamos cookies de seguimiento ni de analítica. El juego también guarda sus archivos y
		ajustes en el almacenamiento de tu navegador para cargar más rápido.
	</p>
{/snippet}

{#snippet keep()}
	<p>
		Guardamos tus datos mientras exista tu cuenta. Las sesiones caducan a los 7 días.
	</p>
{/snippet}

{#snippet rights()}
	<p>
		Puedes ver y cambiar tu perfil en <a href="/settings">Ajustes</a>. También puedes pedirnos una
		copia de tus datos, corregirlos o eliminar tu cuenta, usando el contacto de abajo.
	</p>
{/snippet}

{#snippet contact()}
	<p>
		Encuentra el proyecto en GitHub:
		<a href="https://github.com/Transcendence-wejdz">github.com/Transcendence-wejdz</a>.
	</p>
{/snippet}
