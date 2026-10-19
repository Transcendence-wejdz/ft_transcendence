using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// RuleTile treating configured tiles as connected siblings.
[CreateAssetMenu(fileName = "New Sibling Rule Tile", menuName = "Tiles/Sibling Rule Tile")]
public class SiblingRuleTile : RuleTile<SiblingRuleTile.Neighbor>
{
    public class Neighbor : RuleTile.TilingRuleOutput.Neighbor
    {
        public const int Sibling = 3;
        public const int NotSibling = 4;
    }

    [Tooltip("Other tiles that count as connected neighbors (e.g. the other ground biomes).")]
    public List<TileBase> siblingTiles = new();

    public override bool RuleMatch(int neighbor, TileBase other)
    {
        switch (neighbor)
        {
            case Neighbor.Sibling:
                return other == this || siblingTiles.Contains(other);
            case Neighbor.NotSibling:
                return other != this && !siblingTiles.Contains(other);
        }

        return base.RuleMatch(neighbor, other);
    }
}
