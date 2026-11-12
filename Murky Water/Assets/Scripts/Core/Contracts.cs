using UnityEngine;

/// Source of the hit received by the fish
public enum HitSource
{
    Obstacle,
    Predator
}

/// Cause recorded when a game ends in defeat.
public enum DeathCause
{
    Health,
    Energy,
    Lethal,
    Predator
}

/// Global game states.
public enum GameState
{
    Menu,
    Countdown,
    Playing,
    Paused,
    Won,
    Lost
}

/// Common data structure for obstacles and the predator.
/// Percentages are expressed as percentage points of the maximum value (e.g., 25 = 25%).

[System.Serializable]
public struct HitData
{
    public bool isLethal;
    [Min(0f)] public float lifeDamagePercent;
    [Min(0f)] public float energyDamagePercent;
    [Min(0f)] public float visionDuration;
    [Range(0f, 1f)] public float visionIntensity;
    public HitSource source;
}

/// Contract for objects that react to player contact.
public interface IPlayerInteractable
{
    void OnPlayerContact(PlayerCollisionHandler player);
}
