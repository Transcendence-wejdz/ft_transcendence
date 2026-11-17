<script lang="ts">
	import { page } from '$app/state';
	import { m } from '$lib/paraglide/messages.js';
	import Button from '$lib/components/ui/Button.svelte';
	import Ribbon from '$lib/components/ui/Ribbon.svelte';
	import PixelNumber from '$lib/components/PixelNumber.svelte';

	const notFound = $derived(page.status === 404);
</script>

<svelte:head><title>Transcendence — {notFound ? m.error_404_ribbon() : m.error_500_ribbon()}</title></svelte:head>

<section class="error-page">
	<Ribbon>{notFound ? m.error_404_ribbon() : m.error_500_ribbon()}</Ribbon>
	<div class="number"><PixelNumber value={page.status} /></div>
	<h1>{notFound ? m.error_404_title() : m.error_500_title()}</h1>
	<p>{notFound ? m.error_404_text() : m.error_500_text()}</p>
	<div class="actions">
		<Button href="/">{m.error_home()}</Button>
		{#if notFound}
			<Button variant="secondary" href="/about">{m.error_about()}</Button>
		{:else}
			<Button variant="secondary" onclick={() => location.reload()}>{m.error_retry()}</Button>
		{/if}
	</div>
</section>

<style>
	.error-page {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		min-height: 60vh;
		text-align: center;
	}

	.number {
		margin: 1.875rem 0 1rem;
	}

	h1 {
		margin: 1.75rem 0 0.625rem;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: clamp(2.75rem, 6vw, 4rem);
		line-height: 1;
	}

	p {
		max-width: 28.75rem;
		margin: 0 0 2rem;
		color: var(--color-text-secondary);
		font-size: 1.125rem;
		line-height: 1.6;
	}

	.actions {
		display: flex;
		flex-wrap: wrap;
		justify-content: center;
		gap: 0.875rem;
	}
</style>
