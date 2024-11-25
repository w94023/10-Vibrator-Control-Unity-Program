using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using SerialManager;
using UnityUI;

public class SerialDeviceHandler : MonoBehaviour
{
    private enum DeviceType {
        USB,
        BTClassic,
        BLE
    }

    // serial device 선택을 위한 dropdown
    public Dropdown          deviceName;
    public UIExtension       deviceNameExtension;

    [Space(20)]

    // device type 선택을 위한 toggle button
    public ToggleButton deviceTypeToggleButton;
    private DeviceType _deviceType;

    [Space(20)]

    // connect / disconnect 버튼
    public Button            connectButton;
    public Button            disconnectButton;

    [Space(20)]

    // connection state 표시
    public ProgressIndicator connectionIndicator;

    [Space(20)]

    // BPS 표시
    public Text bpsText;

    [Space(20)]

    // serial handle 및 연결 상태
    public SerialHandleUnity serialHandle;
    private bool _isConnected;

    // 스캔된 device 저장
    private List<string> _scannedDevice = new List<string>();

    // 데이터
    public byte[] packet = new byte[14];

    // 수신된 진동자 상태
    public int[] vibratorStates = new int[10];
    
    // 이벤트
    public Action        onConnected;
    public Action        onDisconnected;
    public Action<int[]> onDataReceived;

    private void Awake()
    {
        // 패킷 헤더 설정
        packet[0] = 0xFF;
        packet[1] = 0xFF;

        // 데이터 길이 표시하는 패킷 설정
        packet[2] = 0x0B;

        // 디바이스 타입 변경 이벤트 등록
        _deviceType = (DeviceType)deviceTypeToggleButton.value; // 최초 deviceType 설정
        deviceTypeToggleButton.onClick.AddListener(OnToggleButtonClicked);

        // 시리얼 매니저 이벤트 등록
        serialHandle.onScanEnded.AddListener(OnSerialDeviceScanEnded);
        serialHandle.onConnected.AddListener(OnDeviceConnected);
        serialHandle.onConnectionFailed.AddListener(OnDeviceConnectionFailed);
        serialHandle.onDisconnected.AddListener(OnDeviceDisconnected);
        serialHandle.onDataReceived.AddListener(OnDataReceived);

        // 장치 이름 선택 dropdown 이벤트 등록
        deviceNameExtension.onPointerClick.AddListener(OnDeviceNameClicked);
        deviceName.onValueChanged.AddListener(OnDeviceNameChanged);

        // 장치 connect 및 disconnect 버튼 이벤트 등록
        disconnectButton.gameObject.SetActive(false);
        connectButton.onClick.AddListener(ConnectDevice);
        disconnectButton.onClick.AddListener(DisconnectDevice);
    }

    private void Start()
    {
        OnDeviceNameClicked();
    }

    private void OnToggleButtonClicked()
    {
        DeviceType deviceType = (DeviceType)deviceTypeToggleButton.value;
        if (_deviceType != deviceType) {
            _deviceType = deviceType;
            OnDeviceTypeChanged();
        }
    }

    private void OnDeviceTypeChanged()
    {
        UpdateDeviceNameDropdown();
    }

    private void OnSerialDeviceScanEnded(SerialLog e)
    {
        _scannedDevice.Clear();

        for (int i = 0; i < e.devices.Length; i++) {
            _scannedDevice.Add(e.devices[i]);
        }

        UpdateDeviceNameDropdown();
    }

