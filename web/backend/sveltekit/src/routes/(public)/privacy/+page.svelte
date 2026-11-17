<script lang="ts">
	import { getLocale } from '$lib/paraglide/runtime.js';
	import PrivacyEn from '$lib/content/legal/privacy.en.svelte';
	import PrivacyFr from '$lib/content/legal/privacy.fr.svelte';
	import PrivacyEs from '$lib/content/legal/privacy.es.svelte';

	const CONTENT = { en: PrivacyEn, fr: PrivacyFr, es: PrivacyEs };
	const Content = CONTENT[getLocale()];
</script>

<Content />
