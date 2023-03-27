using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
namespace UCHPlayerTrackerMod;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private int framesLeft;

    private (Queue<Vector3> positions, LineRenderer renderer)[] _lines;
    private ConfigEntry<int> _trackingLength;
    private static ConfigEntry<int> _skipFrames;
    private ConfigEntry<float> _lineWidthStart;
    private ConfigEntry<float> _lineWidthEnd;
    private ConfigEntry<bool> _enabled;

    private void Awake()
    {
        _enabled = Config.Bind("General", "Enabled", true, "To disable this mod set this setting to 'false'.");
        _enabled.SettingChanged += (_, _) => ClearLines();
        _trackingLength = Config.Bind("General", "TrackingLength", 120, "The length of the line in timeSteps (60 -> 1s).");
        _skipFrames = Config.Bind("General", "SkipFrames", 0, "Skip n frames before tracking next frame.");
        _lineWidthStart = Config.Bind("General", "LineStartWidth", 0.1f, "Width of the tracking line at the start.");
        _lineWidthStart.SettingChanged += (_, _) => UpdateLineRenderer(a => a.endWidth = _lineWidthStart.Value);
        _lineWidthEnd = Config.Bind("General", "LineEndWidth", 0.1f, "Width of the tracking line at the end.");
        _lineWidthEnd.SettingChanged += (_, _) => UpdateLineRenderer(a => a.startWidth = _lineWidthEnd.Value);


        _lines = new (Queue<Vector3>, LineRenderer)[8];
        for (int i = 0; i < _lines.Length; i++)
        {
            _lines[i] = (new Queue<Vector3>(), CreateLineRenderer());
        }

        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void ClearLines()
    {
        foreach ((Queue<Vector3> positions, LineRenderer renderer) in _lines)
        {
            positions.Clear();
            renderer.positionCount = positions.Count;
            renderer.SetPositions(positions.ToArray());
        }
    }

    private void UpdateLineRenderer(Action<LineRenderer> action)
    {
        foreach ((Queue<Vector3> positions, LineRenderer renderer) in _lines)
        {
            action(renderer);
        }
    }

    private LineRenderer CreateLineRenderer()
    {
        var obj = new GameObject
        {
            transform =
            {
                parent = transform
            }
        };
        var lineRenderer = obj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startWidth = _lineWidthEnd.Value;
        lineRenderer.endWidth = _lineWidthStart.Value;
        lineRenderer.endColor = Color.white;
        lineRenderer.useWorldSpace = true;
        return lineRenderer;
    }

    private void FixedUpdate()
    {
        if (!_enabled.Value)
            return;
        try
        {
            if (framesLeft-- > 0)
                return;

            framesLeft = _skipFrames.Value;

            foreach ((Character character, Queue<Vector3> positions, LineRenderer renderer) in GetPlayers())
            {
                Vector3 pos = character.transform.position;

                positions.Enqueue(pos);

                if (positions.Count > _trackingLength.Value)
                    positions.Dequeue();

                renderer.positionCount = positions.Count;
                renderer.SetPositions(positions.ToArray());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message + ex.StackTrace);
        }
    }

    private IEnumerable<(Character character, Queue<Vector3> positions, LineRenderer renderer)> GetPlayers()
    {
        if (LobbyManager.instance == null)
            yield break;

        if (LobbyManager.instance.CurrentGameController != null)
        {
            Dictionary<int, GamePlayer> players = LobbyManager.instance.CurrentGameController.CurrentPlayerQueue
                .ToDictionary(a => a.networkNumber - 1);

            for (int i = 0; i < _lines.Length; i++)
            {
                (Queue<Vector3> positions, LineRenderer renderer) = _lines[i];

                if (!players.TryGetValue(i, out GamePlayer gamePlayer) || gamePlayer.CharacterInstance == null)
                {
                    positions.Clear();
                    renderer.positionCount = positions.Count;
                    renderer.SetPositions(positions.ToArray());
                    continue;
                }

                renderer.startColor = gamePlayer.PlayerColor;
                //renderer.material.color = gamePlayer.PlayerColor;
                yield return (gamePlayer.CharacterInstance, positions, renderer);
            }
        }
        else
        {
            Dictionary<int, LobbyPlayer> players = LobbyManager.instance.GetLobbyPlayers().ToDictionary(a => a.networkNumber - 1);
            for (int i = 0; i < _lines.Length; i++)
            {
                (Queue<Vector3> positions, LineRenderer renderer) = _lines[i];

                if (!players.TryGetValue(i, out LobbyPlayer lobbyPlayer) || lobbyPlayer.CharacterInstance == null)
                {
                    positions.Clear();
                    renderer.positionCount = positions.Count;
                    renderer.SetPositions(positions.ToArray());
                    continue;
                }

                renderer.material.color = lobbyPlayer.PlayerColor;
                yield return (lobbyPlayer.CharacterInstance, positions, renderer);
            }
        }
    }
}