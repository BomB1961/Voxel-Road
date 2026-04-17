// 2026-04-17
// Assets/Editor/FilteredDebugLog.cs
// Unity 콘솔 로그를 핵심 1줄로 압축하여 debug_filtered.log에 저장 (Claude 디버깅 토큰 절약용)

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 모든 Unity 콘솔 로그를 가로채어 핵심 내용만 추출 후 파일로 저장.
/// Claude Code가 debug_filtered.log 한 파일만 읽으면 전체 상황 파악 가능.
/// </summary>
[InitializeOnLoad]
public static class FilteredDebugLog
{
    // 프로젝트 루트/debug_filtered.log
    private static readonly string LogPath = Path.Combine(
        Directory.GetParent(Application.dataPath).FullName, "debug_filtered.log");

    static FilteredDebugLog()
    {
        Application.logMessageReceived += OnLog;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        // Editor 시작 시 헤더 기록
        WriteHeader("Editor Loaded");
    }

    // Play 모드 진입 시 파일 초기화 (누적 방지)
    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            WriteHeader("Play Started");
    }

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        // 메시지 첫 줄만 추출
        string firstLine = message.Split('\n')[0].Trim();
        if (string.IsNullOrEmpty(firstLine)) return;

        // 타입 태그
        string tag = type switch
        {
            LogType.Warning   => "[WRN]",
            LogType.Error     => "[ERR]",
            LogType.Exception => "[EXC]",
            LogType.Assert    => "[AST]",
            _                 => "[LOG]"
        };

        // 스택트레이스에서 우리 코드(Assets/Scripts) 위치 1줄만 추출
        string location = ExtractLocation(stackTrace);

        string time = DateTime.Now.ToString("HH:mm:ss");
        string line = string.IsNullOrEmpty(location)
            ? $"{time} {tag} {firstLine}"
            : $"{time} {tag} {firstLine} @ {location}";

        try { File.AppendAllText(LogPath, line + "\n", Encoding.UTF8); }
        catch { /* 파일 잠금 시 무시 */ }
    }

    // Assets/Scripts 경로가 포함된 스택트레이스 첫 줄에서 파일:라인 추출
    private static string ExtractLocation(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace)) return string.Empty;

        foreach (string line in stackTrace.Split('\n'))
        {
            if (!line.Contains("Assets/Scripts") && !line.Contains("Assets\\Scripts"))
                continue;

            int inIdx = line.IndexOf(" in ", StringComparison.Ordinal);
            if (inIdx < 0) return line.Trim();

            string path = line.Substring(inIdx + 4).Trim();
            int assetsIdx = path.IndexOf("Assets", StringComparison.Ordinal);
            return assetsIdx >= 0 ? path.Substring(assetsIdx) : path;
        }
        return string.Empty;
    }

    private static void WriteHeader(string label)
    {
        try
        {
            File.AppendAllText(LogPath,
                $"\n=== {label} {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n",
                Encoding.UTF8);
        }
        catch { }
    }
}
