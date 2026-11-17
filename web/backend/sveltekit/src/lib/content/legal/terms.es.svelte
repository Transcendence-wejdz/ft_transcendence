<script lang="ts">
	import LegalPage from '$lib/components/LegalPage.svelte';

	const sections = [
		{ id: 'service', title: 'El servicio', content: service },
		{ id: 'account', title: 'Tu cuenta', content: account },
		{ id: 'play', title: 'Juego limpio', content: play },
		{ id: 'content', title: 'Lo que compartes', content: shared },
		{ id: 'end', title: 'Cierre de cuenta', content: end },
		{ id: 'changes', title: 'Cambios', content: changes },
		{ id: 'contact', title: 'Contacto', content: contact }
	];
</script>

<LegalPage
	title="Términos del servicio"
	updated="2026-11-09"
	{sections}
	other={{ href: '/privacy', label: 'Política de privacidad' }}
/>

{#snippet service()}
	<p>
		Transcendence es un proyecto estudiantil gratuito de 42 Lyon. Se ofrece tal cual, sin ninguna
		garantía: puede no estar disponible a veces, perder datos y dejar de funcionar cuando termine
		el proyecto.
	</p>
{/snippet}

{#snippet account()}
	<ul>
		<li>Una cuenta por persona. No compartas tu contraseña.</li>
		<li>Tu nombre de jugador no debe suplantar a otra persona ni ser ofensivo.</li>
		<li>Eres responsable de lo que ocurra con tu cuenta.</li>
	</ul>
{/snippet}

{#snippet play()}
	<ul>
		<li>Nada de trampas, bots ni aprovechar errores a propósito. Si encuentras un error, avísanos.</li>
		<li>Nada de acoso, odio ni spam, ni en el juego ni en tu nombre de jugador o tu avatar.</li>
	</ul>
{/snippet}

{#snippet shared()}
	<p>
		Conservas los derechos sobre el avatar que subes. Nos permites guardarlo y mostrarlo a otros
		jugadores para que el sitio funcione. Podemos retirar un nombre de jugador o un avatar que no
		respete estas normas.
	</p>
{/snippet}

{#snippet end()}
	<p>
		Puedes pedirnos que eliminemos tu cuenta en cualquier momento, usando el contacto de abajo.
		Podemos suspender o eliminar las cuentas que no respeten estas normas.
	</p>
{/snippet}

{#snippet changes()}
	<p>
		Si estos términos cambian, actualizamos la fecha al principio de esta página. Seguir usando
		el sitio después de un cambio supone aceptar los nuevos términos.
	</p>
{/snippet}

{#snippet contact()}
	<p>
		Encuentra el proyecto en GitHub:
		<a href="https://github.com/Transcendence-wejdz">github.com/Transcendence-wejdz</a>.
	</p>
{/snippet}
