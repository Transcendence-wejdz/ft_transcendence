export const UPGRADE_TYPES = new Set([
    'AttackSpeed',
    'SpecialCooldown',
    'Damage',
    'Health',
    'MoveSpeed',
    'HealthRegen',
    'ManaMax',
    'ManaRegen',
    'Size',
    'AttackMobility'
]);
export const MAX_UPGRADE_LEVEL = 5;
export const UPGRADE_BASE_COST = 50;

// Gold needed to go from this level to the next (must match PermanentUpgrades.CostFor in Unity).
export function upgradeCost(level: number): number {
    return UPGRADE_BASE_COST * 2 ** level;
}

// Keeps only known types with a level in range, so a malformed column never breaks a purchase.
export function readUpgrades(value: unknown): Record<string, number> {
    const levels: Record<string, number> = {};
    if (!value || typeof value !== 'object' || Array.isArray(value)) return levels;
    for (const [type, level] of Object.entries(value)) {
        if (UPGRADE_TYPES.has(type) && Number.isInteger(level) && (level as number) > 0) {
            levels[type] = Math.min(level as number, MAX_UPGRADE_LEVEL);
        }
    }
    return levels;
}
