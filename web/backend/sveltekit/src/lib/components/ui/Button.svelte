<script lang="ts">
	import type { Snippet } from 'svelte';

	let { children }: { children: Snippet } = $props();
</script>

<button type="button" class="btn">
	{@render children()}
</button>

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
		color: var(--color-parchment-text);
		background: var(--color-gold);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		box-shadow: inset 0 -5px 0 var(--color-gold-shadow), var(--shadow-drop);
		cursor: pointer;
	}

	.btn:hover {
		background: var(--color-gold-hover);
	}
</style>
