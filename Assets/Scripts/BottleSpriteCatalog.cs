using UnityEngine;

namespace BottleBattle
{
    /// <summary>
    /// Maps the 20 transparent bottle artworks to their exact alpha bounds.
    /// Using measured bounds prevents atlas seams and preserves each silhouette's aspect ratio.
    /// </summary>
    public static class BottleSpriteCatalog
    {
        public const int BottleCount = 20;

        private const float AtlasWidth = 2172f;
        private const float AtlasHeight = 724f;
        private const int PrimaryCount = 7;

        private static readonly RectInt[] PrimaryBounds =
        {
            new(109, 72, 194, 591),
            new(385, 78, 223, 584),
            new(705, 74, 174, 589),
            new(982, 113, 205, 550),
            new(1290, 82, 214, 581),
            new(1606, 79, 194, 584),
            new(1897, 77, 195, 586)
        };

        private static readonly RectInt[] ExtraBounds =
        {
            new(39, 152, 135, 426),
            new(213, 197, 179, 381),
            new(427, 185, 112, 393),
            new(587, 175, 112, 403),
            new(751, 201, 148, 377),
            new(939, 185, 119, 393),
            new(1097, 260, 140, 318),
            new(1276, 197, 120, 381),
            new(1429, 175, 128, 403),
            new(1590, 192, 120, 386),
            new(1739, 207, 122, 371),
            new(1891, 197, 114, 381),
            new(2034, 226, 114, 352)
        };

        private static Texture2D primaryAtlas;
        private static Texture2D extraAtlas;
        private static readonly Texture2D[] GrayscaleBottles = new Texture2D[BottleCount];

        public static bool TryGet(
            int identity,
            out Texture2D texture,
            out Rect textureCoordinates,
            out float aspectRatio)
        {
            int normalizedIdentity = Mathf.Abs(identity) % BottleCount;
            RectInt bounds;

            if (normalizedIdentity < PrimaryCount)
            {
                primaryAtlas ??= Resources.Load<Texture2D>("Bottles/generic-soda-bottles");
                texture = primaryAtlas;
                bounds = PrimaryBounds[normalizedIdentity];
            }
            else
            {
                extraAtlas ??= Resources.Load<Texture2D>("Bottles/generic-soda-bottles-extra");
                texture = extraAtlas;
                bounds = ExtraBounds[normalizedIdentity - PrimaryCount];
            }

            if (texture == null)
            {
                textureCoordinates = default;
                aspectRatio = 1f;
                return false;
            }

            textureCoordinates = new Rect(
                bounds.x / AtlasWidth,
                (AtlasHeight - bounds.yMax) / AtlasHeight,
                bounds.width / AtlasWidth,
                bounds.height / AtlasHeight);
            aspectRatio = bounds.width / (float)bounds.height;
            return true;
        }

        public static bool TryGetGrayscale(
            int identity,
            out Texture2D texture,
            out Rect textureCoordinates,
            out float aspectRatio)
        {
            int normalizedIdentity = Mathf.Abs(identity) % BottleCount;
            if (!TryGet(normalizedIdentity, out Texture2D atlas, out Rect atlasCoordinates, out aspectRatio))
            {
                texture = null;
                textureCoordinates = default;
                return false;
            }

            texture = GrayscaleBottles[normalizedIdentity];
            if (texture == null)
            {
                int x = Mathf.RoundToInt(atlasCoordinates.x * atlas.width);
                int y = Mathf.RoundToInt(atlasCoordinates.y * atlas.height);
                int width = Mathf.Max(1, Mathf.RoundToInt(atlasCoordinates.width * atlas.width));
                int height = Mathf.Max(1, Mathf.RoundToInt(atlasCoordinates.height * atlas.height));
                Color[] pixels = atlas.GetPixels(x, y, width, height);

                var silhouettePixels = new Color[pixels.Length];
                Color silhouetteColor = new(0.055f, 0.065f, 0.075f, 1f);
                for (int row = 0; row < height; row++)
                {
                    int leftEdge = width;
                    int rightEdge = -1;
                    for (int column = 0; column < width; column++)
                    {
                        if (pixels[row * width + column].a <= 0.035f)
                        {
                            continue;
                        }

                        leftEdge = Mathf.Min(leftEdge, column);
                        rightEdge = Mathf.Max(rightEdge, column);
                    }

                    // Fill between the outermost opaque pixels. Some source labels
                    // contain transparent bands, so recoloring alone can leak them.
                    for (int column = leftEdge; column <= rightEdge; column++)
                    {
                        silhouettePixels[row * width + column] = silhouetteColor;
                    }
                }

                texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = $"Bottle-{normalizedIdentity}-Grayscale",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixels(silhouettePixels);
                texture.Apply(false, true);
                GrayscaleBottles[normalizedIdentity] = texture;
            }

            textureCoordinates = new Rect(0f, 0f, 1f, 1f);
            return true;
        }
    }
}
