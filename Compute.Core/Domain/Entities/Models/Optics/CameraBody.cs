using Compute.Core.Utils;

namespace Compute.Core.Domain.Entities.Models.Optics
{
    /// <summary>
    /// A camera body, as far as this calculator is concerned: a sensor size and a pixel count.
    ///
    /// Those two are the whole point. Sensor format alone gives depth of field and field of view,
    /// but the NPF rule needs the <em>pitch</em>, and pitch is sensor width over image width in
    /// pixels. Asking a photographer for "image width (px)" is asking them to look up a number
    /// about their own camera that they have no reason to know, and getting it wrong is invisible:
    /// a wrong pitch does not produce an error, it produces a plausible shutter speed that trails
    /// the stars.
    ///
    /// Labels are brand and model, so they are deliberately not localised — a Fujifilm X-T5 is an
    /// X-T5 in every language.
    /// </summary>
    /// <param name="Label">Brand and model as the manufacturer writes them.</param>
    /// <param name="Format">Sensor format, which with the width gives the pixel pitch.</param>
    /// <param name="ImageWidthPixels">Native full-resolution width in pixels.</param>
    public sealed record CameraBody(string Label, Utils.Optics.SensorFormat Format, int ImageWidthPixels)
    {
        /// <summary>
        /// Megapixels, rounded the way the brochure rounds them. Derived rather than stored so it
        /// cannot disagree with the width — and within one format the pixel count is the only
        /// thing separating two bodies as far as this calculator is concerned.
        /// </summary>
        public int Megapixels
        {
            get
            {
                var height = ImageWidthPixels / (Format.WidthMm / Format.HeightMm);
                return (int)Math.Round(ImageWidthPixels * height / 1_000_000.0, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>What the picker shows on the row: the model, with its pixel count beside it.</summary>
        public string MegapixelsLabel => $"{Megapixels} MP";
    }
}
