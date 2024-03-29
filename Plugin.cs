﻿using System;
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
        private ConfigEntry<bool> _partyAutoSaveEnabled;
        private ConfigEntry<bool> _freePlayAutoSaveEnabled;

        private Task _saveTask = Task.CompletedTask;
        private string _autoSaveFileName;
        private bool _autoSavePending;

        private void Awake()
        {
            _partyAutoSaveEnabled = Config.Bind("General", "AutoSave in Party", true, "Set to false to disable AutoSave in Party games.");
            _freePlayAutoSaveEnabled = Config.Bind("General", "AutoSave in FreePlay", true, "Set to false to disable AutoSave in FreePlay games.");

            GameEventManager.ChangeListener<PiecePlacedEvent>(this, true);
            GameEventManager.ChangeListener<DestroyPieceEvent>(this, true);
            GameEventManager.ChangeListener<EndPhaseEvent>(this, true);

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        public void handleEvent(GameEvent.GameEvent e)
        {
            if (e is EndPhaseEvent { Phase: GameControl.GamePhase.START })
            {
                _autoSaveFileName = QuickSaver.LocalSavesFolder + "/" +
                                    $"AutoSave {DateTime.Now:yyyy.MM.dd HHmm}" +
                                    QuickSaver.GetLocalSaveSuffixForLevelType(FeaturedQuickFilter.LevelTypes.Versus) +
                                    ".snapshot";

                Debug.Log($"Set AutoSave file to '{_autoSaveFileName}'");
            }
            else if (e is PiecePlacedEvent or DestroyPieceEvent)
            {
                var gameMode = GameSettings.GetInstance().GameMode;
                
                switch (gameMode)
                {
                    case GameState.GameMode.CHALLENGE:
                    case GameState.GameMode.PARTY or GameState.GameMode.CREATIVE when !_partyAutoSaveEnabled.Value:
                    case GameState.GameMode.FREEPLAY when !_freePlayAutoSaveEnabled.Value:
                        return;
                }

                if (LobbyManager.instance.CurrentGameController.Phase != GameControl.GamePhase.PLACE)
                    return;

                _autoSavePending = true;
            }
        }

        private void LateUpdate()
        {
            try
            {
                if (!_saveTask.Wait(0))
                    return;

                if (!_autoSavePending)
                    return;

                _autoSavePending = false;
                _saveTask = SaveLevel();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.GetBaseException());

                _autoSavePending = false;
                _saveTask = Task.CompletedTask;
            }
        }

        private async Task SaveLevel()
        {
            Debug.Log($"Start AutoSave to '{_autoSaveFileName}'");

            QuickSaver quickSaver = LobbyManager.instance.CurrentGameController.GetComponent<QuickSaver>();

            XmlDocument currentXmlSnapshot = quickSaver.GetCurrentXmlSnapshot(false);

            byte[] compressedBytes = await Task.Run(() => QuickSaver.GetCompressedBytesFromXmlDoc(currentXmlSnapshot));

            File.WriteAllBytes(_autoSaveFileName, compressedBytes);

            quickSaver.SaveLocalThumbnail(
                QuickSaver.GetSnapshotNameWithoutSuffix(
                    Path.GetFileNameWithoutExtension(_autoSaveFileName))
            );

            Debug.Log($"AutoSaved '{_autoSaveFileName}'");
        }
    }
}