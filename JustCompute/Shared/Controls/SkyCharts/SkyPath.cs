namespace JustCompute.Shared.Controls
{
    /// <summary>
    /// Geometry for a body across a midnight-to-midnight (0..24) axis: its time above the horizon
    /// as one or more arcs, and any clock hour mapped to an x and y on the curve. The arc is a
    /// half-sine above the horizon and the night a half-sine trough below it.
    ///
    /// Handles every daily shape, not only a rise before a set: a span that wraps midnight (set
    /// earlier than rise), a day with only a rise (still up at midnight) or only a set (already up
    /// at 00:00), and a body up all day. A day with nothing above the horizon has no path. The
    /// Moon needs all of them; the Sun needs the last two above the polar circles.
    ///
    /// Ported from Penombre's SkyPath, which shares this axis with its sun and moon charts.
    /// </summary>
    internal sealed class SkyPath
    {
        /// <summary>
        /// One span above the horizon. <see cref="RisesAtStart"/> and <see cref="SetsAtEnd"/> are
        /// false where that end is only the edge of the chart rather than a horizon crossing,
        /// which draws a rising or falling half instead of a whole arc.
        /// </summary>
        private readonly record struct Arc(double Start, double End, bool RisesAtStart, bool SetsAtEnd);

        private readonly float _width;
        private readonly float _horizonY;
        private readonly float _nightAmp;
        private readonly Arc[] _arcs;

        /// <summary>When the span above the horizon wraps midnight, its rise and set.</summary>
        private readonly (double Rise, double Set)? _wrapUp;

        private SkyPath(float width, float horizonY, float dayAmp, float nightAmp, Arc[] arcs)
        {
            _width = width;
            _horizonY = horizonY;
            DayAmp = dayAmp;
            _nightAmp = nightAmp;
            _arcs = arcs;

            DayIntervals = [.. arcs.Select(a => (a.Start, a.End))];

            var night = new List<(double, double)>();
            var cursor = 0.0;
            foreach (var arc in arcs)
            {
                if (arc.Start > cursor) night.Add((cursor, arc.Start));
                cursor = arc.End;
            }
            if (cursor < 24.0) night.Add((cursor, 24.0));
            NightIntervals = night;

            if (arcs is [var first, var second]
                && first.Start == 0.0 && first.SetsAtEnd && !first.RisesAtStart
                && second.End == 24.0 && second.RisesAtStart && !second.SetsAtEnd)
            {
                _wrapUp = (second.Start, first.End);
            }
        }

        /// <summary>How far the arc stands above the horizon at its highest.</summary>
        public float DayAmp { get; }

        /// <summary>The spans above the horizon, as hours.</summary>
        public IReadOnlyList<(double From, double To)> DayIntervals { get; }

        /// <summary>The gaps between them and the edges: the stretches below the horizon.</summary>
        public IReadOnlyList<(double From, double To)> NightIntervals { get; }

        public float X(double hour) => (float)(hour / 24.0) * _width;

        public bool IsUp(double hour) => _arcs.Any(a => hour >= a.Start && hour <= a.End);

        public float Y(double hour)
        {
            // A span wrapping midnight is one arch peaking at transit and one night trough, not two
            // half-arcs that each peak at an edge of the chart.
            if (_wrapUp is { } wrap)
            {
                var (rise, set) = wrap;
                if (hour >= rise || hour <= set)
                {
                    var upLength = (24.0 - rise) + set;
                    var progressed = hour >= rise ? hour - rise : (24.0 - rise) + hour;
                    return (float)(_horizonY - DayAmp * Math.Sin(Math.PI * (progressed / upLength)));
                }

                var t = (hour - set) / (rise - set);
                return (float)(_horizonY + _nightAmp * Math.Sin(Math.PI * t));
            }

            foreach (var arc in _arcs)
            {
                if (hour < arc.Start || hour > arc.End) continue;

                var t = Math.Clamp((hour - arc.Start) / (arc.End - arc.Start), 0.0, 1.0);
                var s = (arc.RisesAtStart, arc.SetsAtEnd) switch
                {
                    (true, true) => Math.Sin(Math.PI * t),         // horizon, peak, horizon
                    (true, false) => Math.Sin(Math.PI / 2 * t),    // risen, still climbing at midnight
                    (false, true) => Math.Sin(Math.PI / 2 * (1 - t)), // up at 00:00, coming down to set
                    _ => 0.7 + 0.3 * Math.Sin(Math.PI * t),         // up all day: a gentle high bump
                };
                return (float)(_horizonY - DayAmp * s);
            }

            // On an ordinary day the night is one dip that wraps midnight: at the horizon only at
            // sunset and sunrise, deepest in the middle. Each of [0, rise] and [set, 24] as its own
            // trough would come back up to the horizon at midnight, as if the body set twice.
            if (_arcs is [{ RisesAtStart: true, SetsAtEnd: true } main])
            {
                var nightLength = 24.0 - (main.End - main.Start);
                if (nightLength > 0.0)
                {
                    var progressed = hour >= main.End ? hour - main.End : (24.0 - main.End) + hour;
                    return (float)(_horizonY + _nightAmp * Math.Sin(Math.PI * (progressed / nightLength)));
                }
            }

            foreach (var (from, to) in NightIntervals)
            {
                if (hour < from || hour > to) continue;
                var t = (hour - from) / (to - from);
                return (float)(_horizonY + _nightAmp * Math.Sin(Math.PI * t));
            }

            return _horizonY;
        }

        /// <summary>Points along the curve from one hour to another, six to the hour.</summary>
        public IEnumerable<PointF> Points(double from, double to)
        {
            var segments = Math.Max(2, (int)((to - from) * 6));
            for (var i = 0; i <= segments; i++)
            {
                var hour = from + (to - from) * i / segments;
                yield return new PointF(X(hour), Y(hour));
            }
        }

        public PathF StrokePath(double from, double to)
        {
            var path = new PathF();
            var first = true;
            foreach (var p in Points(from, to))
            {
                if (first) path.MoveTo(p); else path.LineTo(p);
                first = false;
            }
            return path;
        }

        /// <summary>The area between the arc and the horizon.</summary>
        public PathF FillPath(double from, double to)
        {
            var path = new PathF();
            path.MoveTo(X(from), _horizonY);
            foreach (var p in Points(from, to)) path.LineTo(p);
            path.LineTo(X(to), _horizonY);
            path.Close();
            return path;
        }

        /// <summary>
        /// The path for a body that rises at <paramref name="rise"/> and sets at
        /// <paramref name="set"/> (hours, 0..24, either of them null when it does not happen that
        /// day). Null when the body is below the horizon all day.
        /// </summary>
        public static SkyPath? Of(double? rise, double? set, float width, float height, float horizonY, bool alwaysUp)
        {
            Arc[] arcs;
            if (alwaysUp) arcs = [new Arc(0.0, 24.0, false, false)];
            else if (rise is { } r && set is { } s && r < s) arcs = [new Arc(r, s, true, true)];
            // Both, but the set first: the span that set this morning and the one that rose tonight.
            else if (rise is { } r2 && set is { } s2) arcs = [new Arc(0.0, s2, false, true), new Arc(r2, 24.0, true, false)];
            else if (rise is { } r3) arcs = [new Arc(r3, 24.0, true, false)];
            else if (set is { } s3) arcs = [new Arc(0.0, s3, false, true)];
            else return null;

            // The arch and the trough are both half-sines, so where they meet their slopes are
            // amplitude times pi over length. Fixed amplitudes join smoothly only on an equinox;
            // every other day the curve reached the horizon at a visible corner, which is the one
            // thing a body crossing the horizon never does. Matching the slopes puts the amplitudes
            // in proportion to the lengths, and when that does not fit, both shrink together: so a
            // winter day draws a low arch and a summer day a tall one.
            var dayLength = arcs.Sum(a => a.End - a.Start);
            var nightLength = 24.0 - dayLength;
            float dayAmp, nightAmp;
            if (dayLength > 0.5 && nightLength > 0.5)
            {
                var ratio = (float)(nightLength / dayLength);
                var scale = Math.Min(height * 0.30f, height * 0.34f / ratio);
                dayAmp = scale;
                nightAmp = scale * ratio;
            }
            else
            {
                dayAmp = height * 0.27f;
                nightAmp = height * 0.12f;
            }

            return new SkyPath(width, horizonY, dayAmp, nightAmp, arcs);
        }
    }
}
