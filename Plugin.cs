using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        private DateTime _startTime;

        private void Awake()
        {
            _autoSaveEnabled = Config.Bind("General", "AutoSave Enabled", true, "Set to false to disable AutoSave.");

            GameEventManager.ChangeListener<PiecePlacedEvent>(this, true);
            GameEventManager.ChangeListener<DestroyPieceEvent>(this, true);
            GameEventManager.ChangeListener<EndPhaseEvent>(this, true);

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public void handleEvent(GameEvent.GameEvent e)
        {
            if (e is PiecePlacedEvent or DestroyPieceEvent)
            {
                if (!_autoSaveEnabled.Value)
                    return;

                if (LobbyManager.instance.CurrentGameController.Phase != GameControl.GamePhase.PLACE)
                    return;

                AutoSaveGame();
            }
            else if (e is EndPhaseEvent { Phase: GameControl.GamePhase.START })
                _startTime = DateTime.UtcNow;
        }

        private void AutoSaveGame()
        {
            Debug.Log("Start AutoSave");

            string autoSaveName = $"AutoSave [{GetLevelName()}] {_startTime:yyyy.MM.dd_HHmm}";

            var playerNames = LobbyManager.instance.GetLobbyPlayers()
                .Select(player => player.playerName)
                .ToArray();

            if (playerNames.Length > 1)
                autoSaveName += " " + string.Join(", ", playerNames);

            SaveLevel(autoSaveName);
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

        private static void SaveLevel(string name)
        {
            QuickSaver quickSaver = LobbyManager.instance.CurrentGameController.GetComponent<QuickSaver>();

            XmlDocument currentXmlSnapshot = quickSaver.GetCurrentXmlSnapshot(false);
            byte[] compressedBytes = QuickSaver.GetCompressedBytesFromXmlDoc(currentXmlSnapshot);

            string fileName = QuickSaver.LocalSavesFolder + "/" + name +
                              QuickSaver.GetLocalSaveSuffixForLevelType(FeaturedQuickFilter.LevelTypes.Versus) +
                              ".snapshot";

            try
            {
                File.WriteAllBytes(fileName, compressedBytes);

                quickSaver.SaveLocalThumbnail(
                    QuickSaver.GetSnapshotNameWithoutSuffix(
                        Path.GetFileNameWithoutExtension(fileName))
                );

                Debug.Log($"AutoSaved {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError("Couldn't save file: " + ex.Message);
            }
        }
    }
}