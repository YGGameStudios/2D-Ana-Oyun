using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BackgroundType { Blue,Brown,Gray,Green,Pink,Purple,Yellow}

public class AnimatedBackGround : MonoBehaviour
{
    private MeshRenderer mesh;
    [SerializeField] private Vector2 movementDirection;

    [Header("Color")]
    [SerializeField] private BackgroundType backgroundType;

    [SerializeField] private Texture2D[] textures;

    private void Awake()
    {
        mesh = GetComponent<MeshRenderer>();
        UpdateBackgroundTexture();
    }

    private void Update()
    {
        mesh.material.mainTextureOffset += movementDirection * Time.deltaTime;
    }

    [ContextMenu("Update Background")]
    private void UpdateBackgroundTexture()
    {
        if(mesh == null)
            mesh = GetComponent<MeshRenderer>();

        mesh.sharedMaterial.mainTexture = textures[((int)backgroundType)];
    }
}
