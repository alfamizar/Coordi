using Format = Compute.Core.Utils.Optics.SensorFormat;

namespace Compute.Core.Domain.Entities.Models.Optics
{
    /// <summary>
    /// The bodies offered in the picker.
    ///
    /// Chosen for coverage of the formats rather than completeness of any brand: someone whose
    /// camera is not here still has the format chips and the pixel field, and a list trying to be
    /// exhaustive would be a list going stale. Every width is the manufacturer's full-resolution
    /// figure — see the tests, which check each one yields a pitch a real sensor could have,
    /// because a digit dropped from 6000 is exactly the kind of typo nothing else would catch.
    ///
    /// No phones. Their sensors are quoted as fractions of an inch that do not map to one set of
    /// millimetres, they vary between models sharing a name, and most answer the night sky with a
    /// stacking mode rather than a single exposure — so an NPF figure for a phone would be precise
    /// about the wrong thing.
    /// </summary>
    public static class CameraBodies
    {
        public static IReadOnlyList<CameraBody> All { get; } =
        [
            // Full frame, 36 × 24.
            new("Canon EOS R5", Format.FullFrame, 8192),
            new("Canon EOS R6 Mark II", Format.FullFrame, 6000),
            new("Canon EOS R8", Format.FullFrame, 6000),
            new("Nikon Z 6III", Format.FullFrame, 6048),
            new("Nikon Z 8", Format.FullFrame, 8256),
            new("Nikon Zf", Format.FullFrame, 6048),
            new("Panasonic Lumix S5 II", Format.FullFrame, 6000),
            new("Sony α1", Format.FullFrame, 8640),
            new("Sony α7 III", Format.FullFrame, 6000),
            new("Sony α7 IV", Format.FullFrame, 7008),
            new("Sony α7R V", Format.FullFrame, 9504),
            new("Sony α7S III", Format.FullFrame, 4240),

            // APS-C at the 1.5× crop.
            new("Fujifilm X-S20", Format.ApsC, 6240),
            new("Fujifilm X-T4", Format.ApsC, 6240),
            new("Fujifilm X-T5", Format.ApsC, 7728),
            new("Nikon Z 50II", Format.ApsC, 5568),
            new("Pentax K-3 Mark III", Format.ApsC, 6192),
            new("Sony α6400", Format.ApsC, 6000),
            new("Sony α6700", Format.ApsC, 6192),

            // Canon's APS-C is smaller again — 1.6×, and the difference shows in the pitch.
            new("Canon EOS R7", Format.ApsCCanon, 6960),
            new("Canon EOS R10", Format.ApsCCanon, 6000),
            new("Canon EOS R100", Format.ApsCCanon, 6000),

            // Micro Four Thirds.
            new("OM System OM-1 Mark II", Format.MicroFourThirds, 5184),
            new("Panasonic Lumix G9 II", Format.MicroFourThirds, 5776),
            new("Panasonic Lumix GH6", Format.MicroFourThirds, 5776),

            // One inch.
            new("Sony RX100 VII", Format.OneInch, 5472),
            new("Sony ZV-1", Format.OneInch, 5472),

            // Medium format, 44 × 33.
            new("Fujifilm GFX 50S II", Format.MediumFormat, 8256),
            new("Fujifilm GFX100 II", Format.MediumFormat, 11648),
            new("Fujifilm GFX100S II", Format.MediumFormat, 11648),
        ];

        /// <summary>
        /// The one body a format and pixel width can only be — null when it could be several, or none.
        ///
        /// "Only be" is the point. Full frame at 6000 px is the opening default and it is also the
        /// EOS R6 Mark II, the EOS R8, the Lumix S5 II and the α7 III, so picking the first match
        /// would mean the screen opens by telling everyone they shoot Canon. A 24 MP full-frame
        /// body is not identifiable from its pixel count, and the honest answer there is no name.
        ///
        /// Where the numbers <em>are</em> unique the name is worth showing: 7728 px on APS-C is an
        /// X-T5 and nothing else.
        /// </summary>
        public static CameraBody? UniqueMatch(Format format, int imageWidthPixels)
        {
            CameraBody? found = null;

            foreach (var body in All)
            {
                if (body.Format != format || body.ImageWidthPixels != imageWidthPixels) continue;
                if (found is not null) return null; // ambiguous
                found = body;
            }

            return found;
        }
    }
}
