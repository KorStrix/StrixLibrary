﻿#region Header
/*	============================================
 *	작성자 : Strix
 *	작성일 : 2018-05-22 오후 12:18:11
 *	기능 : 
   ============================================ */
#endregion Header

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine.TestTools;
#endif

public class CManagerInputEventSystem : CSingletonMonoBase<CManagerInputEventSystem>
{
    /* const & readonly declaration             */

    /* enum & struct declaration                */

    /* public - Field declaration            */

    [Rename_Inspector("이벤트 카메라")]
    public Camera p_pEventCamera;
    [Rename_Inspector("히트할 레이어")]
    public LayerMask p_pLayerMask_Hit;
    [Rename_Inspector("Is 2D")]
    public bool p_bIs2D;

    [Space(10)]
    [Rename_Inspector("히트 어레이 Capacity")]
    public int p_iHitArrayCapapcity = 10;

    public List<CRaycastHitWrapper> p_listLastHit { get; private set; }

    /* protected & private - Field declaration         */

    List<Transform> _listTransform_EnterAlready = new List<Transform>();
    List<Transform> _listTransform_EnterNew = new List<Transform>();
    List<Transform> _listTransform_ExitEnter = new List<Transform>();

    List<RaycastHit2D> _listHit_2D = new List<RaycastHit2D>();

    RaycastHit2D[] _arrHit_2D;
    RaycastHit[] _arrHit;

    Ray pRay_OnClick_ForDebug;
    int _iLastHitCount;

    // ========================================================================== //

    /* public - [Do] Function
     * 외부 객체가 호출(For External class call)*/

    public Vector3 DoGetMousePos()
    {
        return p_pEventCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, p_pEventCamera.nearClipPlane));
    }

    public Vector3 DoRayCasting_MousePos_3D(Camera pCamera, LayerMask sLayerMask_Hit)
    {
        RaycastHit pHitInfo;
        bool bIsHit = Physics.Raycast(pCamera.ScreenPointToRay(Input.mousePosition), out pHitInfo, Mathf.Infinity, sLayerMask_Hit);

        if (CheckDebugFilter(EDebugFilter.Debug_Level_Core))
        {
            Ray pRay = pCamera.ScreenPointToRay(Input.mousePosition);
            if (bIsHit)
                Debug.DrawRay(pRay.origin, (Vector3)pHitInfo.point - pRay.origin, Color.red, 1f);
            else
                Debug.DrawRay(pRay.origin, pRay.direction * 1000f, Color.green, 1f);
        }

        if (bIsHit)
            return pHitInfo.point;
        else
            return Vector3.zero;
    }

    public Vector3 DoRayCasting_MousePos_2D(Camera pCamera, LayerMask sLayerMask_Hit)
    {
        Ray pRay = pCamera.ScreenPointToRay(Input.mousePosition);
        var pHitInfo = Physics2D.GetRayIntersection(pRay, Mathf.Infinity, sLayerMask_Hit);

        if (CheckDebugFilter(EDebugFilter.Debug_Level_Core))
        {
            if (pHitInfo)
                Debug.DrawRay(pRay.origin, (Vector3)pHitInfo.point - pRay.origin, Color.red, 1f);
            else
                Debug.DrawRay(pRay.origin, pRay.direction * 1000f, Color.green, 1f);
        }

        if (pHitInfo)
            return pHitInfo.point;
        else
            return Vector3.zero;
    }

    public List<RaycastHit2D> DoRayCasting_2D(LayerMask sLayerMask_Hit)
    {
        int iHitCount = Physics2D.GetRayIntersectionNonAlloc(p_pEventCamera.ScreenPointToRay(Input.mousePosition), _arrHit_2D, Mathf.Infinity, sLayerMask_Hit);

        _listHit_2D.Clear();
        for (int i = 0; i < iHitCount; i++)
            _listHit_2D.Add(_arrHit_2D[i]);

        return _listHit_2D;
    }


    public Vector3 DoRayCasting_MousePos_2D(LayerMask sLayerMask_Hit)
    {
        return DoRayCasting_MousePos_2D(p_pEventCamera, sLayerMask_Hit);
    }

    public Vector3 DoRayCasting_MousePos_3D(LayerMask sLayerMask_Hit)
    {
        return DoRayCasting_MousePos_3D(p_pEventCamera, sLayerMask_Hit);
    }


    public Vector3 DoRayCasting_MousePos()
    {
        var pHitInfo = Physics2D.GetRayIntersection(p_pEventCamera.ScreenPointToRay(Input.mousePosition), Mathf.Infinity);
        if (pHitInfo)
            return pHitInfo.point;
        else
            return Vector3.zero;
    }

    // ========================================================================== //

    /* protected - Override & Unity API         */

    protected override void OnAwake()
    {
        base.OnAwake();

        p_listLastHit = new List<CRaycastHitWrapper>();
        InitCamera();

        _arrHit_2D = new RaycastHit2D[p_iHitArrayCapapcity];
        _arrHit = new RaycastHit[p_iHitArrayCapapcity];
    }

    protected override IEnumerator OnEnableObjectCoroutine()
    {
        yield return null;

        while (true)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            CalculateInputEvent_OnPC();
#elif UNITY_ANDROID
            CalculateInputEvent_OnMobile();
#endif

            yield return null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (CheckDebugFilter(EDebugFilter.Debug_Level_Core) == false)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(pRay_OnClick_ForDebug.origin, pRay_OnClick_ForDebug.origin + (pRay_OnClick_ForDebug.direction * Mathf.Infinity));
        if (p_bIs2D)
        {
            for (int i = 0; i < _iLastHitCount; i++)
            {
                RaycastHit2D pHit = _arrHit_2D[i];
                Gizmos.DrawSphere(pHit.point, 1f);
            }
        }
        else
        {
            for (int i = 0; i < _iLastHitCount; i++)
            {
                RaycastHit pHit = _arrHit[i];
                Gizmos.DrawSphere(pHit.point, 1f);
            }
        }
    }
