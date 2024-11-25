using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityPlotter;

public class PlotHandle : MonoBehaviour
{
    // sceneManager 인스턴스 저장
    public SceneManager sceneManager;

    // 데이터 플랏 인스턴스 저장
    public Plotter plotter;

    // 플랏 데이터
    private List<List<float>> _plotData = new List<List<float>>();
    private int _dataCountLimit = 500;

    private void Awake()
    {
        sceneManager.onConnected    += Clear;
        sceneManager.onDisconnected += Clear;
        sceneManager.onDataReceived += PlotData;

        Clear();
    }

    private void Clear()
    {
        // plot data initialization
        _plotData.Clear();
        for (int i = 0; i < 10; i++) {
            _plotData.Add(new List<float>());
        }
        
        plotter.Clear();
    }

    private void PlotData(int[] vibratorIntensities)
    {
        if (vibratorIntensities.Length != 10) return;

        for (int i = 0; i < vibratorIntensities.Length; i++) {
            _plotData[i].Add((float)vibratorIntensities[i] + 10f + 120f * i);

            if (_plotData[i].Count > _dataCountLimit) {
                _plotData[i].RemoveAt(0);
            }

            plotter.Plot(_plotData[i].ToArray(), lineWidth : 2f);
        }

        plotter.Draw();
    }
}
