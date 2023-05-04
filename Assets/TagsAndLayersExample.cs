using System.Collections.ObjectModel;
using UnityEngine;
using UniTyped.Reflection;

public class TagsAndLayersExample : MonoBehaviour
{
    private void Start()
    {
        // ---Tags---
        
        Debug.Log(Tags.New_tag);
        
        // tag names can be queried with UniTyped.Reflection.TagUtility
        Debug.Log(TagUtility.GetTagName(Tags.New_tag)); // "New tag"
        TagUtility.TryGetTagValue("New tag", out Tags result); // result: Tags.New_tag
        ReadOnlyCollection<string> tagNames = TagUtility.TagNames; // enumerate tag names
        
        
        // ---Layers---
        
        Debug.Log(Layers.Default);
        Debug.Log(Layers.UI);
        Debug.Log(Layers.Water);
        Debug.Log(Layers.Ignore_Raycast);
        Debug.Log(Layers.TransparentFX);
        
        // layer enum values can be used as layer indices.
        Debug.Log(LayerMask.LayerToName((int)Layers.Default));
        
        
        // ---Sorting Layers---
        
        Debug.Log(SortingLayers.Default);
        
        // sorting layer enum values can be used as sorting layer IDs.
        SortingLayer.GetLayerValueFromID((int)SortingLayers.Default);
    }
}