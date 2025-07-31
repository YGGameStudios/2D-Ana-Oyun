using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 public enum BackgroundTypee { Blue, Brown,Gray,Green,Pink,Purple,Yellow }

public class quadAnimate : MonoBehaviour
{
    public Vector2 moveDirection;
    private MeshRenderer mesh;

    [Header("Color")]
    [SerializeField] private BackgroundTypee backgroundTypeee;

    [SerializeField] private Texture2D[] texturess;
    private void Awake()
    {
        mesh = GetComponent<MeshRenderer>();
        UpdateBackgroundTexture(); 
    }

    private void Update()
    {
        mesh.material.mainTextureOffset += moveDirection * Time.deltaTime;
    }

    [ContextMenu("Upgrade BackGround")]
    private void UpdateBackgroundTexture()
    {
        if(mesh == null)
            mesh = GetComponent<MeshRenderer>();

        mesh.material.mainTexture = texturess[((int)backgroundTypeee)];
    }
}
