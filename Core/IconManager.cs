namespace WhatAreThose;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using UnityEngine;

static class IconManager {
  static readonly Dictionary<string, string> _iconResources = new() {
    ["IceShoes"] = "WhatAreThose.Assets.iceshoes.png",
    ["IceSkates"] = "WhatAreThose.Assets.iceskates.png",
  };

  static readonly Dictionary<string, Sprite> _sprites = [];

  // The Unity 6 image-conversion module targets netstandard2.1, which a net48 plugin cannot reference directly.
  static readonly MethodInfo _loadImageMethod = GetLoadImageMethod();

  public static void ReplaceIcons(ObjectDB objectDB) {
    foreach (KeyValuePair<string, string> entry in _iconResources) {
      ReplaceIcon(objectDB, entry.Key, entry.Value);
    }
  }

  static void ReplaceIcon(ObjectDB objectDB, string prefabName, string resourceName) {
    GameObject prefab = objectDB.GetItemPrefab(prefabName);

    if (!prefab) {
      WhatAreThose.LogWarning($"Could not find prefab '{prefabName}'.");
      return;
    }

    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

    if (!itemDrop || itemDrop.m_itemData == null || itemDrop.m_itemData.m_shared == null) {
      WhatAreThose.LogWarning($"Prefab '{prefabName}' has no item data.");
      return;
    }

    Sprite[] icons = itemDrop.m_itemData.m_shared.m_icons;

    if (icons == null || icons.Length == 0) {
      WhatAreThose.LogWarning($"Prefab '{prefabName}' has no icon slot.");
      return;
    }

    Sprite sprite = GetOrLoadSprite(prefabName, resourceName, icons[0]);

    if (!sprite) {
      return;
    }

    Sprite[] newIcons = new Sprite[icons.Length];

    for (int index = 0; index < newIcons.Length; index++) {
      newIcons[index] = sprite;
    }

    itemDrop.m_itemData.m_shared.m_icons = newIcons;

    WhatAreThose.LogInfo($"Replaced icon for '{prefabName}'.");
  }

  static Sprite GetOrLoadSprite(string prefabName, string resourceName, Sprite source) {
    if (_sprites.TryGetValue(prefabName, out Sprite sprite)) {
      return sprite;
    }

    using Stream stream = typeof(IconManager).Assembly.GetManifestResourceStream(resourceName);

    if (stream == null) {
      WhatAreThose.LogWarning($"Embedded icon '{resourceName}' is missing.");
      return null;
    }

    using MemoryStream memoryStream = new();
    stream.CopyTo(memoryStream);

    Texture2D texture = new(2, 2, TextureFormat.RGBA32, mipChain: false);

    if (!LoadPng(texture, memoryStream.ToArray())) {
      UnityEngine.Object.Destroy(texture);
      WhatAreThose.LogWarning($"Could not decode embedded icon '{resourceName}'.");
      return null;
    }

    texture.name = $"{prefabName}_Icon";
    texture.filterMode = FilterMode.Bilinear;
    texture.wrapMode = TextureWrapMode.Clamp;

    float pixelsPerUnit = source ? source.pixelsPerUnit : 100f;

    sprite =
        Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit,
            0u,
            SpriteMeshType.FullRect);

    sprite.name = $"{prefabName}_Icon";
    _sprites[prefabName] = sprite;

    return sprite;
  }

  static bool LoadPng(Texture2D texture, byte[] pngBytes) {
    if (_loadImageMethod == null) {
      WhatAreThose.LogWarning("Could not find UnityEngine.ImageConversion.LoadImage().");
      return false;
    }

    try {
      return (bool) _loadImageMethod.Invoke(null, [texture, pngBytes, true]);
    } catch (Exception exception) {
      WhatAreThose.LogWarning($"Could not decode the embedded icon: {exception.Message}");
      return false;
    }
  }

  static MethodInfo GetLoadImageMethod() {
    try {
      Assembly imageConversionAssembly =
          Array.Find(
              AppDomain.CurrentDomain.GetAssemblies(),
              assembly => assembly.GetName().Name == "UnityEngine.ImageConversionModule");

      if (imageConversionAssembly == null) {
        imageConversionAssembly = Assembly.Load("UnityEngine.ImageConversionModule");
      }

      Type imageConversionType = imageConversionAssembly.GetType("UnityEngine.ImageConversion");

      return imageConversionType?.GetMethod(
          "LoadImage",
          BindingFlags.Public | BindingFlags.Static,
          null,
          [typeof(Texture2D), typeof(byte[]), typeof(bool)],
          null);
    } catch (Exception exception) {
      WhatAreThose.LogWarning($"Could not load the image conversion module: {exception.Message}");
      return null;
    }
  }
}
