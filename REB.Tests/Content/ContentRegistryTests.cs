using REB.Engine.Content;
using REB.Engine.Enemy;
using REB.Engine.World;
using Xunit;

namespace REB.Tests.Content;

// ---------------------------------------------------------------------------
//  ContentRegistry tests (Story 10.3 — Content Completion)
//  Verifies that all six floor themes have profiles, and that each profile
//  satisfies the basic content contracts required by RunManagerSystem.
// ---------------------------------------------------------------------------

public sealed class ContentRegistryTests
{
    // =========================================================================
    //  Coverage
    // =========================================================================

    [Fact]
    public void AllSixThemes_HaveRegisteredProfiles()
    {
        var themes = Enum.GetValues<FloorTheme>();
        Assert.Equal(themes.Length, ContentRegistry.ProfileCount);
    }

    [Fact]
    public void GetProfile_ReturnsCorrectTheme()
    {
        foreach (var theme in Enum.GetValues<FloorTheme>())
        {
            var profile = ContentRegistry.GetProfile(theme);
            Assert.Equal(theme, profile.Theme);
        }
    }

    [Fact]
    public void AllProfiles_HaveAtLeastOneCommonEnemy()
    {
        foreach (var profile in ContentRegistry.AllProfiles)
            Assert.NotEmpty(profile.CommonEnemies);
    }

    [Fact]
    public void AllProfiles_HavePositiveEnemiesPerRoom()
    {
        foreach (var profile in ContentRegistry.AllProfiles)
            Assert.True(profile.EnemiesPerRoom > 0,
                $"Theme {profile.Theme} has EnemiesPerRoom ≤ 0.");
    }

    [Fact]
    public void AllProfiles_HavePositiveLootMultiplier()
    {
        foreach (var profile in ContentRegistry.AllProfiles)
            Assert.True(profile.LootMultiplier > 0f,
                $"Theme {profile.Theme} has LootMultiplier ≤ 0.");
    }

    [Fact]
    public void AllProfiles_HaveValidBossSpawnChance()
    {
        foreach (var profile in ContentRegistry.AllProfiles)
        {
            Assert.True(profile.BossSpawnChance >= 0f && profile.BossSpawnChance <= 1f,
                $"Theme {profile.Theme} BossSpawnChance {profile.BossSpawnChance} is outside [0, 1].");
        }
    }

    [Fact]
    public void AllProfiles_HavePositiveBaseFloorDifficulty()
    {
        foreach (var profile in ContentRegistry.AllProfiles)
            Assert.True(profile.BaseFloorDifficulty >= 1,
                $"Theme {profile.Theme} BaseFloorDifficulty < 1.");
    }

    // =========================================================================
    //  Specific theme spot-checks
    // =========================================================================

    [Fact]
    public void Dungeon_HasGuardAndArcher()
    {
        var profile = ContentRegistry.GetProfile(FloorTheme.Dungeon);
        Assert.Contains(EnemyArchetype.Guard,  profile.CommonEnemies);
        Assert.Contains(EnemyArchetype.Archer, profile.CommonEnemies);
    }

    [Fact]
    public void TreasureVault_HasHighestLootMultiplier()
    {
        float vaultMultiplier = ContentRegistry.GetProfile(FloorTheme.TreasureVault).LootMultiplier;

        foreach (var profile in ContentRegistry.AllProfiles)
        {
            if (profile.Theme == FloorTheme.TreasureVault) continue;
            Assert.True(vaultMultiplier >= profile.LootMultiplier,
                $"TreasureVault ({vaultMultiplier}) should have highest loot multiplier; " +
                $"{profile.Theme} has {profile.LootMultiplier}.");
        }
    }

    [Fact]
    public void Crypt_HasHigherDifficulty_ThanGarden()
    {
        var crypt  = ContentRegistry.GetProfile(FloorTheme.Crypt);
        var garden = ContentRegistry.GetProfile(FloorTheme.Garden);
        Assert.True(crypt.BaseFloorDifficulty > garden.BaseFloorDifficulty);
    }

    // =========================================================================
    //  Fallback behaviour
    // =========================================================================

    [Fact]
    public void GetProfile_ReturnsDungeonFallback_ForUnknownTheme()
    {
        // Cast an out-of-range enum value that has no profile.
        var profile = ContentRegistry.GetProfile((FloorTheme)999);
        Assert.Equal(FloorTheme.Dungeon, profile.Theme);
    }

    // =========================================================================
    //  AllThemes / AllProfiles enumerables
    // =========================================================================

    [Fact]
    public void AllThemes_ContainsAllSixThemes()
    {
        foreach (var theme in Enum.GetValues<FloorTheme>())
            Assert.Contains(theme, ContentRegistry.AllThemes);
    }

    [Fact]
    public void AllProfiles_HasSixEntries()
    {
        Assert.Equal(6, ContentRegistry.AllProfiles.Count());
    }
}
