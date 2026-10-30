<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLAnchorAttributes, HTMLButtonAttributes } from 'svelte/elements';

	type Props = {
		variant?: 'primary' | 'secondary' | 'danger' | '42';
		size?: 'md' | 'sm';
		href?: string;
		children: Snippet;
		busy?: boolean;
	} & HTMLButtonAttributes & HTMLAnchorAttributes;

	let { variant = 'primary', size = 'md', href, type = 'button', disabled, busy = false, children, ...rest }: Props = $props();
</script>

{#if href}
	<a {href} {...rest} class="btn variant-{variant} size-{size}">
		{@render children()}
	</a>
{:else}
	<button
		{type}
		{...rest}
		disabled={disabled || busy}
		aria-busy={busy}
		class="btn variant-{variant} size-{size}"
		class:busy
	>
		{#if busy}
			<span class="spinner"></span>
		{/if}
		{@render children()}
	</button>
{/if}

<style>
	.btn {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: var(--space-sm);
		min-height: var(--control-md);
		padding: 0 var(--space-lg);
		font-family: var(--font-ui);
		font-size: var(--text-lg);
		font-weight: 700;
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		cursor: pointer;
		text-decoration: none;
	}

	.size-sm {
		min-height: var(--control-sm);
		padding: 0 var(--space-md);
		font-size: var(--text-md);
	}

	.variant-primary {
		color: var(--color-parchment-text);
		background: var(--color-gold);
		box-shadow: inset 0 -5px 0 var(--color-gold-shadow), var(--shadow-drop);
	}

	.variant-primary:hover:not(:disabled) {
		background: var(--color-gold-hover);
	}

	.variant-secondary {
		color: var(--color-text);
		background: var(--color-bg-raised);
		box-shadow: inset 0 -5px 0 var(--color-bg-raised-shadow), var(--shadow-drop);
	}

	.variant-secondary:hover:not(:disabled) {
		color: #fff;
		background: var(--color-bg-raised-hover);
	}

	.variant-danger {
		color: var(--color-outline);
		background: var(--color-danger);
		box-shadow: inset 0 -5px 0 var(--color-danger-shadow), var(--shadow-drop);
	}

	.variant-danger:hover:not(:disabled) {
		background: var(--color-danger-hover);
	}

	.variant-42 {
		color: var(--color-outline);
		background: var(--color-input-bg);
		box-shadow: inset 0 -5px 0 #d3bd88, var(--shadow-drop);
	}

	.variant-42:hover:not(:disabled) {
		background: #fff;
	}

	.btn:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.busy:disabled {
		opacity: 1;
		cursor: wait;
	}

	.btn:focus-visible {
		outline: 3px solid var(--color-focus);
		outline-offset: 3px;
	}

	.spinner {
		width: 16px;
		height: 16px;
		border: 3px solid currentColor;
		border-right-color: transparent;
		border-radius: 50%;
		animation: spin 0.8s linear infinite;
	}

	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}
</style>
