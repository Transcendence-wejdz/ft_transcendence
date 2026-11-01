<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLAttributes } from 'svelte/elements';

	type Props = {
		variant?: 'parchment' | 'board';
		as?: 'div' | 'section' | 'article';
		children: Snippet;
	} & HTMLAttributes<HTMLElement>;

	let { variant = 'board', as = 'div', children, ...rest }: Props = $props();
</script>

<svelte:element this={as} {...rest} class="card variant-{variant}">
	{@render children()}
</svelte:element>

<style>
	.card {
		padding: var(--space-lg);
		border-radius: var(--radius-card);
		border-width: var(--border-width);
		border-style: solid;
	}

	.variant-parchment {
		background: var(--color-parchment);
		color: var(--color-parchment-text);
		border-color: var(--color-outline);
		box-shadow: inset 0 0 0 4px var(--color-parchment-shadow), 0 6px 0 var(--color-drop);
	}

	.variant-board {
		background: var(--color-bg-card);
		color: var(--color-text);
		border-color: var(--color-drop);
		box-shadow: inset 0 -6px 0 var(--color-bg-card-shadow);
	}
</style>
