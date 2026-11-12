using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Foundation.Systems.Game;

public class PauseControllerTests
{
    private GameObject owner;
    private PauseController pauseController;

    [SetUp]
    public void SetUp()
    {
        owner = new GameObject(nameof(PauseControllerTests));
        pauseController = owner.AddComponent<PauseController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(owner);
    }
    // A Test behaves as an ordinary method

    [Test]
    public void Start_Unpaused()
    {
        // At the start, the game should not be paused and the token count should be zero.
        Assert.That(pauseController.IsPaused, Is.False);
        Assert.That(pauseController.PauseTokenCount, Is.Zero);
    }

    [Test]
    public void RequestPause_PausesGame()
    {
        // Request a pause token and check that the game is paused and the token count is one.
        var token = pauseController.RequestPause();
        Assert.That(pauseController.IsPaused, Is.True);
        Assert.That(pauseController.PauseTokenCount, Is.EqualTo(1));

        // The game should remain paused and the token count should still be one before releasing the token.
        token.Release();
        Assert.That(pauseController.IsPaused, Is.False);
        Assert.That(pauseController.PauseTokenCount, Is.Zero);
    }

    // Multiple release attempts of the same token should not affect the paused state.
    [Test]
    public void ReleasingSameTokenMultipleTimes_DoesNotAffectPausedState()
    {
        var token = pauseController.RequestPause();
        Assert.That(pauseController.IsPaused, Is.True);
        Assert.That(pauseController.PauseTokenCount, Is.EqualTo(1));

        token.Release();
        Assert.That(pauseController.IsPaused, Is.False);
        Assert.That(pauseController.PauseTokenCount, Is.Zero);

        // Attempt to release the same token again.
        token.Release();
        Assert.That(pauseController.IsPaused, Is.False);
        Assert.That(pauseController.PauseTokenCount, Is.Zero);
    }

    [Test]
    public void ReleasingOneOfTwoTokens_KeepsGamePausedUntilBothAreReleased()
    {
        // Request two pause tokens to simulate multiple systems pausing the game.
        var token1 = pauseController.RequestPause();
        var token2 = pauseController.RequestPause();

        // The game should be paused and the token count should reflect both tokens.
        Assert.That(pauseController.IsPaused, Is.True);
        Assert.That(pauseController.PauseTokenCount, Is.EqualTo(2));

        // Release the first token and check that the game is still paused.
        token1.Release();
        Assert.That(pauseController.IsPaused, Is.True);
        Assert.That(pauseController.PauseTokenCount, Is.EqualTo(1));

        // Attempt to release token1 again, which should have no effect.
        token1.Release();
        Assert.That(pauseController.IsPaused, Is.True);
        Assert.That(pauseController.PauseTokenCount, Is.EqualTo(1));

        // Release the second token and check that the game is no longer paused.
        token2.Release();
        Assert.That(pauseController.IsPaused, Is.False);
        Assert.That(pauseController.PauseTokenCount, Is.Zero);
    }

    //Token release order should not affect the final paused state of the game.
    [Test]
    public void ReleasingTokensInAnyOrder_ResultsInCorrectPausedState()
    {
        // Request two pause tokens to simulate multiple systems pausing the game.
        var token1 = pauseController.RequestPause();
        var token2 = pauseController.RequestPause();

        // Release the tokens in reverse order and check that the game is no longer paused.
        token2.Release();
        Assert.That(pauseController.IsPaused, Is.True);
        Assert.That(pauseController.PauseTokenCount, Is.EqualTo(1));

        token1.Release();
        Assert.That(pauseController.IsPaused, Is.False);
        Assert.That(pauseController.PauseTokenCount, Is.Zero);
    }
}
