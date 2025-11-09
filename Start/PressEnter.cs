using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

public class PressEnter : MonoBehaviour {
    public string next_scene;
    void Start() {
        
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.X) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {            
            TransitionService.LoadScene(next_scene);
        }
    }
}
