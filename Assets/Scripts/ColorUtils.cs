using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ColorUtils {

    private static Color[] palette = null;

    public static void randomizeColorPalette(Color[] orig, int seed = 44356)
    {
        Random.InitState(seed);
        int size = orig.Length;
        Color[] copyPalette = new Color[size];
        Color[] diffPalette = new Color[size];

        for (int i = 0; i < size; i++) copyPalette[i] = orig[i];

        for (int i = 0; i < size; i++)
        {
            // compute random index
            float r = Random.value;
            int idx = (int)(r * (size - i - 1));
            diffPalette[i] = copyPalette[idx];

            // overwrite the color just placed with the end value
            copyPalette[idx] = copyPalette[size - i - 1];
        }

        for (int i = 0; i < size; i++)
        {
            orig[i] = diffPalette[i];
        }

    }

    public static Color[] getColorPalette()
    {
        if (palette == null)
        {
            palette = new Color[24];

            int i = 0;
            palette[i++] = new Color32(102, 255, 255, 255);
            palette[i++] = new Color32(153, 255, 102, 255);
            palette[i++] = new Color32(81, 194, 1, 255);
            palette[i++] = new Color32(60, 208, 112, 255);
            palette[i++] = new Color32(133, 187, 101, 255);
            palette[i++] = new Color32(126, 145, 86, 255);
            palette[i++] = new Color32(33, 171, 205, 255);
            palette[i++] = new Color32(0, 134, 167, 255);
            palette[i++] = new Color32(153, 204, 255, 255);
            palette[i++] = new Color32(255, 0, 255, 255);
            palette[i++] = new Color32(188, 108, 172, 255);
            palette[i++] = new Color32(153, 102, 204, 255);
            palette[i++] = new Color32(153, 153, 255, 255);
            palette[i++] = new Color32(153, 102, 255, 255);
            palette[i++] = new Color32(255, 0, 153, 255);
            palette[i++] = new Color32(255, 151, 0, 255);
            palette[i++] = new Color32(255, 204, 153, 255);
            palette[i++] = new Color32(191, 106, 31, 255);
            palette[i++] = new Color32(255, 128, 0, 255);
            palette[i++] = new Color32(185, 150, 133, 255);
            palette[i++] = new Color32(255, 255, 153, 255);
            palette[i++] = new Color32(167, 139, 0, 255);
            palette[i++] = new Color32(226, 182, 49, 255);
            palette[i++] = new Color32(255, 255, 51, 255);
        }

        return palette;
    }

    
}