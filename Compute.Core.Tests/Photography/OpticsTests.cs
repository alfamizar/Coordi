using Compute.Core.Domain.Entities.Models.Optics;
using Optics = Compute.Core.Utils.Optics;

namespace Compute.Core.Tests.Photography
{
    /// <summary>
    /// Reference checks against published depth-of-field and field-of-view values, ported from
    /// AstroClaw's :feature-optics tests so both apps are held to the same numbers.
    /// </summary>
    public class OpticsTests
    {
        private static readonly Optics.SensorFormat Ff = Optics.SensorFormat.FullFrame;

        [Fact]
        public void CircleOfConfusion_ComesFromTheDiagonal()
        {
            // Full frame: 43.27 mm diagonal / 1500 ≈ 0.0288 mm — the standard FF CoC.
            Assert.Equal(0.0288, Ff.CircleOfConfusionMm, 3);
            // Micro 4/3 is roughly half of full frame.
            Assert.Equal(0.0144, Optics.SensorFormat.MicroFourThirds.CircleOfConfusionMm, 3);
        }

        [Fact]
        public void DepthOfField_FullFrame50mmF8At5m()
        {
            var dof = Optics.CalculateDepthOfField(50.0, 8.0, 5.0, Ff);

            Assert.Equal(10.9, dof.HyperfocalMeters, 0.2);   // ≈ 10.88 m
            Assert.Equal(3.43, dof.NearLimitMeters, 0.05);
            Assert.NotNull(dof.FarLimitMeters);
            Assert.Equal(9.2, dof.FarLimitMeters!.Value, 0.1);
            Assert.Equal(5.77, dof.TotalMeters!.Value, 0.1);
            Assert.InRange(dof.InFrontMeters, 1.5, 1.6);
            Assert.InRange(dof.BehindMeters!.Value, 4.1, 4.3);
        }

        [Fact]
        public void DepthOfField_FarLimitIsInfiniteAtHyperfocal()
        {
            var h = Optics.HyperfocalMeters(50.0, 8.0, Ff.CircleOfConfusionMm);
            var dof = Optics.CalculateDepthOfField(50.0, 8.0, h + 1.0, Ff);

            Assert.Null(dof.FarLimitMeters);
            Assert.Null(dof.TotalMeters);
            Assert.Null(dof.BehindMeters);
        }

        [Fact]
        public void Hyperfocal_AgreesWithTheDepthOfFieldResult()
        {
            var h = Optics.HyperfocalMeters(50.0, 8.0, Ff.CircleOfConfusionMm);
            var dof = Optics.CalculateDepthOfField(50.0, 8.0, 5.0, Ff);

            Assert.Equal(dof.HyperfocalMeters, h, 9);
        }

        [Fact]
        public void FieldOfView_FullFrame50mm()
        {
            var fov = Optics.CalculateFieldOfView(50.0, 5.0, Ff);

            Assert.Equal(39.6, fov.HorizontalDeg, 0.3);  // the "nifty fifty" ≈ 40° horizontal
            Assert.Equal(27.0, fov.VerticalDeg, 0.3);
            Assert.Equal(46.8, fov.DiagonalDeg, 0.3);
            // Linear coverage at 5 m: 2·5·tan(19.8°) ≈ 3.6 m.
            Assert.Equal(3.6, fov.HorizontalCoverageMeters, 0.1);
        }

        [Fact]
        public void FieldOfView_AWiderLensSeesMore()
        {
            var wide = Optics.CalculateFieldOfView(24.0, 5.0, Ff).HorizontalDeg;
            var tele = Optics.CalculateFieldOfView(200.0, 5.0, Ff).HorizontalDeg;

            Assert.True(wide > 70.0, $"24 mm should be wide, got {wide}");
            Assert.True(tele < 12.0, $"200 mm should be narrow, got {tele}");
        }
    }

    /// <summary>
    /// Star-trail exposure limits, checked against the figures astrophotographers actually quote.
    /// Both rules answer "how long before trailing shows", so these pin the values that appear in
    /// the standard tables rather than merely asserting the code runs.
    /// </summary>
    public class StarExposureTests
    {
        private static readonly Optics.SensorFormat Ff = Optics.SensorFormat.FullFrame;

        /// <summary>The 500 rule is the one everybody knows: 20 mm full frame → 25 s.</summary>
        [Fact]
        public void Rule500_MatchesTheClassicTable()
        {
            Assert.Equal(25.0, Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000).Rule500Seconds, 0.1);
            Assert.Equal(35.7, Optics.CalculateStarExposure(14.0, 2.8, Ff, 6000).Rule500Seconds, 0.2);
        }

        /// <summary>On a crop body the rule uses the equivalent focal length, so the limit shortens.</summary>
        [Fact]
        public void Rule500_UsesTheFullFrameEquivalentFocalLength()
        {
            var ff = Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000);
            var apsc = Optics.CalculateStarExposure(20.0, 2.8, Optics.SensorFormat.ApsC, 6000);

