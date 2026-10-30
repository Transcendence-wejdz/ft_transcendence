<script lang="ts">
	import type { HTMLInputAttributes } from 'svelte/elements';

	type Props = HTMLInputAttributes;

	let { value = $bindable(), ...rest }: Props = $props();
</script>

<input bind:value {...rest} class="field"/>

<style>
	.field {
		width: 100%;
		box-sizing: border-box;
		min-height: var(--control-md);
		padding: var(--space-sm) var(--space-md);
		font-family: var(--font-body);
		font-size: var(--text-md);
		color: var(--color-parchment-text);
		background: var(--color-input-bg);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		box-shadow: inset 0 3px 0 var(--color-input-shadow);
	}

	.field::placeholder {
		color: var(--color-input-placeholder);
	}

	.field:focus-visible {
		outline: 3px solid var(--color-focus);
		outline-offset: 2px;
	}
</style>
