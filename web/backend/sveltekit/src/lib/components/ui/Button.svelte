<script lang="ts">
	import type { Snippet } from 'svelte';

	type Props = {
		variant?: 'primary' | 'secondary' | 'danger' | '42';
		size?: 'md' | 'sm';
		href?: string;
		children: Snippet;
	};

	let { variant = 'primary', size = 'md', href, children }: Props = $props();
</script>

{#if href}
	<a {href} class="btn variant-{variant} size-{size}">
		{@render children()}
	</a>
{:else}
	<button type="button" class="btn variant-{variant} size-{size}">
		{@render children()}
	</button>
{/if}

<style>
	.btn {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		gap: 10px;
		min-height: 48px;
		padding: 0 22px;
		font-family: var(--font-ui);
		font-size: 19px;
		font-weight: 700;
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		cursor: pointer;
		text-decoration: none;
	}

	.size-sm {
		min-height: 36px;
		padding: 0 14px;
		font-size: 15px;
	}

	.variant-primary {
		color: var(--color-parchment-text);
		background: var(--color-gold);
		box-shadow: inset 0 -5px 0 var(--color-gold-shadow), var(--shadow-drop);
	}

	.variant-primary:hover {
		background: var(--color-gold-hover);
	}

	.variant-secondary {
		color: var(--color-text);
		background: var(--color-bg-raised);
		box-shadow: inset 0 -5px 0 var(--color-bg-raised-shadow), var(--shadow-drop);
	}

	.variant-secondary:hover {
		color: #fff;
		background: var(--color-bg-raised-hover);
	}

	.variant-danger {
		color: var(--color-outline);
		background: var(--color-danger);
		box-shadow: inset 0 -5px 0 var(--color-danger-shadow), var(--shadow-drop);
	}

	.variant-danger:hover {
		background: var(--color-danger-hover);
	}

	.variant-42 {
		color: var(--color-outline);
		background: var(--color-input-bg);
		box-shadow: inset 0 -5px 0 #d3bd88, var(--shadow-drop);
	}

	.variant-42:hover {
		background: #fff;
	}
</style>