    private void UpdateDeviceNameDropdown()
    {
        List<string> devices = new List<string>();

        for (int i = 0; i < _scannedDevice.Count; i++) {
            if (_deviceType == DeviceType.USB) {
                if (_scannedDevice[i].Contains("[USB] ")) {
                    devices.Add(_scannedDevice[i].Replace("[USB] ", ""));
                }
            }
            else if (_deviceType == DeviceType.BTClassic) {
                if (_scannedDevice[i].Contains("[BTClassic] ")) {
                    devices.Add(_scannedDevice[i].Replace("[BTClassic] ", ""));
                }
                if (_scannedDevice[i].Contains("[BLE] ")) {
                    devices.Add(_scannedDevice[i].Replace("[BLE] ", ""));
                }
            }
            else if (_deviceType == DeviceType.BLE) {
                if (_scannedDevice[i].Contains("[BTClassic] ")) {
                    devices.Add(_scannedDevice[i].Replace("[BTClassic] ", ""));
                }
                if (_scannedDevice[i].Contains("[BLE] ")) {
                    devices.Add(_scannedDevice[i].Replace("[BLE] ", ""));
                }
            }
        }

        deviceName.ClearOptions();
        deviceName.AddOptions(devices);
        OnDeviceNameChanged(deviceName.value);
    }

    private void OnDeviceNameClicked()
    {
        serialHandle.ScanDevices();
    }

    private void OnDeviceNameChanged(int index)
    {
        if (deviceName.options.Count < 1) {
            UnityEngine.Debug.Log("조회된 연결 가능한 장치가 없습니다");
            return;
        }

        if (_deviceType == DeviceType.USB) {
            serialHandle.deviceType = SerialManager.SerialHandleUnity.DeviceType.USB;
            serialHandle.portName = deviceName.options[index].text;
        }
        else if (_deviceType == DeviceType.BTClassic) {
            serialHandle.deviceType = SerialManager.SerialHandleUnity.DeviceType.BTClassic;
            serialHandle.deviceName = deviceName.options[index].text;
        }
        else if (_deviceType == DeviceType.BLE) {
            serialHandle.deviceType = SerialManager.SerialHandleUnity.DeviceType.BLE;
            serialHandle.deviceName = deviceName.options[index].text;
        }
    }

    private void ConnectDevice()
    {
        serialHandle.Connect();
        connectionIndicator.StartProgressing();
    }

    private void DisconnectDevice()
    {
        serialHandle.Disconnect();
        connectionIndicator.ClearProgressing();
        connectButton.gameObject.SetActive(true);
        disconnectButton.gameObject.SetActive(false);
    }

    private void OnDeviceConnected()
    {
        connectionIndicator.StopProgressing(true);
        connectButton.gameObject.SetActive(false);
        disconnectButton.gameObject.SetActive(true);

        bpsText.text = "";
        onConnected?.Invoke();
    }

    private void OnDeviceConnectionFailed()
    {
        connectionIndicator.StopProgressing(false);
        connectButton.gameObject.SetActive(true);
        disconnectButton.gameObject.SetActive(false);

        bpsText.text = "";
        onDisconnected?.Invoke();
    }

    private void OnDeviceDisconnected()
    {
        bpsText.text = "";
        onDisconnected?.Invoke();
    }

    public void SetVibratorIntensity(int index, float value)
    {
        packet[index+3] = (byte)(value * 100f);
    }

    public void SendPacket()
    {
        // 체크섬 계산
        byte checksum = 0;
        for (int i = 2; i < 13; i++) {
            checksum = (byte)(checksum + packet[i]); // 8비트로 제한
        }
        packet[13] = (byte)(~checksum);

        // 데이터 전송
        serialHandle.SendData(packet);
    }

    public void OnDataReceived(double time, SerialData e)
    {
        // HEX 패킷 - 기준으로 분할
        string[] tokens = e.packet.Split('-');

        // 패킷 길이 14가 아닐 시 종료
        if (tokens.Length != 14) return;

        // 진동자 데이터 부분만 HEX 패킷 DEC int로 변환
        for (int i = 1; i < tokens.Length-3; i++) {
            vibratorStates[i-1] = Convert.ToInt32(tokens[i], 16);
        }

        // BPS text 초기화
        bpsText.text = (serialHandle.PPS * 14).ToString("0000");

        // 이벤트 호출
        onDataReceived?.Invoke((int[])vibratorStates.Clone());
    }

    private void OnApplicationQuit()
    {
        SendPacket();
    }
}
