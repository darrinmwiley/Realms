using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface LPos<P>
{
    Vector3 GetPositionLocal(P context);
    Vector3 GetPositionAbsolute(P context);
    Vector3 GetDirectionLocal(P context);
    Vector3 GetDirectionAbsolute(P context);
}

public interface LUpdate<M>
{
    void Update(M context);
}

public class LSystemV2<P, M> : LPos<P>, LUpdate<M>
{
    public GameObject gameObject;
    public LSystemMonoV2 mono;

    public LSystemV2()
    {
        this.gameObject = new GameObject();
        gameObject.name = "L-System V2";
    }

    public virtual Vector3 GetPositionLocal(P context)
    {
        // Implementation for getting the local position
        throw new NotImplementedException("GetPositionLocal not implemented yet!");
    }

    public virtual Vector3 GetPositionAbsolute(P context)
    {
        Vector3 localPosition = GetPositionLocal(context);
        return gameObject.transform.TransformPoint(localPosition);
    }

    public virtual Vector3 GetDirectionLocal(P context)
    {
        // Implementation for getting the local direction
        throw new NotImplementedException("GetDirectionLocal not implemented yet!");
    }

    public virtual Vector3 GetDirectionAbsolute(P context)
    {
        throw new NotImplementedException("GetDirectionAbsolute not implemented yet!");
    }

    public virtual void Update(M context){}
}