using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class MathUtil
{
    #region 角度弧度转换
    /// <summary>
    /// 将角度转换为弧度
    /// </summary>
    /// <param name="deg">角度</param>
    /// <returns>弧度</returns>
    public static float Deg2Rad(float deg)
    {
        return deg * Mathf.Deg2Rad;
    }

    /// <summary>
    /// 将弧度转换为角度
    /// </summary>
    /// <param name="rad"></param>
    /// <returns></returns>
    public static float Rad2Deg(float rad)
    {
        return rad * Mathf.Rad2Deg;
    }
    #endregion

    #region 距离判断函数
    /// <summary>
    /// 获取两个物体在 XZ 平面上的距离
    /// </summary>
    /// <param name="startPos">起始点</param>
    /// <param name="targetPos">目标点</param>
    /// <returns>两点在 XZ 平面上的距离</returns>
    public static float GetObjDistanceXZ(Vector3 startPos,Vector3 targetPos)
    {
        startPos.y = 0;
        targetPos.y = 0;
        return Vector3.Distance(startPos, targetPos);
    }

    /// <summary>
    /// 获取两个物体在 XZ 平面上的距离 是否小于等于指定距离
    /// </summary>
    /// <param name="startPos">起始点</param>
    /// <param name="targetPos">目标点</param>
    /// <param name="distance">指定距离范围</param>
    /// <returns>两点在 XZ 平面上的距离 是否小于等于指定距离</returns>
    public static bool CheckObjDistanceXZ(Vector3 startPos, Vector3 targetPos, float distance)
    {
        return GetObjDistanceXZ(startPos, targetPos) <= distance;
    }

    /// <summary>
    /// 获取两个物体在 XY 平面上的距离
    /// </summary>
    /// <param name="startPos">起始点</param>
    /// <param name="targetPos">目标点</param>
    /// <returns>两点在 XY 平面上的距离</returns>
    public static float GetObjDistanceXY(Vector3 startPos, Vector3 targetPos)
    {
        startPos.z = 0;
        targetPos.z = 0;
        return Vector3.Distance(startPos, targetPos);
    }

    /// <summary>
    /// 获取两个物体在 XY 平面上的距离 是否小于等于指定距离
    /// </summary>
    /// <param name="startPos">起始点</param>
    /// <param name="targetPos">目标点</param>
    /// <param name="distance">指定距离范围</param>
    /// <returns>两点在 XY 平面上的距离 是否小于等于指定距离</returns>
    public static bool CheckObjDistanceXY(Vector3 startPos, Vector3 targetPos, float distance)
    {
        return GetObjDistanceXY(startPos, targetPos) <= distance;
    }
    #endregion

    #region 屏幕内判断
    /// <summary>
    /// 判断世界坐标位置是否超出屏幕显示范围的方法
    /// </summary>
    /// <param name="worldPos">世界坐标</param>
    /// <param name="camera">相机(默认使用主相机)</param>
    /// <returns>世界坐标位置是否超出屏幕显示范围</returns>
    public static bool IsWorldPositionOutOfScreen(Vector3 worldPos, Camera camera)
    {
        Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
        //判断屏幕坐标是否超出屏幕范围
        if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
            return true;
        return false;
    }
    #endregion

    #region 范围判断函数
    /// <summary>
    /// 判断目标 是否在 扇形范围内 XZ平面
    /// </summary>
    /// <param name="startPos">起始点</param>
    /// <param name="targetPos">目标点</param>
    /// <param name="forward">起始点的前向(朝向)</param>
    /// <param name="angle">扇形范围的角度</param>
    /// <param name="distance">扇形范围半径</param>
    /// <returns>目标 是否在 扇形范围内 XZ平面</returns>
    public static bool IsInSectorRangeXZ(Vector3 startPos,Vector3 targetPos ,Vector3 forward, float angle, float distance)
    {
        startPos.y = 0;
        targetPos.y = 0;
        forward.y = 0;
        return CheckObjDistanceXZ(startPos, targetPos, distance) && Vector3.Angle(forward,targetPos-startPos) <= angle/2 ;
    }

    /// <summary>
    /// 判断目标 是否在 扇形范围内 XY平面
    /// </summary>
    /// <param name="startPos">起始点</param>
    /// <param name="targetPos">目标点</param>
    /// <param name="forward">起始点的前向(朝向)</param>
    /// <param name="angle">扇形范围的角度</param>
    /// <param name="distance">扇形范围半径</param>
    /// <returns>目标 是否在 扇形范围内 XY平面</returns>
    public static bool IsInSectorRangeXY(Vector3 startPos, Vector3 targetPos, Vector3 forward, float angle, float distance)
    {
        startPos.z = 0;
        targetPos.z = 0;
        forward.z = 0;
        return CheckObjDistanceXZ(startPos, targetPos, distance) && Vector3.Angle(forward, targetPos - startPos) <= angle / 2;
    }
    #endregion

    #region 3D射线检测
    /// <summary>
    /// 单个射线检测
    /// </summary>
    /// <typeparam name="T">射线检测的对象类型</typeparam>
    /// <param name="ray">射线</param>
    /// <param name="callback">回调方法(将射线检测到的信息传出去使用)</param>
    /// <param name="distance">检测距离</param>
    /// <param name="layermask">层级掩码</param>
    public static void RayCast<T>(Ray ray,UnityAction<T> callback,float distance,LayerMask layermask) where T : class
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, distance, layermask))
        {
            if (typeof(T) == typeof(GameObject))
                callback?.Invoke(hitInfo.collider.gameObject as T);
            else if (typeof(T) == typeof(Collider))
                callback?.Invoke(hitInfo.collider as T);
            else
                callback?.Invoke(hitInfo.collider?.GetComponent<T>());
        }
    }

    /// <summary>
    /// 射线检测多个
    /// </summary>
    /// <typeparam name="T">射线检测的对象类型</typeparam>
    /// <param name="ray">射线</param>
    /// <param name="callback">回调方法 用来把检测信息传出去使用</param>
    /// <param name="distance">检测距离</param>
    /// <param name="layMask">层级掩码</param>
    public static void RayCastAll<T>(Ray ray,UnityAction<T> callback,float distance, LayerMask layMask) where T : class
    {
        RaycastHit[] hitsInfo = Physics.RaycastAll(ray, distance, layMask);
        foreach (RaycastHit info in hitsInfo)
        {
            if (typeof(T) == typeof(GameObject))
                callback?.Invoke(info.collider.gameObject as T);
            else if (typeof(T) == typeof(Collider))
                callback?.Invoke(info.collider as T);
            else
                callback?.Invoke(info.collider?.GetComponent<T>());
        }
    }
    #endregion

    #region 2D射线检测
    /// <summary>
    /// 2D 单个射线检测
    /// </summary>
    public static void RayCast2D<T>(Ray ray, UnityAction<T> callback, float distance, LayerMask layermask) where T : class
    {
        RaycastHit2D hitInfo = Physics2D.Raycast(ray.origin, ray.direction, distance, layermask);
        if (hitInfo.collider != null)
        {
            if (typeof(T) == typeof(GameObject))
                callback?.Invoke(hitInfo.collider.gameObject as T);
            else if (typeof(T) == typeof(Collider2D))
                callback?.Invoke(hitInfo.collider as T);
            else
                callback?.Invoke(hitInfo.collider.GetComponent<T>());
        }
    }

    /// <summary>
    /// 2D 射线检测多个
    /// </summary>
    public static void RayCastAll2D<T>(Ray ray, UnityAction<T> callback, float distance, LayerMask layermask) where T : class
    {
        RaycastHit2D[] hitsInfo = Physics2D.RaycastAll(ray.origin, ray.direction, distance, layermask);
        foreach (RaycastHit2D info in hitsInfo)
        {
            if (info.collider == null) continue;

            if (typeof(T) == typeof(GameObject))
                callback?.Invoke(info.collider.gameObject as T);
            else if (typeof(T) == typeof(Collider2D))
                callback?.Invoke(info.collider as T);
            else
                callback?.Invoke(info.collider.GetComponent<T>());
        }
    }
    #endregion

    #region 范围碰撞检测
    /// <summary>
    /// 盒形范围检测
    /// </summary>
    /// <typeparam name="T">检测目标</typeparam>
    /// <param name="position">盒形范围的中心位置</param>
    /// <param name="halfExtents">盒形范围的长宽高的一半</param>
    /// <param name="rotation">盒形范围的角度四元数</param>
    /// <param name="layermask">层级掩码</param>
    /// <param name="callback">回调方法</param>
    public static void OverlapBox<T>(Vector3 position,Vector3 halfExtents,Quaternion rotation,LayerMask layermask,UnityAction<T> callback)where T : class
    {
        Collider[] colliders = Physics.OverlapBox(position, halfExtents, rotation, layermask);
        foreach (Collider collider in colliders)
        {
            if (typeof(T) == typeof(GameObject))
                callback?.Invoke(collider.gameObject as T);
            else if (typeof(T) == typeof(Collider))
                callback?.Invoke(collider as T);
            else
                callback?.Invoke(collider?.GetComponent<T>());
        }
    }

    /// <summary>
    /// 球形范围检测
    /// </summary>
    /// <typeparam name="T">检测对象</typeparam>
    /// <param name="position">球形范围的中心</param>
    /// <param name="radius">球形范围的半径</param>
    /// <param name="quaternion">球形范围角度四元数</param>
    /// <param name="layermask">层级掩码</param>
    /// <param name="callback">回调方法</param>
    public static void OverlapSphere<T>(Vector3 position,float radius,Quaternion quaternion,LayerMask layermask,UnityAction<T> callback) where T : class
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius, layermask);
        foreach (Collider collider in colliders)
        {
            if (typeof(T) == typeof(GameObject))
                callback?.Invoke(collider.gameObject as T);
            else if (typeof(T) == typeof(Collider))
                callback?.Invoke(collider as T);
            else
                callback?.Invoke(collider?.GetComponent<T>());
        }
    }
    #endregion
}
