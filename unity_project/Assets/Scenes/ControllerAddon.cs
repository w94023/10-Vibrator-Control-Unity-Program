using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUI;

public class ControllerAddon : MonoBehaviour
{
    // Controller 참조
    public Controller controller;

    // Mode 선택 toggle buttons
    public ToggleButton waveToggleButton;
    public ToggleButton impactToggleButton;
    public ToggleButton pulseToggleButton;
    public ToggleButton randomToggleButton;

    // Toggle 버튼의 toggle state 저장하는 array
    private ToggleButton[] _toggleButtons = new  ToggleButton[4];
    private bool[] _toggleState = new bool[4];

    // 진동자 패턴 비동기 제어 루틴
    private Coroutine _modeRoutine;

    private void Awake()
    {
        // Toggle button array로 저장
        _toggleButtons[0] = waveToggleButton;
        _toggleButtons[1] = impactToggleButton;
        _toggleButtons[2] = pulseToggleButton;
        _toggleButtons[3] = randomToggleButton;



        _modeRoutine = null;
    }

    private void Start()
    {
        // 이벤트 연결
        for (int i = 0; i < _toggleButtons.Length; i++) {
            int index = i;
            _toggleButtons[i].onClick.AddListener(() => OnToggleButtonClicked(index, _toggleButtons[index].value));
        }
    }

    private void OnToggleButtonClicked(int index, int value)
    {
        // Toggle 정보 저장
        _toggleState[index] = (value == 1) ? true : false;

        // 4개의 mode 중 하나가 활성화 되었을 때
        if (value == 1) {

            // 다른 mode가 활성화 되어 있다면 종료
            for (int i = 0; i < _toggleState.Length; i++) {
                if (i == index) continue;

                if (_toggleState[i] == true) {
                    _toggleButtons[i].Toggle();
                    _toggleState[i] = false;
                }
            }

            StopVibratingMode();
            if      (index == 0) StartWaveMode();
            else if (index == 1) StartImpactMode();
            else if (index == 2) StartPulseMode();
            else if (index == 3) StartRandomMode();
        }

        else {
            StopVibratingMode();
        }
    }

    private void StopVibratingMode()
    {
        if (_modeRoutine != null) {
            StopCoroutine(_modeRoutine);
            for (int i = 0; i < controller.vibratorIntensitySliders.Length; i++) {
                controller.vibratorIntensitySliders[i].value = 0f;
            }
            _modeRoutine = null;
        }
    }

    private void StartWaveMode()
    {
        _modeRoutine = StartCoroutine(WaveModeThreadFunc());
    }

    private IEnumerator WaveModeThreadFunc()
    {
        int count = -1;
        int devider = 3;

        while (true) {
            count += 1;

            // count가 devider의 배수일 때
            if (count % devider == 0) {
                int index = count / devider;

                if (index < 10) {
                    for (int i = 0; i < index; i++) {
                        controller.vibratorIntensitySliders[i].value += 0.05f;
                    }
                }
                else {
                    for (int i = 0; i < index-9; i++) {
                        if (i < 0 || i > 9) continue;
                        controller.vibratorIntensitySliders[i].value -= 0.05f;
                    }

                    for (int i = index-9; i < index; i++) {
                        if (i < 0 || i > 9) continue;
                        controller.vibratorIntensitySliders[i].value += 0.05f;
                    }
                }
                

                if (index == 29) {
                    count = -1;
                } 
            }
            yield return new WaitForSeconds(0.005f);
        }
    }

    private void StartImpactMode()
    {
        _modeRoutine = StartCoroutine(ImpactModeThreadFunc());
    }

    private IEnumerator ImpactModeThreadFunc()
    {
        int count = 0;
        while (true) {
            count += 1;

            if (count == 100) {
                for (int i = 0; i < controller.vibratorIntensitySliders.Length; i++) {
                    controller.vibratorIntensitySliders[i].value = 0.7f;
                }
            }
            else if (count == 110) {
                for (int i = 0; i < controller.vibratorIntensitySliders.Length; i++) {
                    controller.vibratorIntensitySliders[i].value = 0;
                }

                count = 0;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    private void StartPulseMode()
    {
        _modeRoutine = StartCoroutine(PulseModeThreadFunc());
    }

    private IEnumerator PulseModeThreadFunc()
    {
        int count = -1;
        int devider = 4;

        while (true) {
            count += 1;
            // count가 devider의 배수일 때

            if (count % devider == 0) {
                int index = count / devider;

                // 파형 활성화
                if (index < 5) {
                    // 왼쪽 
                    controller.vibratorIntensitySliders[4-index].value = 0.6f;
                    // 오른쪽
                    controller.vibratorIntensitySliders[5+index].value = 0.6f;
                }
                
                // 파형 초기화
                for (int i = 0; i < index; i++) {
                    if (i > 4) break;
                    // 왼쪽
                    controller.vibratorIntensitySliders[4-i].value -= 0.05f;
                    // 오른쪽
                    controller.vibratorIntensitySliders[5+i].value -= 0.05f;
                }
                

                if (index == 29) {
                    count = -1;
                } 
            }

            yield return new WaitForSeconds(0.005f);
        }
    }

    private void StartRandomMode()
    {
        _modeRoutine = StartCoroutine(RandomModeThreadFunc());
    }

    private IEnumerator RandomModeThreadFunc()
    {
        int count = 0;
        while (true) {
            count += 1;

            if (count == 20) {
                System.Random random = new System.Random();
                int index = random.Next(0, 10); // 0에서 9까지 random int 반환
                float value = (float)(random.NextDouble() * 0.3); // 0에서 0.3까지 random float 반환

                controller.vibratorIntensitySliders[index].value = value;
                count = 0;
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    private void OnApplicationQuit()
    {
        StopVibratingMode();
    }
}
