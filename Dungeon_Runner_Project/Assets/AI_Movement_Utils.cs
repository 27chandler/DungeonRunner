using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AI_Movement_Utils
{
    // General function for steering an object towards another
    public static void Seek_Target(Rigidbody2D i_rb, Vector2 i_target, float i_force)
    {
        Vector2 steering_dir = (i_target - i_rb.position).normalized;
        i_rb.AddForce(steering_dir * i_force);
    }

    // General function for steering an object away from another
    public static void Flee_Target(Rigidbody2D i_rb, Vector2 i_target, float i_force)
    {
        Vector2 steering_dir = (i_rb.position - i_target).normalized;
        i_rb.AddForce(steering_dir * i_force);
    }
}
