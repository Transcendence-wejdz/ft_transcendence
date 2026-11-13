import { redirect, type Handle } from '@sveltejs/kit';
import { sequence } from '@sveltejs/kit/hooks';
import { getTextDirection } from '$lib/paraglide/runtime.js';
import { paraglideMiddleware } from '$lib/paraglide/server.js';
import { SESSION_COOKIE, validateSession } from '$lib/server/session.js';

const handleSession: Handle = async ({ event, resolve }) => {
    const token = event.cookies.get(SESSION_COOKIE);
    event.locals.user = token ? await validateSession(token) : null;

    if (token && !event.locals.user) {
        event.cookies.delete(SESSION_COOKIE, { path: '/' }); // stale cookie
    }
	//this if covers actions, +server.ts and page load in (app)/, which the layout load alone does not
    if (event.route.id?.startsWith('/(app)/') && !event.locals.user) {
        redirect(303, `/login?redirectTo=${encodeURIComponent(event.url.pathname + event.url.search)}`);
    }
    return resolve(event);
};

const handleParaglide: Handle = ({ event, resolve }) => paraglideMiddleware(event.request, ({ request, locale }) => {
    return resolve({ ...event, request }, {
        transformPageChunk: ({ html }) => html.replace('%paraglide.lang%', locale).replace('%paraglide.dir%', getTextDirection(locale))
    });
});

export const handle = sequence(handleParaglide, handleSession);
