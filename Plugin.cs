using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using BepInEx;
using BepInEx.Configuration;
using GameEvent;
using UnityEngine;

namespace UCHAutoSaveMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin, IGameEventListener
    {
        private static readonly Regex AutoSaveNameRegex = new(@"^AutoSave \[(?<LevelName>.*?)\].*", RegexOptions.Compiled);

        private ConfigEntry<bool> _autoSaveEnabled;
        private Task _saveTask = Task.CompletedTask;
        private string _autoSaveFileName;
        private bool _autoSavePending;

        private void Awake()
        {
            _autoSaveEnabled = Config.Bind("General", "AutoSave Enabled", true, "Set to false to disable AutoSave.");

            GameEventManager.ChangeListener<PiecePlacedEvent>(this, true);
            GameEventManager.ChangeListener<DestroyPieceEvent>(this, true);
            GameEventManager.ChangeListener<EndPhaseEvent>(this, true);

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void LateUpdate()
        {
            if (!_saveTask.Wait(0))
                return;

            if (_autoSavePending)
            {
                _autoSavePending = false;
                _saveTask = SaveLevel(_autoSaveFileName);
            }
        }

        public void handleEvent(GameEvent.GameEvent e)
        {
            if (e is EndPhaseEvent { Phase: GameControl.GamePhase.START })
            {
                _autoSaveFileName = $"AutoSave [{GetLevelName()}] {DateTime.UtcNow:yyyy.MM.dd_HHmm}";

                var playerNames = LobbyManager.instance.GetLobbyPlayers()
                    .Select(player => player.playerName)
                    .ToArray();

                if (playerNames.Length > 1)
                    _autoSaveFileName += " " + string.Join(", ", playerNames);

                Debug.Log($"Set AutoSave file to '{_autoSaveFileName}'");
            }
            else if (e is PiecePlacedEvent or DestroyPieceEvent)
            {
                if (!_autoSaveEnabled.Value)
                    return;

                if (LobbyManager.instance.CurrentGameController.Phase != GameControl.GamePhase.PLACE)
                    return;
                
                _autoSavePending = true;
            }
        }

        private static async Task SaveLevel(string name)
        {
            string fileName = QuickSaver.LocalSavesFolder + "/" + name +
                              QuickSaver.GetLocalSaveSuffixForLevelType(FeaturedQuickFilter.LevelTypes.Versus) +
                              ".snapshot";

            Debug.Log($"Start AutoSave to '{fileName}'");

            QuickSaver quickSaver = LobbyManager.instance.CurrentGameController.GetComponent<QuickSaver>();

            XmlDocument currentXmlSnapshot = quickSaver.GetCurrentXmlSnapshot(false);

            byte[] compressedBytes = await Task.Run(() => QuickSaver.GetCompressedBytesFromXmlDoc(currentXmlSnapshot));

            File.WriteAllBytes(fileName, compressedBytes);

            quickSaver.SaveLocalThumbnail(
                QuickSaver.GetSnapshotNameWithoutSuffix(
                    Path.GetFileNameWithoutExtension(fileName))
            );

            Debug.Log($"AutoSaved '{fileName}'");
        }

        private static string GetLevelName()
        {
            string currentLevelName = GameState.GetInstance().currentSnapshotInfo.snapshotName.Length > 0
                ? GameState.GetInstance().currentSnapshotInfo.snapshotName
                : LobbyManager.instance.CurrentGameController.LevelLayout.thisLevelis.ToString();

            if (currentLevelName.Length > 0)
            {
                var match = AutoSaveNameRegex.Match(currentLevelName);

                if (match.Success)
                    currentLevelName = match.Groups["LevelName"].Value;
            }

            return currentLevelName;
        }
    }
}