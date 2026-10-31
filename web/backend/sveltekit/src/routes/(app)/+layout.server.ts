import { redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';
//hooks.server.ts is the real guard; this load reads `url` so it reruns on every client-side navigation.
export const load: LayoutServerLoad = ({ locals, url }) => {
    if (!locals.user) redirect(303, `/login?redirectTo=${encodeURIComponent(url.pathname + url.search)}`);
};
