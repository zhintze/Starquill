namespace Starquill.UI
{
    public static class ParallaxMath
    {
        public static float CalculateUVWidth(float displayWidth, float textureWidth)
        {
            if (textureWidth <= 0f) return 1f;
            return displayWidth / textureWidth;
        }

        public static float AdvanceOffset(float currentOffset, float scrollSpeed, float deltaTime, float textureWidth)
        {
            if (textureWidth <= 0f) return currentOffset;
            float newOffset = currentOffset + scrollSpeed * deltaTime / textureWidth;
            newOffset %= 1f;
            if (newOffset < 0f) newOffset += 1f;
            return newOffset;
        }
    }
}
