<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLLabelAttributes } from 'svelte/elements';

	type Props = {
		children: Snippet;
	} & HTMLLabelAttributes;

	let { children, ... rest}: Props = $props();
</script>

<label {...rest } class="label">
	{@render children()}
</label>

<style>
	.label {
		font-family: var(--font-ui);
		font-weight: 600;
		font-size: var(--text-md);
	}
</style>
