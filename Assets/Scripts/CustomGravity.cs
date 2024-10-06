using System.Collections.Generic;
using UnityEngine;

public static class CustomGravity
{
    private static List<GravitySource> sources = new List<GravitySource>();

    public static Vector3 GetGravity(Vector3 _position)
    {
        Vector3 g = Vector3.zero;

        for (int i = 0; i < sources.Count; i++)
        {
            g += sources[i].GetGravity(_position);
        }

        return g;
    }

    public static Vector3 GetUpAxis(Vector3 _position)
    {
        Vector3 g = Vector3.zero;

        for (int i = 0; i < sources.Count; i++)
        {
            g += sources[i].GetGravity(_position);
        }

        return -g.normalized;
    }

    public static Vector3 GetGravity(Vector3 _position, out Vector3 _upAxis)
    {
        Vector3 g = Vector3.zero;

        for (int i = 0; i < sources.Count; i++)
        {
            g += sources[i].GetGravity(_position);
        }

        _upAxis = -g.normalized;

        return g;
    }

    public static void Register(GravitySource _source)
    {
        Debug.Assert(!sources.Contains(_source), "Duplicate registration of gravity source !", _source);
        
        sources.Add(_source);
    }

    public static void Unregister(GravitySource _source)
    {
        Debug.Assert(sources.Contains(_source), "Unregistration of unknown gravity source !", _source);

        sources.Remove(_source);
    }
}