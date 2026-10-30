<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { HTMLAttributes } from 'svelte/elements';

	type Props = {
		dot?: string;
		children: Snippet;
	} & HTMLAttributes<HTMLSpanElement>;

	let { dot, children, ...rest };
</script>

<span {...rest} class="tag">

{#if dot}
	<span
{/if}
