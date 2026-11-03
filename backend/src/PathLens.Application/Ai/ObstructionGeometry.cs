namespace PathLens.Application.Ai;

/// <summary>
/// A box returned by SAM 3.1 in the pixel space of the image that was sent to it.
/// SAM answers with <c>&lt;|box;x1=..;y1=..;x2=..;y2=..;w=..;h=..|&gt;</c> tokens and no score,
/// so a box is the strongest claim the provider makes on its own.
/// </summary>
public readonly record struct PixelBox(double X1, double Y1, double X2, double Y2, double FrameWidth, double FrameHeight)
{
    public double Width => Math.Max(0, X2 - X1);

    public double Height => Math.Max(0, Y2 - Y1);

    public double Area => Width * Height;
}

/// <summary>
/// Deterministic geometry behind the obstruction share. Kept free of HTTP and JSON so the numbers a
/// judge sees can be reasoned about and unit-tested on their own.
///
/// The share is always expressed against the walkway region SAM was given, never against the whole
/// frame: a photo that shows mostly sky would otherwise report a blockage as trivial.
/// </summary>
public static class ObstructionGeometry
{
    /// <summary>
    /// Parses every <c>&lt;|box;…|&gt;</c> token in a SAM 3.1 text response. Tokens that are missing a
    /// coordinate, or whose numbers do not parse, are skipped rather than guessed at.
    /// </summary>
    public static IReadOnlyList<PixelBox> ParseBoxes(string text)
    {
        var boxes = new List<PixelBox>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return boxes;
        }

        foreach (System.Text.RegularExpressions.Match match in
                 System.Text.RegularExpressions.Regex.Matches(text, "<\\|box;([^|]*)\\|>"))
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in match.Groups[1].Value.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var bits = pair.Split('=', 2);
                if (bits.Length == 2)
                {
                    fields[bits[0].Trim()] = bits[1].Trim();
                }
            }

            if (!fields.TryGetValue("x1", out var x1Text) || !fields.TryGetValue("y1", out var y1Text) ||
                !fields.TryGetValue("x2", out var x2Text) || !fields.TryGetValue("y2", out var y2Text))
            {
                continue;
            }

            if (!double.TryParse(x1Text, out var x1) || !double.TryParse(y1Text, out var y1) ||
                !double.TryParse(x2Text, out var x2) || !double.TryParse(y2Text, out var y2))
            {
                continue;
            }

            double.TryParse(fields.GetValueOrDefault("w"), out var frameWidth);
            double.TryParse(fields.GetValueOrDefault("h"), out var frameHeight);

            boxes.Add(new PixelBox(x1, y1, x2, y2, frameWidth, frameHeight));
        }

        return boxes;
    }

    /// <summary>Clips a box to the bounds of another box, returning null when they do not overlap.</summary>
    public static PixelBox? Clip(PixelBox inner, PixelBox bounds)
    {
        var x1 = Math.Max(inner.X1, bounds.X1);
        var y1 = Math.Max(inner.Y1, bounds.Y1);
        var x2 = Math.Min(inner.X2, bounds.X2);
        var y2 = Math.Min(inner.Y2, bounds.Y2);

        if (x2 <= x1 || y2 <= y1)
        {
            return null;
        }

        return new PixelBox(x1, y1, x2, y2, bounds.Width, bounds.Height);
    }

    /// <summary>
    /// Area of the union of the given boxes, by sweeping their x edges and merging the y intervals that
    /// cover each slab. Overlapping detections therefore count once instead of stacking past 100%.
    /// </summary>
    public static double UnionArea(IReadOnlyList<PixelBox> boxes)
    {
        var usable = boxes.Where(box => box.Width > 0 && box.Height > 0).ToList();
        if (usable.Count == 0)
        {
            return 0;
        }

        var edges = usable.SelectMany(box => new[] { box.X1, box.X2 }).Distinct().OrderBy(x => x).ToArray();
        double total = 0;

        for (var i = 0; i < edges.Length - 1; i++)
        {
            var slabLeft = edges[i];
            var slabRight = edges[i + 1];
            if (slabRight <= slabLeft)
            {
                continue;
            }

            // Every box that spans the whole slab contributes its y range to this slab.
            var intervals = usable
                .Where(box => box.X1 <= slabLeft && box.X2 >= slabRight)
                .Select(box => (box.Y1, box.Y2))
                .OrderBy(range => range.Y1)
                .ToList();

            double covered = 0;
            double openStart = double.NaN;
            double openEnd = double.NaN;

            foreach (var (y1, y2) in intervals)
            {
                if (double.IsNaN(openStart))
                {
                    (openStart, openEnd) = (y1, y2);
                    continue;
                }

                if (y1 <= openEnd)
                {
                    openEnd = Math.Max(openEnd, y2);
                    continue;
                }

                covered += openEnd - openStart;
                (openStart, openEnd) = (y1, y2);
            }

            if (!double.IsNaN(openStart))
            {
                covered += openEnd - openStart;
            }

            total += (slabRight - slabLeft) * covered;
        }

        return total;
    }

    /// <summary>
    /// Share of the walkway region covered by at least one obstacle box, as a whole-number percentage.
    /// Returns null when the measurement cannot be made - no walkway box, no obstacle box, or a walkway
    /// with no area. Null is the honest answer: SAM returning zero obstacle boxes for an image that
    /// plainly contains them (measured: the bare concept "scooter" found nothing on a photo full of
    /// scooters, while "scooters blocking the sidewalk" found two) must never be reported as "0% blocked",
    /// because that is the one reading a wheelchair user would act on.
    /// </summary>
    public static int? WalkwayOccupancyPercent(IReadOnlyList<PixelBox> walkwayBoxes, IReadOnlyList<PixelBox> obstacleBoxes)
    {
        if (walkwayBoxes is null || walkwayBoxes.Count == 0 || obstacleBoxes is null || obstacleBoxes.Count == 0)
        {
            return null;
        }

        // The largest walkway box is the region the person would actually walk along.
        var walkway = walkwayBoxes.OrderByDescending(box => box.Area).First();
        if (walkway.Area <= 0)
        {
            return null;
        }

        var clipped = obstacleBoxes
            .Select(box => Clip(box, walkway))
            .Where(box => box is not null)
            .Select(box => box!.Value)
            .ToList();

        if (clipped.Count == 0)
        {
            return null;
        }

        var occupied = UnionArea(clipped);
        if (occupied <= 0)
        {
            return null;
        }

        var percent = (int)Math.Round(Math.Min(100.0, occupied / walkway.Area * 100.0));
        return percent < 1 ? 1 : percent;
    }
}
