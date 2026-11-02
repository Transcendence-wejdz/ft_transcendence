<script lang="ts">
	import { Toaster as Sonner } from 'svelte-sonner';
</script>

<Sonner
	position="bottom-right"
	closeButton
	toastOptions={{
		unstyled: true,
		classes: {
			toast: 'toast',
			icon: 'toast-icon',
			content: 'toast-content',
			title: 'toast-title',
			description: 'toast-description',
			closeButton: 'toast-close',
			success: 'toast-success',
			error: 'toast-error'
		}
	}}
>
	{#snippet successIcon()}
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
			stroke-width="3.2" stroke-linecap="square" aria-hidden="true"><path d="M5 12l5 5 9-10" /></svg>
	{/snippet}
	{#snippet errorIcon()}
		<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor"
			stroke-width="3.2" stroke-linecap="square" aria-hidden="true"><path d="M12 5v9M12 18v1" /></svg>
	{/snippet}
	{#snippet closeIcon()}
		<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
			stroke-width="3" stroke-linecap="square" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18" /></svg>
	{/snippet}
</Sonner>

<style>
	:global(.toast) {
		display:flex;
		align-items: center;
		gap: 0.75rem;
		padding: 0.75rem 0.625rem 0.75rem 0.875rem;
		background: var(--color-parchment);
		color: var(--color-parchment-text);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius-card);
		box-shadow: inset 0 0 0 3px var(--color-parchment-shadow), 0 4px 0 var(--color-drop);
		font-family: var(--font-body);
		font-size: 0.9375rem;
	}

	:global(.toast-icon) {
		flex: none;
		display: grid;
		place-items: center;
		width: 1.75rem;
		height: 1.75rem;
		border: 2px solid var(--color-outline);
		border-radius: 5px;
		color: var(--color-outline);
	}

	:global(.toast-success .toast-icon) {
		background: var(--color-online);
	}

	:global(.toast-error .toast-icon) {
		background: var(--color-danger);
	}

	:global(.toast-title) {
		font-weight: 700;
	}

	:global(.toast-description) {
		margin-top: 0.125rem;
		font-size: 0.8125rem;
		color: var(--color-parchment-text-muted);
	}

	:global(.toast-close) {
		order: 1;
		flex: none;
		display: grid;
		place-items: center;
		width: 2rem;
		height: 2rem;
		background: transparent;
		border: 0;
		border-radius: 4px;
		color: var(--color-parchment-text-muted);
		cursor: pointer;
	}

	:global(.toast-content) {
		flex: 1;
		min-width: 0;
	}
</style>