            Assert.True(apsc.Rule500Seconds < ff.Rule500Seconds, "APS-C must allow less time than full frame");
            // APS-C crop ≈ 1.53×, so ~25 s / 1.53 ≈ 16.3 s.
            Assert.Equal(16.3, apsc.Rule500Seconds, 0.4);
        }

        /// <summary>
        /// NPF on a 24 MP full frame (6000 px ⇒ 6 µm pitch) at 20 mm f/2.8:
        /// (35·2.8 + 30·6) / 20 = 13.9 s — roughly half the 500 rule.
        /// </summary>
        [Fact]
        public void Npf_MatchesTheHandComputedValue()
        {
            var e = Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000);

            Assert.Equal(6.0, e.PixelPitchMicrons, 0.01);
            Assert.Equal(13.9, e.NpfSeconds, 0.1);
            Assert.True(e.NpfSeconds < e.Rule500Seconds, "NPF must be stricter than the 500 rule");
        }

        /// <summary>Denser sensors trail sooner through the same lens — the point of NPF.</summary>
        [Fact]
        public void Npf_TightensAsPixelsShrink()
        {
            var mp24 = Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000);
            var mp61 = Optics.CalculateStarExposure(20.0, 2.8, Ff, 9504);

            Assert.True(mp61.NpfSeconds < mp24.NpfSeconds, "a 61 MP body must allow less time than 24 MP");
            // The 500 rule cannot see the difference at all — that is its blind spot.
            Assert.Equal(mp24.Rule500Seconds, mp61.Rule500Seconds, 9);
        }

        /// <summary>Stars near the pole drift slower, so both rules relax by 1/cos δ.</summary>
        [Fact]
        public void HighDeclination_AllowsLongerExposures()
        {
            var equator = Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000, 0.0);
            var dec60 = Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000, 60.0);

            // cos 60° = 0.5, so the limit doubles.
            Assert.Equal(equator.Rule500Seconds * 2.0, dec60.Rule500Seconds, 0.05);
            Assert.Equal(equator.NpfSeconds * 2.0, dec60.NpfSeconds, 0.05);
        }

        /// <summary>cos δ → 0 at the pole must not divide by zero.</summary>
        [Fact]
        public void ThePole_IsClampedRatherThanInfinite()
        {
            var pole = Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000, 90.0);

            Assert.True(double.IsFinite(pole.Rule500Seconds) && double.IsFinite(pole.NpfSeconds));
            Assert.True(pole.Rule500Seconds > 0.0);

            // Negative declinations are symmetric — the southern sky behaves the same.
            var south = Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000, -60.0);
            var north = Optics.CalculateStarExposure(20.0, 2.8, Ff, 6000, 60.0);
            Assert.Equal(north.NpfSeconds, south.NpfSeconds, 9);
        }
    }

    /// <summary>
    /// The catalogue. A dropped digit in a pixel width produces a plausible-looking but wrong
    /// shutter speed rather than an error, so every entry is checked for a pitch a real sensor
    /// could actually have.
    /// </summary>
    public class CameraBodiesTests
    {
        [Fact]
        public void EveryBody_HasAPlausiblePixelPitch()
        {
            foreach (var body in CameraBodies.All)
            {
                var pitch = Optics.PixelPitchMicrons(body.Format, body.ImageWidthPixels);
                Assert.True(pitch > 2.0 && pitch < 10.0,
                    $"{body.Label}: pitch {pitch:F2} µm is not one a real sensor has");
            }
        }

        [Fact]
        public void EveryBody_HasAPlausibleMegapixelCount()
        {
            foreach (var body in CameraBodies.All)
            {
                Assert.True(body.Megapixels is > 10 and < 120,
                    $"{body.Label}: {body.Megapixels} MP is out of range");
            }
        }

        [Fact]
        public void Labels_AreUnique()
        {
            var labels = CameraBodies.All.Select(b => b.Label).ToList();
            Assert.Equal(labels.Count, labels.Distinct().Count());
        }

        /// <summary>
        /// The opening default — full frame at 6000 px — is four different bodies, so naming one
        /// would greet everybody with a camera they do not own.
        /// </summary>
        [Fact]
        public void UniqueMatch_IsNullWhenThePixelCountIsAmbiguous()
        {
            Assert.Null(CameraBodies.UniqueMatch(Optics.SensorFormat.FullFrame, 6000));
        }

        [Fact]
        public void UniqueMatch_NamesTheBodyWhenTheNumbersCanOnlyBeOne()
        {
            var match = CameraBodies.UniqueMatch(Optics.SensorFormat.ApsC, 7728);

            Assert.NotNull(match);
            Assert.Equal("Fujifilm X-T5", match!.Label);
        }

        [Fact]
        public void UniqueMatch_IsNullWhenNothingMatches()
        {
            Assert.Null(CameraBodies.UniqueMatch(Optics.SensorFormat.FullFrame, 1234));
        }
    }
}
