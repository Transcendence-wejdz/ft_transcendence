<script lang="ts">
	import { getLocale } from '$lib/paraglide/runtime.js';
	import TermsEn from '$lib/content/legal/terms.en.svelte';
	import TermsFr from '$lib/content/legal/terms.fr.svelte';
	import TermsEs from '$lib/content/legal/terms.es.svelte';

	const CONTENT = { en: TermsEn, fr: TermsFr, es: TermsEs };
	const Content = CONTENT[getLocale()];
</script>

<Content />
