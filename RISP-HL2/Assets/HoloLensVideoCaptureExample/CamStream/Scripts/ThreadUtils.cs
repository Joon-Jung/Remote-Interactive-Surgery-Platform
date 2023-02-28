//  
// Copyright (c) 2017 Vulcan, Inc. All rights reserved.  
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreadUtils : MonoBehaviour
{
	private object _methodsLock = new object();
	List<Action> _methodsToInvoke = new List<Action>();

	static ThreadUtils instance;
    public static ThreadUtils Instance
    {
        get
        {
            return instance;
        }
    }

	/// <summary>
	/// Some functions in the UnityEngine namespace can only be invoked on the Main thread.
	/// Some CameraStream processes happen on a different thread, so when we need to do some Unity stuff,
	/// we can invoke them using this utility method.
	/// </summary>
	public void InvokeOnMainThread(Action method)
	{
		lock(_methodsLock)
		{
			_methodsToInvoke.Add(method);
		}
	}

	private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Cannot create two instances of ThreadUtils.");
            return;
        }

        instance = this;
    }

	void Update()
	{
		lock(_methodsLock)
		{
			foreach (Action method in _methodsToInvoke)
			{
				method();
			}
			_methodsToInvoke.Clear();
		}
	}
}
