using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Last update: 7/11/2021
/// Version: 1.0
/// </summary>
namespace com.TFTEstherZC.SharedARModuleV2
{
    public interface TransformSynchronization
    {
        void UpdateTransform(object[] newTransform);
    }
}