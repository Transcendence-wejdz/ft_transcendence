import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db.js';
import { CLASSES, COLORS, displayNameError, isUniqueViolation } from '$lib/server/validation.js';

const MAX_AVATAR_LENGTH = 700_000;

export const PUT: RequestHandler = async ({ locals, request }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    let body: unknown;
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }
    if (!body || typeof body !== 'object') {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }

    const input = body as Record<string, unknown>;
    const displayName = typeof input.display_name === 'string' ? input.display_name.trim() : '';
    const color = input.color;
    const playerClass = input.class;
    const avatarUrl = input.avatar_url;

    const nameError = displayNameError(displayName);
    if (nameError) return json({ success: false, error: nameError }, { status: 400 });
    if (typeof color !== 'string' || !COLORS.has(color)) {
        return json({ success: false, error: 'Choose a color from the available options.' }, { status: 400 });
    }
    if (typeof playerClass !== 'string' || !CLASSES.has(playerClass)) {
        return json({ success: false, error: 'Choose a class from the available options.' }, { status: 400 });
    }
    if (avatarUrl !== null && (typeof avatarUrl !== 'string' || avatarUrl.length > MAX_AVATAR_LENGTH || !/^data:image\/(png|jpeg|webp|gif);base64,[A-Za-z0-9+/]+=*$/.test(avatarUrl))) {
        return json({ success: false, error: 'Upload a PNG, JPG, WebP or GIF smaller than 500 KB.' }, { status: 400 });
    }

    try {
        const user = await prisma.user.update({
            where: { id: locals.user.id },
            data: { displayName, color, class: playerClass, avatarUrl },
            select: { displayName: true, color: true, class: true, avatarUrl: true }
        });
        const updatedUser = {
            ...locals.user,
            display_name: user.displayName,
            color: user.color,
            class: user.class,
            avatar_url: user.avatarUrl
        };
        locals.user = updatedUser;
        return json({ success: true, user: updatedUser });
    } catch (error) {
        if (isUniqueViolation(error)) {
            return json({ success: false, error: 'That player name is already taken.' }, { status: 409 });
        }
        console.error('Failed to update player settings:', error);
        return json({ success: false, error: 'Could not save your settings. Please try again.' }, { status: 500 });
    }
};
