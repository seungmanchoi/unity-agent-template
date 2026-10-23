using Game.Domain;
using NUnit.Framework;

namespace Game.Tests
{
    public class GameSimTests
    {
        [Test]
        public void SameSeedSameTargets()
        {
            var a = new GameSim(42);
            var b = new GameSim(42);
            for (int i = 0; i < 300; i++)
            {
                Assert.AreEqual(a.TargetX, b.TargetX);
                Assert.AreEqual(a.TargetY, b.TargetY);
                a.Tick();
                b.Tick();
            }
            Assert.AreNotEqual(new GameSim(1).TargetX, new GameSim(2).TargetX);
        }

        [Test]
        public void HitScoresAndMovesTarget()
        {
            var sim = new GameSim(7);
            float x = sim.TargetX, y = sim.TargetY;
            Assert.IsTrue(sim.Tap(x + sim.Rules.TargetRadius * 0.9f, y));
            Assert.AreEqual(1, sim.Score);
            Assert.AreNotEqual(x, sim.TargetX);
        }

        [Test]
        public void MissDoesNotScore()
        {
            var sim = new GameSim(7);
            Assert.IsFalse(sim.Tap(sim.TargetX + sim.Rules.TargetRadius * 1.1f, sim.TargetY));
            Assert.AreEqual(0, sim.Score);
        }

        [Test]
        public void TargetTimesOutAsAMiss()
        {
            var sim = new GameSim(7);
            float x = sim.TargetX;
            for (int i = 0; i < sim.Rules.TargetLifeSteps; i++) sim.Tick();
            Assert.AreEqual(1, sim.Misses);
            Assert.AreNotEqual(x, sim.TargetX);
        }

        [Test]
        public void EndsAfterDurationAndIgnoresInput()
        {
            var sim = new GameSim(7);
            for (int i = 0; i < sim.Rules.DurationSteps + 10; i++) sim.Tick();
            Assert.IsTrue(sim.Done);
            Assert.AreEqual(sim.Rules.DurationSteps, sim.Step);
            Assert.IsFalse(sim.Tap(sim.TargetX, sim.TargetY));
            Assert.AreEqual(0f, sim.SecondsLeft);
        }
    }
}