#endif

    /* protected - [abstract & virtual]         */


    // ========================================================================== //

    #region Private

    private void CalculateInputEvent_OnMobile()
    {
        p_listLastHit.Clear();
        _listTransform_EnterNew.Clear();
        _listTransform_ExitEnter.Clear();

        for (int j = 0; j < Input.touchCount; j++)
        {
            Touch pTouch = Input.GetTouch(j);
            bool bIsClick = pTouch.phase == TouchPhase.Ended;

            if (p_bIs2D)
                _iLastHitCount = Physics2D.GetRayIntersectionNonAlloc(p_pEventCamera.ScreenPointToRay(Input.mousePosition), _arrHit_2D, Mathf.Infinity, p_pLayerMask_Hit.value);
            else
                _iLastHitCount = Physics.RaycastNonAlloc(p_pEventCamera.ScreenPointToRay(Input.mousePosition), _arrHit, Mathf.Infinity, p_pLayerMask_Hit.value);

            for (int i = 0; i < _iLastHitCount; i++)
            {
                CRaycastHitWrapper pHit = _arrHit[i];
                p_listLastHit.Add(pHit);
                Transform pTransformHit = pHit.transform;

                if (CheckDebugFilter(EDebugFilter.Debug_Level_Core))
                    Debug.Log(pTransformHit.name + " RayCast Hit bMouseClick: " + bIsClick, pTransformHit);

                if (bIsClick)
                {
                    var pClick = pTransformHit.GetComponent<IPointerClickHandler>();
                    if (pClick != null)
                        pClick.OnPointerClick(null);
                }

                _listTransform_EnterNew.Add(pHit.transform);
            }

            for (int i = 0; i < _listTransform_EnterAlready.Count; i++)
            {
                Transform pTransform = _listTransform_EnterAlready[i];
                if (_listTransform_EnterNew.Contains(pTransform))
                    _listTransform_EnterNew.Remove(pTransform);
                else
                    _listTransform_ExitEnter.Add(pTransform);
            }

            for (int i = 0; i < _listTransform_EnterNew.Count; i++)
            {
                var pEnter = _listTransform_EnterNew[i].GetComponent<IPointerEnterHandler>();
                if (pEnter != null)
                    pEnter.OnPointerEnter(null);
            }

            for (int i = 0; i < _listTransform_ExitEnter.Count; i++)
            {
                Transform pTransform = _listTransform_ExitEnter[i];
                _listTransform_EnterAlready.Remove(pTransform);

                var pExit = pTransform.GetComponent<IPointerExitHandler>();
                if (pExit != null)
                    pExit.OnPointerExit(null);

            }

            _listTransform_EnterAlready.AddRange(_listTransform_EnterNew);
        }
    }

    private void CalculateInputEvent_OnPC()
    {
        if (p_pEventCamera == null)
        {
            InitCamera();
            if (p_pEventCamera == null)
                return;
        }

        bool bIsClick = Input.GetMouseButtonUp(0);

        if (p_bIs2D)
            _iLastHitCount = Physics2D.GetRayIntersectionNonAlloc(p_pEventCamera.ScreenPointToRay(Input.mousePosition), _arrHit_2D, Mathf.Infinity, p_pLayerMask_Hit.value);
        else
            _iLastHitCount = Physics.RaycastNonAlloc(p_pEventCamera.ScreenPointToRay(Input.mousePosition), _arrHit, Mathf.Infinity, p_pLayerMask_Hit.value);

        if (CheckDebugFilter(EDebugFilter.Debug_Level_Core) && bIsClick)
            pRay_OnClick_ForDebug = p_pEventCamera.ScreenPointToRay(Input.mousePosition);

        p_listLastHit.Clear();
        _listTransform_EnterNew.Clear();
        _listTransform_ExitEnter.Clear();
        for (int i = 0; i < _iLastHitCount; i++)
        {
            CRaycastHitWrapper pHit = _arrHit[i];
            if (pHit.transform == null)
                continue;

            p_listLastHit.Add(pHit);
            Transform pTransformHit = pHit.transform;

            if (CheckDebugFilter(EDebugFilter.Debug_Level_Core))
                Debug.Log(pTransformHit.name + " RayCast Hit bMouseClick: " + bIsClick, pTransformHit);

            if (bIsClick)
            {
                var pClick = pTransformHit.GetComponent<IPointerClickHandler>();
                if (pClick != null)
                    pClick.OnPointerClick(null);
            }

            _listTransform_EnterNew.Add(pHit.transform);
        }

        for (int i = 0; i < _listTransform_EnterAlready.Count; i++)
        {
            Transform pTransform = _listTransform_EnterAlready[i];
            if (_listTransform_EnterNew.Contains(pTransform))
                _listTransform_EnterNew.Remove(pTransform);
            else
                _listTransform_ExitEnter.Add(pTransform);
        }

        for (int i = 0; i < _listTransform_EnterNew.Count; i++)
        {
            if (_listTransform_EnterNew[i] == null)
                continue;

            var pEnter = _listTransform_EnterNew[i].GetComponent<IPointerEnterHandler>();
            if (pEnter != null)
                pEnter.OnPointerEnter(null);
        }

        for (int i = 0; i < _listTransform_ExitEnter.Count; i++)
        {
            Transform pTransform = _listTransform_ExitEnter[i];
            _listTransform_EnterAlready.Remove(pTransform);

            var pExit = pTransform.GetComponent<IPointerExitHandler>();
            if (pExit != null)
                pExit.OnPointerExit(null);

        }

        _listTransform_EnterAlready.AddRange(_listTransform_EnterNew);
    }

    private void InitCamera()
    {
        if (p_pEventCamera == null)
        {
            Camera[] arrCamera = FindObjectsOfType<Camera>();
            for (int i = 0; i < arrCamera.Length; i++)
            {
                if (arrCamera[i].gameObject.tag == "MainCamera")
                {
                    p_pEventCamera = arrCamera[i];
                    break;
                }
            }

            if (p_pEventCamera == null)
                p_pEventCamera = arrCamera[0];
        }
    }

    #endregion Private
}