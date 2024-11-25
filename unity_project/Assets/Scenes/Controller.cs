using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    // SceneManager 저장
    public SceneManager sceneManager;

    // Device handler 인스턴스 저장
    public SerialDeviceHandler deviceHandler;

    [Space(20)]

    // Vibrator 강도 조절할 슬라이더 저장
    public Slider[] vibratorIntensitySliders = new Slider[10];

    // 비동기적으로 진동자 제어
    private Coroutine _controlRoutine;

    private void Awake()
    {
        // 슬라이더 개수가 10개가 아닐 경우 종료 (진동자 수가 10개이기 때문)
        if (vibratorIntensitySliders.Length != 10) return;

        // 이벤트 연결
        sceneManager.onConnected += OnConnected;
        sceneManager.onDisconnected += OnDisconnected;
    }

    private void OnConnected()
    {
        if (_controlRoutine != null) {
            StopCoroutine(_controlRoutine);
        }

        _controlRoutine = StartCoroutine(VibratorControlThread());
    }

    private void OnDisconnected()
    {
        StopCoroutine(_controlRoutine);
        _controlRoutine = null;
    }

    private IEnumerator VibratorControlThread()
    {
        while (true)
        {
            for (int i = 0; i < 10; i++) {
                deviceHandler.SetVibratorIntensity(i, vibratorIntensitySliders[i].value);
            }
            deviceHandler.SendPacket();
            yield return new WaitForSeconds(0.01f);
        }
    }

    private void OnApplicationQuit()
    {
        if (_controlRoutine != null) {
            StopCoroutine(_controlRoutine);
        }
    }
}
